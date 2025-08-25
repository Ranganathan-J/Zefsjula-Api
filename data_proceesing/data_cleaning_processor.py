import pandas as pd
import numpy as np
import re

def clean_startup_data(input_file='big_startup_secsees_dataset.csv', output_file='cleaned_startup_data.csv'):
    """
    Clean and process startup dataset according to specifications:
    1. Remove columns: region, permalink
    2. Exclude rows with blank funding_total_usd or first_funding_at
    3. Exclude bank-related companies
    4. Save cleaned data to new CSV
    """
    
    print("Loading dataset...")
    df = pd.read_csv(input_file)
    print(f"Original dataset shape: {df.shape}")
    print(f"Original columns: {list(df.columns)}")
    
    # Step 1: Remove specified columns
    print("\n1. Removing specified columns...")
    columns_to_remove = ['region', 'permalink']
    
    # Check which columns exist before removing
    existing_columns_to_remove = [col for col in columns_to_remove if col in df.columns]
    missing_columns = [col for col in columns_to_remove if col not in df.columns]
    
    if missing_columns:
        print(f"Warning: These columns were not found: {missing_columns}")
    
    if existing_columns_to_remove:
        df = df.drop(columns=existing_columns_to_remove)
        print(f"Removed columns: {existing_columns_to_remove}")
    
    print(f"Shape after column removal: {df.shape}")
    
    # Step 2: Filter out rows with blank funding_total_usd or funding dates
    print("\n2. Filtering rows with blank funding data...")
    
    # Count blanks before filtering
    blank_funding_total = df['funding_total_usd'].isnull().sum()
    blank_first_funding = df['first_funding_at'].isnull().sum()
    blank_last_funding = df['last_funding_at'].isnull().sum()
    
    print(f"Blank funding_total_usd: {blank_funding_total}")
    print(f"Blank first_funding_at: {blank_first_funding}")
    print(f"Blank last_funding_at: {blank_last_funding}")
    
    # Filter out rows where funding_total_usd OR first_funding_at is blank
    initial_rows = len(df)
    df = df.dropna(subset=['funding_total_usd', 'first_funding_at'])
    rows_removed_funding = initial_rows - len(df)
    print(f"Rows removed due to blank funding data: {rows_removed_funding}")
    print(f"Shape after funding filter: {df.shape}")
    
    # Step 3: Exclude bank-related companies
    print("\n3. Excluding bank-related companies...")
    
    # Define bank-related keywords to search for
    bank_keywords = [
        'bank', 'banking', 'financial services', 'credit union', 'mortgage',
        'lending', 'loan', 'finance', 'investment banking', 'commercial banking',
        'retail banking', 'private banking', 'wealth management', 'credit',
        'savings', 'deposits', 'fintech', 'financial technology'
    ]
    
    # Function to check if company is bank-related
    def is_bank_related(row):
        # Check in company name
        if pd.notna(row['name']):
            name_lower = str(row['name']).lower()
            if any(keyword in name_lower for keyword in bank_keywords):
                return True
        
        # Check in category_list
        if pd.notna(row['category_list']):
            category_lower = str(row['category_list']).lower()
            if any(keyword in category_lower for keyword in bank_keywords):
                return True
        
        return False
    
    # Apply filter
    initial_rows = len(df)
    bank_mask = df.apply(is_bank_related, axis=1)
    bank_companies_count = bank_mask.sum()
    
    print(f"Bank-related companies found: {bank_companies_count}")
    
    # Show some examples of bank-related companies before removing
    if bank_companies_count > 0:
        print("\nExamples of bank-related companies being removed:")
        bank_examples = df[bank_mask][['name', 'category_list']].head(5)
        for idx, row in bank_examples.iterrows():
            print(f"  - {row['name']} | Categories: {row['category_list']}")
    
    # Remove bank-related companies
    df = df[~bank_mask]
    print(f"Rows removed (bank-related): {bank_companies_count}")
    print(f"Shape after bank filter: {df.shape}")
    
    # Step 4: Additional data cleaning
    print("\n4. Additional data cleaning...")
    
    # Remove rows with zero funding
    zero_funding_mask = (df['funding_total_usd'] == 0)
    zero_funding_count = zero_funding_mask.sum()
    if zero_funding_count > 0:
        df = df[~zero_funding_mask]
        print(f"Removed {zero_funding_count} rows with zero funding")
    
    # Clean and standardize data types
    df['funding_total_usd'] = pd.to_numeric(df['funding_total_usd'], errors='coerce')
    df['funding_rounds'] = pd.to_numeric(df['funding_rounds'], errors='coerce')
    
    # Convert date columns to datetime
    date_columns = ['founded_at', 'first_funding_at', 'last_funding_at']
    for col in date_columns:
        if col in df.columns:
            df[col] = pd.to_datetime(df[col], errors='coerce')
    
    # Step 5: Final data summary
    print("\n5. Final data summary...")
    print(f"Final dataset shape: {df.shape}")
    print(f"Final columns: {list(df.columns)}")
    
    # Show statistics
    print(f"\nFunding statistics:")
    print(f"  - Average funding: ${df['funding_total_usd'].mean():,.2f}")
    print(f"  - Median funding: ${df['funding_total_usd'].median():,.2f}")
    print(f"  - Max funding: ${df['funding_total_usd'].max():,.2f}")
    print(f"  - Min funding: ${df['funding_total_usd'].min():,.2f}")
    
    print(f"\nTop categories:")
    if 'category_list' in df.columns:
        top_categories = df['category_list'].value_counts().head(10)
        print(top_categories)
    
    print(f"\nTop countries:")
    if 'country_code' in df.columns:
        top_countries = df['country_code'].value_counts().head(10)
        print(top_countries)
    
    # Step 6: Save cleaned data
    print(f"\n6. Saving cleaned data to {output_file}...")
    df.to_csv(output_file, index=False)
    print(f"Cleaned data saved successfully!")
    
    # Create a summary report
    summary_report = f"""
    DATA CLEANING SUMMARY REPORT
    ============================
    
    Original dataset: {input_file}
    Cleaned dataset: {output_file}
    
    Processing Steps:
    1. Removed columns: {existing_columns_to_remove}
    2. Filtered out {rows_removed_funding} rows with blank funding data
    3. Removed {bank_companies_count} bank-related companies
    4. Additional cleaning: Removed {zero_funding_count} rows with zero funding
    
    Results:
    - Original rows: {pd.read_csv(input_file).shape[0]:,}
    - Final rows: {df.shape[0]:,}
    - Rows removed: {pd.read_csv(input_file).shape[0] - df.shape[0]:,}
    - Data reduction: {((pd.read_csv(input_file).shape[0] - df.shape[0]) / pd.read_csv(input_file).shape[0] * 100):.1f}%
    
    Final dataset contains:
    - Companies with valid funding data
    - Non-bank/financial services companies
    - {df.shape[1]} columns: {', '.join(df.columns)}
    """
    
    # Save summary report
    with open('data_cleaning_report.txt', 'w') as f:
        f.write(summary_report)
    
    print(summary_report)
    print("\nSummary report saved to 'data_cleaning_report.txt'")
    
    return df

# Additional utility functions
def analyze_removed_data(input_file='big_startup_secsees_dataset.csv'):
    """Analyze what data would be removed by the cleaning process"""
    print("Analyzing data that will be removed...")
    
    df = pd.read_csv(input_file)
    
    # Analyze blank funding data
    blank_funding = df[df['funding_total_usd'].isnull() | df['first_funding_at'].isnull()]
    print(f"\nRows with blank funding data: {len(blank_funding)}")
    
    if len(blank_funding) > 0:
        print("Sample companies with blank funding:")
        print(blank_funding[['name', 'category_list', 'funding_total_usd', 'first_funding_at']].head())
    
    # Analyze bank-related companies
    bank_keywords = ['bank', 'banking', 'financial services', 'fintech', 'lending', 'loan']
    
    def is_bank_related(row):
        if pd.notna(row['name']):
            name_lower = str(row['name']).lower()
            if any(keyword in name_lower for keyword in bank_keywords):
                return True
        if pd.notna(row['category_list']):
            category_lower = str(row['category_list']).lower()
            if any(keyword in category_lower for keyword in bank_keywords):
                return True
        return False
    
    bank_companies = df[df.apply(is_bank_related, axis=1)]
    print(f"\nBank-related companies: {len(bank_companies)}")
    
    if len(bank_companies) > 0:
        print("Sample bank-related companies:")
        print(bank_companies[['name', 'category_list']].head())

if __name__ == "__main__":
    # Run the cleaning process
    print("=== STARTUP DATA CLEANING PROCESSOR ===\n")
    
    # Optional: Analyze what will be removed first
    # analyze_removed_data()
    
    # Clean the data
    cleaned_df = clean_startup_data()
    
    print("\n=== CLEANING COMPLETED SUCCESSFULLY ===")