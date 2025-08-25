"""
Simple Migration Script - Quick Start
For comprehensive migration with validation, logging, and error handling, use migrate_data.py
"""

import pandas as pd
from sqlalchemy import create_engine
from config import get_connection_string, COLUMN_MAPPING

def quick_migration():
    """Quick and simple migration for testing purposes"""
    print("🚀 Starting quick migration...")
    
    # 1. Read CSV
    print("📖 Reading CSV file...")
    df = pd.read_csv("cleaned_startup_data.csv")
    print(f"✅ Loaded {len(df)} records")
    print(f"📊 Columns: {list(df.columns)}")
    
    # 2. Rename columns to match database schema
    print("🔄 Mapping columns...")
    df = df.rename(columns=COLUMN_MAPPING)
    print("✅ Column mapping completed")
    
    # 3. Basic data cleaning
    print("🧹 Basic data cleaning...")
    # Handle date columns
    date_columns = ['FoundedAt', 'FirstFundingAt', 'LastFundingAt']
    for col in date_columns:
        if col in df.columns:
            df[col] = pd.to_datetime(df[col], errors='coerce')
    
    # Handle numeric columns
    if 'FundingTotalUSD' in df.columns:
        df['FundingTotalUSD'] = pd.to_numeric(df['FundingTotalUSD'], errors='coerce')
    if 'FundingRounds' in df.columns:
        df['FundingRounds'] = pd.to_numeric(df['FundingRounds'], errors='coerce')
    
    print("✅ Data cleaning completed")
    
    # 4. Connect to database
    print("🔌 Connecting to database...")
    engine = create_engine(get_connection_string())
    
    # 5. Migrate data
    print("📤 Migrating data to SQL Server...")
    df.to_sql(
        name="Companies",
        schema="dbo",
        con=engine,
        if_exists="replace",  # Use 'append' if you want to add to existing data
        index=False,
        chunksize=1000
    )
    
    print("✅ Data migrated successfully!")
    print(f"📊 Migrated {len(df)} records to SQL Server")
    
    # 6. Quick verification
    print("🔍 Quick verification...")
    verification_query = "SELECT COUNT(*) as record_count FROM dbo.Companies"
    result = pd.read_sql(verification_query, engine)
    print(f"✅ Records in database: {result['record_count'].iloc[0]}")

if __name__ == "__main__":
    print("=" * 60)
    print("🏢 STARTUP DATA MIGRATION - QUICK START")
    print("=" * 60)
    print()
    print("ℹ️  This is a simple migration script for quick testing.")
    print("ℹ️  For production use with full validation and logging,")
    print("ℹ️  please use: python migrate_data.py")
    print()
    
    try:
        quick_migration()
        print()
        print("🎉 Migration completed successfully!")
        print("💡 Next steps:")
        print("   1. Open SQL Server Management Studio")
        print("   2. Connect to your SQL Server instance")
        print("   3. Navigate to StartupDB > Tables > dbo.Companies")
        print("   4. Run sample queries from README.md")
        
    except Exception as e:
        print(f"❌ Migration failed: {str(e)}")
        print("💡 Try using the comprehensive migration script:")
        print("   python migrate_data.py")
