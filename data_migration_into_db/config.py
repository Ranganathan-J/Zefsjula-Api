"""
Configuration file for database migration
"""
import os
from urllib.parse import quote_plus

# Database Configuration
DATABASE_CONFIG = {
    'server': 'localhost',  # Change this to your SQL Server instance
    'database': 'StartupDB',
    'driver': 'ODBC Driver 17 for SQL Server',  # or 'ODBC Driver 18 for SQL Server'
    'trusted_connection': True,  # Set to False if using SQL Server authentication
    'username': '',  # Only needed if trusted_connection is False
    'password': '',  # Only needed if trusted_connection is False
}

# File Paths
CSV_FILE_PATH = 'cleaned_startup_data.csv'
LOG_FILE_PATH = 'migration.log'

# Migration Settings
MIGRATION_CONFIG = {
    'batch_size': 1000,  # Number of rows to insert at once
    'table_name': 'Companies',
    'schema': 'dbo',
    'if_exists': 'replace',  # 'replace', 'append', or 'fail'
    'chunksize': 1000,  # Chunk size for reading large CSV files
}

def get_connection_string():
    """Generate SQLAlchemy connection string"""
    if DATABASE_CONFIG['trusted_connection']:
        # Windows Authentication
        connection_string = (
            f"mssql+pyodbc://"
            f"@{DATABASE_CONFIG['server']}/{DATABASE_CONFIG['database']}"
            f"?driver={quote_plus(DATABASE_CONFIG['driver'])}"
            f"&trusted_connection=yes"
        )
    else:
        # SQL Server Authentication
        connection_string = (
            f"mssql+pyodbc://"
            f"{DATABASE_CONFIG['username']}:{quote_plus(DATABASE_CONFIG['password'])}"
            f"@{DATABASE_CONFIG['server']}/{DATABASE_CONFIG['database']}"
            f"?driver={quote_plus(DATABASE_CONFIG['driver'])}"
        )
    
    return connection_string

def get_pyodbc_connection_string():
    """Generate pyodbc connection string"""
    if DATABASE_CONFIG['trusted_connection']:
        connection_string = (
            f"DRIVER={{{DATABASE_CONFIG['driver']}}};"
            f"SERVER={DATABASE_CONFIG['server']};"
            f"DATABASE={DATABASE_CONFIG['database']};"
            f"Trusted_Connection=yes;"
        )
    else:
        connection_string = (
            f"DRIVER={{{DATABASE_CONFIG['driver']}}};"
            f"SERVER={DATABASE_CONFIG['server']};"
            f"DATABASE={DATABASE_CONFIG['database']};"
            f"UID={DATABASE_CONFIG['username']};"
            f"PWD={DATABASE_CONFIG['password']};"
        )
    
    return connection_string

# Column mapping from CSV to database
COLUMN_MAPPING = {
    'name': 'Name',
    'homepage_url': 'HomepageURL',
    'category_list': 'CategoryList',
    'funding_total_usd': 'FundingTotalUSD',
    'status': 'Status',
    'country_code': 'CountryCode',
    'state_code': 'StateCode',
    'city': 'City',
    'funding_rounds': 'FundingRounds',
    'founded_at': 'FoundedAt',
    'first_funding_at': 'FirstFundingAt',
    'last_funding_at': 'LastFundingAt'
}

# Data validation rules
VALIDATION_RULES = {
    'max_name_length': 255,
    'max_url_length': 500,
    'max_category_length': 500,
    'max_status_length': 50,
    'max_country_code_length': 10,
    'max_state_code_length': 50,
    'max_city_length': 100,
    'min_funding_amount': 0,
    'max_funding_amount': 999999999999999.99  # Based on DECIMAL(18,2)
}