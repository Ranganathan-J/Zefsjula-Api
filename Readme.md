# Startup Data Migration to SQL Server

This project migrates cleaned startup data from CSV format to SQL Server database.

## Prerequisites

### 1. SQL Server Setup

- SQL Server 2016 or later (Express, Standard, or Enterprise)
- SQL Server Management Studio (SSMS) - optional but recommended
- ODBC Driver 17 or 18 for SQL Server

### 2. Python Requirements

- Python 3.7 or later
- Required packages (install via `pip install -r requirements.txt`)

## Installation

1. **Install Python Dependencies**

   ```bash
   pip install -r requirements.txt
   ```

2. **Install SQL Server ODBC Driver**

   - Download from: https://docs.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server
   - Install ODBC Driver 17 or 18 for SQL Server

3. **Configure Database Connection**
   - Edit `config.py` to match your SQL Server configuration
   - Update server name, authentication method, etc.

## Usage

### Step 1: Create Database Schema

Run the SQL script to create the database and table structure:

```sql
-- Option 1: Run in SQL Server Management Studio
-- Open and execute: create_database_schema.sql

-- Option 2: Run via command line (if sqlcmd is available)
sqlcmd -S localhost -E -i create_database_schema.sql
```

### Step 2: Run Data Migration

Execute the Python migration script:

```bash
python migrate_data.py
```

### Step 3: Verify Migration

The script will automatically verify the migration and generate a summary report.

## Configuration

### Database Configuration (`config.py`)

```python
DATABASE_CONFIG = {
    'server': 'localhost',  # Your SQL Server instance
    'database': 'StartupDB',
    'driver': 'ODBC Driver 17 for SQL Server',
    'trusted_connection': True,  # Windows Authentication
    'username': '',  # Only for SQL Server Authentication
    'password': '',  # Only for SQL Server Authentication
}
```

### Migration Settings

```python
MIGRATION_CONFIG = {
    'batch_size': 1000,
    'table_name': 'Companies',
    'schema': 'dbo',
    'if_exists': 'replace',  # 'replace', 'append', or 'fail'
    'chunksize': 1000,
}
```

## Database Schema

### Companies Table Structure

| Column          | Type          | Description               |
| --------------- | ------------- | ------------------------- |
| CompanyID       | INT IDENTITY  | Primary key               |
| Name            | NVARCHAR(255) | Company name              |
| HomepageURL     | NVARCHAR(500) | Company website           |
| CategoryList    | NVARCHAR(500) | Business categories       |
| FundingTotalUSD | DECIMAL(18,2) | Total funding amount      |
| Status          | NVARCHAR(50)  | Company status            |
| CountryCode     | NVARCHAR(10)  | Country code              |
| StateCode       | NVARCHAR(50)  | State/province code       |
| City            | NVARCHAR(100) | City location             |
| FundingRounds   | INT           | Number of funding rounds  |
| FoundedAt       | DATE          | Founded date              |
| FirstFundingAt  | DATE          | First funding date        |
| LastFundingAt   | DATE          | Last funding date         |
| CreatedAt       | DATETIME2     | Record creation timestamp |
| UpdatedAt       | DATETIME2     | Record update timestamp   |

### Indexes Created

- `IX_Companies_Name` - On company name
- `IX_Companies_CountryCode` - On country code
- `IX_Companies_Status` - On company status
- `IX_Companies_CategoryList` - On category list

### Views Created

- `vw_CompanySummary` - Summary view with calculated fields

## Sample Queries

After migration, you can run these sample queries:

```sql
-- 1. Count total companies
SELECT COUNT(*) as TotalCompanies FROM dbo.Companies;

-- 2. Top 10 companies by funding
SELECT TOP 10 Name, FundingTotalUSD, CategoryList, CountryCode
FROM dbo.Companies
ORDER BY FundingTotalUSD DESC;

-- 3. Companies by country
SELECT CountryCode, COUNT(*) as CompanyCount
FROM dbo.Companies
GROUP BY CountryCode
ORDER BY CompanyCount DESC;

-- 4. Average funding by category
SELECT CategoryList, AVG(FundingTotalUSD) as AvgFunding, COUNT(*) as CompanyCount
FROM dbo.Companies
WHERE CategoryList IS NOT NULL
GROUP BY CategoryList
HAVING COUNT(*) >= 10
ORDER BY AvgFunding DESC;

-- 5. Funding trends by year
SELECT
    YEAR(FirstFundingAt) as FundingYear,
    COUNT(*) as CompaniesCount,
    AVG(FundingTotalUSD) as AvgFunding,
    SUM(FundingTotalUSD) as TotalFunding
FROM dbo.Companies
WHERE FirstFundingAt IS NOT NULL
GROUP BY YEAR(FirstFundingAt)
ORDER BY FundingYear DESC;

-- 6. Use the summary view
SELECT * FROM dbo.vw_CompanySummary
WHERE FundingCategory = 'Unicorn (1B+)'
ORDER BY FundingTotalUSD DESC;
```

## Troubleshooting

### Common Issues

1. **ODBC Driver Not Found**

   - Install ODBC Driver 17 or 18 for SQL Server
   - Update driver name in `config.py`

2. **Connection Failed**

   - Check SQL Server is running
   - Verify server name and authentication method
   - Ensure Windows Authentication is enabled (if using trusted connection)

3. **Permission Denied**

   - Ensure user has CREATE DATABASE permissions
   - Grant db_owner role for the StartupDB database

4. **Large Dataset Issues**
   - Reduce `chunksize` in configuration
   - Increase SQL Server memory allocation
   - Consider using `if_exists='append'` for incremental loads

### Log Files

- `migration.log` - Detailed migration log
- `migration_summary_report.txt` - Summary report

## File Structure

```
data_migration_into_db/
├── cleaned_startup_data.csv      # Source data file
├── config.py                     # Configuration settings
├── migrate_data.py              # Main migration script
├── create_database_schema.sql   # Database schema creation
├── requirements.txt             # Python dependencies
├── README.md                    # This documentation
├── migration.log               # Migration log (generated)
└── migration_summary_report.txt # Summary report (generated)
```

## Data Quality Features

- **Validation**: Checks for missing critical data
- **Cleaning**: Handles invalid data types and formats
- **Transformation**: Maps CSV columns to database schema
- **Verification**: Confirms successful migration
- **Reporting**: Generates detailed summary reports

## Performance Considerations

- Uses chunked processing for large datasets
- Implements batch inserts for efficiency
- Creates appropriate indexes for query performance
- Supports incremental loading with append mode

## Security Notes

- Uses Windows Authentication by default
- Supports SQL Server Authentication if needed
- Logs are created with appropriate permissions
- Connection strings are parameterized to prevent injection
