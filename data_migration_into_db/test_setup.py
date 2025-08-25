"""
Test script to verify migration setup and dependencies
Run this before attempting the actual migration
"""

import sys
import os

def test_imports():
    """Test if all required packages are installed"""
    print("🧪 Testing Python package imports...")
    
    try:
        import pandas as pd
        print("✅ pandas imported successfully")
    except ImportError as e:
        print(f"❌ pandas import failed: {e}")
        return False
    
    try:
        import sqlalchemy
        print("✅ sqlalchemy imported successfully")
    except ImportError as e:
        print(f"❌ sqlalchemy import failed: {e}")
        return False
    
    try:
        import pyodbc
        print("✅ pyodbc imported successfully")
    except ImportError as e:
        print(f"❌ pyodbc import failed: {e}")
        return False
    
    return True

def test_odbc_drivers():
    """Test available ODBC drivers"""
    print("\n🔍 Checking available ODBC drivers...")
    
    try:
        import pyodbc
        drivers = pyodbc.drivers()
        
        sql_server_drivers = [d for d in drivers if 'SQL Server' in d]
        
        if sql_server_drivers:
            print("✅ SQL Server ODBC drivers found:")
            for driver in sql_server_drivers:
                print(f"   - {driver}")
            return True
        else:
            print("❌ No SQL Server ODBC drivers found")
            print("💡 Please install ODBC Driver 17 or 18 for SQL Server")
            return False
            
    except Exception as e:
        print(f"❌ Error checking ODBC drivers: {e}")
        return False

def test_csv_file():
    """Test if CSV file exists and is readable"""
    print("\n📄 Testing CSV file...")
    
    csv_path = "cleaned_startup_data.csv"
    
    if not os.path.exists(csv_path):
        print(f"❌ CSV file not found: {csv_path}")
        return False
    
    try:
        import pandas as pd
        df = pd.read_csv(csv_path, nrows=5)  # Read only first 5 rows for testing
        print(f"✅ CSV file readable")
        print(f"📊 Columns: {list(df.columns)}")
        print(f"📏 File has {len(pd.read_csv(csv_path))} rows")
        return True
        
    except Exception as e:
        print(f"❌ Error reading CSV file: {e}")
        return False

def test_config():
    """Test configuration file"""
    print("\n⚙️  Testing configuration...")
    
    try:
        from config import (
            get_connection_string, 
            get_pyodbc_connection_string,
            DATABASE_CONFIG,
            COLUMN_MAPPING
        )
        
        print("✅ Configuration imported successfully")
        print(f"📊 Database: {DATABASE_CONFIG['database']}")
        print(f"🖥️  Server: {DATABASE_CONFIG['server']}")
        print(f"🔐 Trusted Connection: {DATABASE_CONFIG['trusted_connection']}")
        print(f"🗂️  Column mappings: {len(COLUMN_MAPPING)} columns")
        
        # Test connection string generation
        conn_str = get_connection_string()
        pyodbc_str = get_pyodbc_connection_string()
        
        print("✅ Connection strings generated successfully")
        return True
        
    except Exception as e:
        print(f"❌ Configuration test failed: {e}")
        return False

def test_database_connection():
    """Test database connectivity (optional - requires SQL Server to be running)"""
    print("\n🔌 Testing database connection...")
    print("⚠️  This test requires SQL Server to be running and accessible")
    
    try:
        from config import get_pyodbc_connection_string
        import pyodbc
        
        # Try to connect to master database first
        conn_str = get_pyodbc_connection_string().replace('DATABASE=StartupDB;', 'DATABASE=master;')
        
        with pyodbc.connect(conn_str, timeout=5) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT 1")
            print("✅ Database connection successful")
            return True
            
    except Exception as e:
        if 'pyodbc' in str(type(e)):
            print(f"⚠️  Database connection failed: {e}")
            print("💡 This is expected if SQL Server is not running or not configured")
            print("💡 You can still proceed with the migration setup")
        else:
            print(f"⚠️  Database connection test error: {e}")
        return False

def display_next_steps():
    """Display next steps for the user"""
    print("\n" + "="*60)
    print("📋 NEXT STEPS")
    print("="*60)
    print()
    print("1. 🗄️  Setup SQL Server:")
    print("   - Ensure SQL Server is installed and running")
    print("   - Verify you have appropriate permissions")
    print()
    print("2. 🏗️  Create Database Schema:")
    print("   - Option A: Run in SSMS:")
    print("     Open and execute: create_database_schema.sql")
    print("   - Option B: Command line (if sqlcmd available):")
    print("     sqlcmd -S localhost -E -i create_database_schema.sql")
    print()
    print("3. 🚀 Run Migration:")
    print("   - Quick test: python main.py")
    print("   - Full migration: python migrate_data.py")
    print()
    print("4. ✅ Verify Results:")
    print("   - Check migration.log for details")
    print("   - Review migration_summary_report.txt")
    print("   - Run sample queries from README.md")

def main():
    """Run all tests"""
    print("🧪 MIGRATION SETUP TEST SUITE")
    print("="*50)
    
    tests_passed = 0
    total_tests = 5
    
    # Test 1: Package imports
    if test_imports():
        tests_passed += 1
    
    # Test 2: ODBC drivers
    if test_odbc_drivers():
        tests_passed += 1
    
    # Test 3: CSV file
    if test_csv_file():
        tests_passed += 1
    
    # Test 4: Configuration
    if test_config():
        tests_passed += 1
    
    # Test 5: Database connection (optional)
    if test_database_connection():
        tests_passed += 1
    
    # Summary
    print("\n" + "="*50)
    print("📊 TEST SUMMARY")
    print("="*50)
    print(f"Tests passed: {tests_passed}/{total_tests}")
    
    if tests_passed >= 4:  # Database connection is optional
        print("✅ Setup looks good! Ready for migration.")
        display_next_steps()
    else:
        print("❌ Some tests failed. Please fix the issues before proceeding.")
        print("💡 Check the error messages above for guidance.")

if __name__ == "__main__":
    main()