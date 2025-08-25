import requests
from bs4 import BeautifulSoup
from googlesearch import search
import csv
import pandas as pd
import time
import random
from urllib.parse import urljoin, urlparse

def get_craft_url(company_name):
    """Find Craft.co URL for a given company name"""
    try:
        query = f"{company_name} site:craft.co"
        time.sleep(random.uniform(1, 3))  # Random delay to avoid being blocked
        for result in search(query, num_results=5):
            if "craft.co" in result:
                return result
    except Exception as e:
        print(f"Error searching for {company_name}: {e}")
    return None

def parse_craft_data(url):
    """Parse company data from Craft.co page"""
    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
    }
    try:
        response = requests.get(url, headers=headers, timeout=10)
        response.raise_for_status()
        soup = BeautifulSoup(response.text, 'html.parser')
    except Exception as e:
        print(f"Error fetching data from {url}: {e}")
        return {"Error": f"Failed to fetch data: {e}"}

    def get_info(label):
        try:
            element = soup.find("div", string=label)
            return element.find_next_sibling("div").get_text(strip=True)
        except:
            return "N/A"

    data = {
        "Company Name": soup.find("h1").text.strip() if soup.find("h1") else "N/A",
        "Founded Year": get_info("Founded"),
        "HQ": get_info("Headquarters"),
        "Industry": get_info("Industry"),
        "Total Funding": get_info("Total Funding"),
        "Valuation": get_info("Valuation"),
        "Employees": get_info("Employees"),
    }
    return data

def get_g2_rating(company_name):
    """Get G2 rating for a company"""
    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
    }
    try:
        query = f"{company_name} site:g2.com"
        time.sleep(random.uniform(1, 3))  # Random delay
        for result in search(query, num_results=5):
            if "g2.com/products" in result:
                g2_url = result
                break
        else:
            return "N/A"

        response = requests.get(g2_url, headers=headers, timeout=10)
        response.raise_for_status()
        soup = BeautifulSoup(response.text, 'html.parser')
        rating_elem = soup.find("span", class_="screen-reader-only")
        if rating_elem:
            return rating_elem.get_text(strip=True)
    except Exception as e:
        print(f"Error getting G2 rating for {company_name}: {e}")
        return "N/A"
    return "N/A"

def get_company_info(company_name):
    craft_url = get_craft_url(company_name)
    if not craft_url:
        return {"Error": "Company not found on Craft.co"}

    craft_data = parse_craft_data(craft_url)
    g2_rating = get_g2_rating(company_name)
    craft_data["G2 Rating"] = g2_rating
    return craft_data

def scrape_companies_to_csv(company_names, output_file='company_data.csv'):
    """Scrape multiple companies and save to CSV"""
    all_data = []
    
    print(f"Starting to scrape {len(company_names)} companies...")
    
    for i, company in enumerate(company_names, 1):
        if not company or company.strip() == "":
            print(f"Skipping empty company name at position {i}")
            continue
            
        print(f"Processing {i}/{len(company_names)}: {company}")
        company_data = get_company_info(company.strip())
        
        # Add the company name as searched to track what we looked for
        company_data['Searched_Company_Name'] = company.strip()
        all_data.append(company_data)
        
        # Add delay between requests to be respectful
        if i < len(company_names):
            time.sleep(random.uniform(2, 5))
    
    # Create DataFrame and save to CSV
    if all_data:
        df = pd.DataFrame(all_data)
        
        # Reorder columns to put searched name first
        cols = ['Searched_Company_Name'] + [col for col in df.columns if col != 'Searched_Company_Name']
        df = df[cols]
        
        df.to_csv(output_file, index=False, encoding='utf-8')
        print(f"\nData saved to {output_file}")
        print(f"Successfully scraped {len(all_data)} companies")
        
        # Display summary
        print("\nSummary:")
        print(df.to_string(max_rows=10, max_cols=8))
        
        return df
    else:
        print("No data was scraped.")
        return None

def save_to_csv_basic(data_list, filename='company_data.csv'):
    """Save data to CSV using basic csv module"""
    if not data_list:
        print("No data to save")
        return
    
    fieldnames = set()
    for data in data_list:
        fieldnames.update(data.keys())
    
    fieldnames = list(fieldnames)
    
    with open(filename, 'w', newline='', encoding='utf-8') as csvfile:
        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(data_list)
    
    print(f"Data saved to {filename}")

# Example usage
if __name__ == "__main__":
    # List of companies to scrape - you can modify this list
    companies = [
        "Airtable",
        "Notion", 
        "Slack",
        "Zoom",
        "Stripe"
    ]
    
    # Method 1: Using pandas (recommended)
    print("=== Using Pandas for CSV Export ===")
    df = scrape_companies_to_csv(companies, 'companies_data_pandas.csv')
    
    # Method 2: Using basic csv module
    print("\n=== Using Basic CSV Module ===")
    basic_data = []
    for company in companies[:2]:  # Just first 2 for demo
        if company.strip():
            data = get_company_info(company)
            data['Searched_Company_Name'] = company
            basic_data.append(data)
            time.sleep(2)
    
    save_to_csv_basic(basic_data, 'companies_data_basic.csv')
