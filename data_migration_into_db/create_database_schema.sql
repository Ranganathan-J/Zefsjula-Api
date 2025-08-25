-- SQL Server Database Schema for Startup Data Migration
-- Run this script first to create the database and table structure

-- Create Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'StartupDB')
BEGIN
    CREATE DATABASE StartupDB;
    PRINT 'Database StartupDB created successfully';
END
ELSE
BEGIN
    PRINT 'Database StartupDB already exists';
END

-- Use the database
USE StartupDB;
GO

-- Create Companies table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Companies' AND xtype='U')
BEGIN
    CREATE TABLE dbo.Companies (
        CompanyID INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(255) NOT NULL,
        HomepageURL NVARCHAR(500) NULL,
        CategoryList NVARCHAR(500) NULL,
        FundingTotalUSD DECIMAL(18,2) NULL,
        Status NVARCHAR(50) NULL,
        CountryCode NVARCHAR(10) NULL,
        StateCode NVARCHAR(50) NULL,
        City NVARCHAR(100) NULL,
        FundingRounds INT NULL,
        FoundedAt DATE NULL,
        FirstFundingAt DATE NULL,
        LastFundingAt DATE NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE()
    );
    
    PRINT 'Table Companies created successfully';
END
ELSE
BEGIN
    PRINT 'Table Companies already exists';
END

-- Create indexes for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Companies_Name')
BEGIN
    CREATE INDEX IX_Companies_Name ON dbo.Companies(Name);
    PRINT 'Index on Name created';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Companies_CountryCode')
BEGIN
    CREATE INDEX IX_Companies_CountryCode ON dbo.Companies(CountryCode);
    PRINT 'Index on CountryCode created';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Companies_Status')
BEGIN
    CREATE INDEX IX_Companies_Status ON dbo.Companies(Status);
    PRINT 'Index on Status created';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Companies_CategoryList')
BEGIN
    CREATE INDEX IX_Companies_CategoryList ON dbo.Companies(CategoryList);
    PRINT 'Index on CategoryList created';
END

-- Create a view for easy querying
IF NOT EXISTS (SELECT * FROM sys.views WHERE name='vw_CompanySummary')
BEGIN
    EXEC('
    CREATE VIEW dbo.vw_CompanySummary AS
    SELECT 
        CompanyID,
        Name,
        CategoryList,
        FundingTotalUSD,
        Status,
        CountryCode,
        City,
        FundingRounds,
        FoundedAt,
        FirstFundingAt,
        LastFundingAt,
        DATEDIFF(DAY, FirstFundingAt, LastFundingAt) AS DaysBetweenFirstLastFunding,
        CASE 
            WHEN FundingTotalUSD >= 1000000000 THEN ''Unicorn (1B+)''
            WHEN FundingTotalUSD >= 100000000 THEN ''Large (100M+)''
            WHEN FundingTotalUSD >= 10000000 THEN ''Medium (10M+)''
            WHEN FundingTotalUSD >= 1000000 THEN ''Small (1M+)''
            ELSE ''Seed (<1M)''
        END AS FundingCategory
    FROM dbo.Companies
    ');
    PRINT 'View vw_CompanySummary created';
END

-- Sample queries to test the structure
PRINT 'Database schema setup completed successfully!';
PRINT '';
PRINT 'Sample queries to run after data migration:';
PRINT '1. SELECT COUNT(*) FROM dbo.Companies;';
PRINT '2. SELECT TOP 10 * FROM dbo.vw_CompanySummary ORDER BY FundingTotalUSD DESC;';
PRINT '3. SELECT CountryCode, COUNT(*) as CompanyCount FROM dbo.Companies GROUP BY CountryCode ORDER BY CompanyCount DESC;';
PRINT '4. SELECT CategoryList, AVG(FundingTotalUSD) as AvgFunding FROM dbo.Companies WHERE CategoryList IS NOT NULL GROUP BY CategoryList ORDER BY AvgFunding DESC;';

GO