"""
Comprehensive CSV to SQL Server Data Migration Script
Migrates cleaned startup data from CSV to SQL Server database
"""

import pandas as pd
import numpy as np
import pyodbc
from sqlalchemy import create_engine, text
import logging
from datetime import datetime
import sys
import os
from config import (
    get_connection_string, 
    get_pyodbc_connection_string,
    CSV_FILE_PATH, 
    LOG_FILE_PATH,
    MIGRATION_CONFIG,
    COLUMN_MAPPING,
    VALIDATION_RULES
)

def setup_logging():
    """Setup logging configuration"""
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(levelname)s - %(message)s',
        handlers=[
            logging.FileHandler(LOG_FILE_PATH),
            logging.StreamHandler(sys.stdout)
        ]
    )
    return logging.getLogger(__name__)

def validate_data(df, logger):
    """Validate data before migration"""
    logger.info("Starting data validation...")
    
    original_count = len(df)
    
    # Remove rows with missing critical data
    df = df.dropna(subset=['name'])
    logger.info(f"Removed {original_count - len(df)} rows with missing company names")
    
    # Validate string lengths
    if 'name' in df.columns:
        long_names = df['name'].str.len() > VALIDATION_RULES['max_name_length']
        if long_names.any():
            logger.warning(f"Found {long_names.sum()} companies with names longer than {VALIDATION_RULES['max_name_length']} characters")
            df.loc[long_names, 'name'] = df.loc[long_names, 'name'].str[:VALIDATION_RULES['max_name_length']]
    
    # Validate funding amounts
    if 'funding_total_usd' in df.columns:
        invalid_funding = (df['funding_total_usd'] < VALIDATION_RULES['min_funding_amount']) | \
                         (df['funding_total_usd'] > VALIDATION_RULES['max_funding_amount'])
        if invalid_funding.any():
            logger.warning(f"Found {invalid_funding.sum()} rows with invalid funding amounts")
            df.loc[invalid_funding, 'funding_total_usd'] = np.nan
    
    logger.info(f"Data validation completed. Final record count: {len(df)}")
    return df

def clean_and_transform_data(df, logger):
    """Clean and transform data for database insertion"""
    logger.info("Starting data cleaning and transformation...")
    
    # Rename columns to match database schema
    df = df.rename(columns=COLUMN_MAPPING)
    logger.info("Column names mapped to database schema")
    
    # Handle date columns with proper validation
    date_columns = ['FoundedAt', 'FirstFundingAt', 'LastFundingAt']
    sql_server_min_date = pd.Timestamp('1753-01-01')
    sql_server_max_date = pd.Timestamp('9999-12-31')
    
    for col in date_columns:
        if col in df.columns:
            logger.info(f"Processing date column: {col}")
            
            # Convert to datetime first
            df[col] = pd.to_datetime(df[col], errors='coerce')
            
            # Count invalid dates before cleaning
            invalid_before = df[col].isna().sum()
            
            # Handle dates outside SQL Server's valid range
            if df[col].notna().any():
                # Check for dates before SQL Server minimum
                too_old = df[col] < sql_server_min_date
                too_new = df[col] > sql_server_max_date
                
                if too_old.any():
                    logger.warning(f"Found {too_old.sum()} dates in {col} before 1753-01-01, setting to NULL")
                    df.loc[too_old, col] = pd.NaT
                
                if too_new.any():
                    logger.warning(f"Found {too_new.sum()} dates in {col} after 9999-12-31, setting to NULL")
                    df.loc[too_new, col] = pd.NaT
            
            # Keep as datetime object but ensure it's compatible with SQL Server DATE type
            # SQLAlchemy will handle the conversion to DATE automatically
            
            invalid_after = df[col].isnull().sum() if hasattr(df[col], 'isnull') else pd.isna(df[col]).sum()
            logger.info(f"Converted {col} to date - Invalid dates: {invalid_before} -> {invalid_after}")
    
    # Handle numeric columns
    numeric_columns = ['FundingTotalUSD', 'FundingRounds']
    for col in numeric_columns:
        if col in df.columns:
            df[col] = pd.to_numeric(df[col], errors='coerce')
            logger.info(f"Converted {col} to numeric")
    
    # Clean string columns
    string_columns = ['Name', 'HomepageURL', 'CategoryList', 'Status', 'CountryCode', 'StateCode', 'City']
    for col in string_columns:
        if col in df.columns:
            df[col] = df[col].astype(str)
            df[col] = df[col].replace('nan', np.nan)  # Replace string 'nan' with actual NaN
            df[col] = df[col].str.strip()  # Remove leading/trailing whitespace
    
    logger.info("Data cleaning and transformation completed")
    return df

def test_connection(logger):
    """Test database connection"""
    logger.info("Testing database connection...")
    
    try:
        # Test SQLAlchemy connection
        engine = create_engine(get_connection_string())
        with engine.connect() as conn:
            result = conn.execute(text("SELECT 1 AS test"))
            logger.info("SQLAlchemy connection successful")
        
        # Test pyodbc connection
        conn_str = get_pyodbc_connection_string()
        with pyodbc.connect(conn_str) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT 1")
            logger.info("pyodbc connection successful")
        
        return True
        
    except Exception as e:
        logger.error(f"Database connection failed: {str(e)}")
        return False

def create_database_if_not_exists(logger):
    """Create database if it doesn't exist"""
    logger.info("Checking if database exists...")
    
    try:
        # Connect to master database first
        conn_str = get_pyodbc_connection_string().replace('DATABASE=StartupDB;', 'DATABASE=master;')
        
        with pyodbc.connect(conn_str) as conn:
            cursor = conn.cursor()
            
            # Check if database exists
            cursor.execute("""
                SELECT name FROM sys.databases WHERE name = 'StartupDB'
            """)
            
            if not cursor.fetchone():
                logger.info("Database doesn't exist. Creating StartupDB...")
                cursor.execute("CREATE DATABASE StartupDB")
                conn.commit()
                logger.info("Database StartupDB created successfully")
            else:
                logger.info("Database StartupDB already exists")
        
        return True
        
    except Exception as e:
        logger.error(f"Error creating database: {str(e)}")
        return False

def migrate_data_to_sql(df, logger):
    """Migrate data to SQL Server"""
    logger.info(f"Starting data migration of {len(df)} records...")
    
    try:
        engine = create_engine(get_connection_string())
        
        # Migrate data in chunks
        chunk_size = MIGRATION_CONFIG['chunksize']
        total_chunks = (len(df) + chunk_size - 1) // chunk_size
        
        for i, chunk_start in enumerate(range(0, len(df), chunk_size)):
            chunk_end = min(chunk_start + chunk_size, len(df))
            chunk_df = df.iloc[chunk_start:chunk_end]
            
            chunk_df.to_sql(
                name=MIGRATION_CONFIG['table_name'],
                schema=MIGRATION_CONFIG['schema'],
                con=engine,
                if_exists='append' if i > 0 else MIGRATION_CONFIG['if_exists'],
                index=False,
                method='multi'
            )
            
            logger.info(f"Migrated chunk {i+1}/{total_chunks} ({chunk_end}/{len(df)} records)")
        
        logger.info("Data migration completed successfully!")
        return True
        
    except Exception as e:
        logger.error(f"Data migration failed: {str(e)}")
        return False

def verify_migration(logger):
    """Verify the migration was successful"""
    logger.info("Verifying migration...")
    
    try:
        engine = create_engine(get_connection_string())
        
        # Count records in database
        with engine.connect() as conn:
            result = conn.execute(text(f"SELECT COUNT(*) FROM {MIGRATION_CONFIG['schema']}.{MIGRATION_CONFIG['table_name']}"))
            db_count = result.scalar()
        
        logger.info(f"Records in database: {db_count}")
        
        # Get sample data
        sample_query = f"""
        SELECT TOP 5 
            Name, CategoryList, FundingTotalUSD, CountryCode, Status
        FROM {MIGRATION_CONFIG['schema']}.{MIGRATION_CONFIG['table_name']}
        ORDER BY FundingTotalUSD DESC
        """
        
        sample_df = pd.read_sql(sample_query, engine)
        logger.info("Sample of migrated data:")
        logger.info("\n" + sample_df.to_string())
        
        return True
        
    except Exception as e:
        logger.error(f"Migration verification failed: {str(e)}")
        return False

def generate_summary_report(df, logger):
    """Generate migration summary report"""
    logger.info("Generating migration summary report...")
    
    report = f"""
    
    ========================================
    DATA MIGRATION SUMMARY REPORT
    ========================================
    
    Migration Date: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}
    Source File: {CSV_FILE_PATH}
    Target Database: StartupDB
    Target Table: {MIGRATION_CONFIG['schema']}.{MIGRATION_CONFIG['table_name']}
    
    Data Statistics:
    - Total Records Migrated: {len(df):,}
    - Total Columns: {len(df.columns)}
    - Migration Method: {MIGRATION_CONFIG['if_exists']}
    - Chunk Size: {MIGRATION_CONFIG['chunksize']}
    
    Column Summary:
    {df.dtypes.to_string()}
    
    Data Quality:
    - Records with missing names: {df['Name'].isnull().sum()}
    - Records with funding data: {df['FundingTotalUSD'].notna().sum()}
    - Unique countries: {df['CountryCode'].nunique()}
    - Unique categories: {df['CategoryList'].nunique()}
    
    Top 5 Countries by Company Count:
    {df['CountryCode'].value_counts().head().to_string()}
    
    Funding Statistics:
    - Average Funding: ${df['FundingTotalUSD'].mean():,.2f}
    - Median Funding: ${df['FundingTotalUSD'].median():,.2f}
    - Max Funding: ${df['FundingTotalUSD'].max():,.2f}
    - Min Funding: ${df['FundingTotalUSD'].min():,.2f}
    
    ========================================
    """
    
    logger.info(report)
    
    # Save report to file
    with open('migration_summary_report.txt', 'w') as f:
        f.write(report)
    
    logger.info("Summary report saved to 'migration_summary_report.txt'")

def main():
    """Main migration function"""
    logger = setup_logging()
    
    logger.info("=" * 50)
    logger.info("STARTING CSV TO SQL SERVER MIGRATION")
    logger.info("=" * 50)
    
    # Check if CSV file exists
    if not os.path.exists(CSV_FILE_PATH):
        logger.error(f"CSV file not found: {CSV_FILE_PATH}")
        return False
    
    try:
        # Step 1: Test database connection
        if not test_connection(logger):
            return False
        
        # Step 2: Create database if needed
        if not create_database_if_not_exists(logger):
            return False
        
        # Step 3: Load CSV data
        logger.info(f"Loading data from {CSV_FILE_PATH}...")
        df = pd.read_csv(CSV_FILE_PATH)
        logger.info(f"Loaded {len(df)} records from CSV")
        
        # Step 4: Validate and clean data
        df = validate_data(df, logger)
        df = clean_and_transform_data(df, logger)
        
        # Step 5: Migrate data
        if not migrate_data_to_sql(df, logger):
            return False
        
        # Step 6: Verify migration
        if not verify_migration(logger):
            return False
        
        # Step 7: Generate summary report
        generate_summary_report(df, logger)
        
        logger.info("=" * 50)
        logger.info("MIGRATION COMPLETED SUCCESSFULLY!")
        logger.info("=" * 50)
        
        return True
        
    except Exception as e:
        logger.error(f"Migration failed with error: {str(e)}")
        return False

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)