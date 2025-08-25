# Visual Studio Entity Framework Scaffolding Guide

## Prerequisites Check

### 1. Verify EF Core Tools Installation

**Global Tools (Recommended):**
```bash
# Check if EF tools are installed
dotnet tool list -g

# Install or update EF tools globally
dotnet tool install --global dotnet-ef
# OR update existing
dotnet tool update --global dotnet-ef
```

### 2. Required NuGet Packages

Make sure your project has these packages:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
```

## Visual Studio Scaffolding Methods

### Method 1: Package Manager Console (PMC) - RECOMMENDED

1. **Open PMC:** `Tools` → `NuGet Package Manager` → `Package Manager Console`

2. **Set Default Project:** Make sure your API project is selected in the dropdown

3. **Run Scaffolding Command:**
```powershell
Scaffold-DbContext "Server=localhost;Database=StartupDB;Trusted_Connection=true;TrustServerCertificate=true;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Context StartupDbContext -ContextDir Data -Force
```

**PMC Command Options:**
```powershell
# Basic scaffolding
Scaffold-DbContext "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer

# With custom options
Scaffold-DbContext "Server=localhost;Database=StartupDB;Trusted_Connection=true;TrustServerCertificate=true;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Context StartupDbContext -ContextDir Data -Force -DataAnnotations

# Scaffold specific tables only
Scaffold-DbContext "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Tables Companies -Force
```

### Method 2: Visual Studio Terminal

1. **Open Terminal:** `View` → `Terminal` or `Ctrl + `` (backtick)

2. **Navigate to project directory:**
```bash
cd YourProjectName
```

3. **Run dotnet ef command:**
```bash
dotnet ef dbcontext scaffold "Server=localhost;Database=StartupDB;Trusted_Connection=true;TrustServerCertificate=true;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c StartupDbContext --context-dir Data --force
```

### Method 3: External Command Prompt

1. **Open Command Prompt as Administrator**

2. **Navigate to your solution/project folder:**
```bash
cd "C:\sanjay\projects\StartupAPI"
```

3. **Run scaffolding:**
```bash
dotnet ef dbcontext scaffold "Server=localhost;Database=StartupDB;Trusted_Connection=true;TrustServerCertificate=true;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c StartupDbContext --context-dir Data --force
```

## Expected Output

After successful scaffolding, you should see:

### Generated Files Structure:
```
YourProject/
├── Data/
│   └── StartupDbContext.cs
└── Models/
    └── Company.cs
```

### Sample Generated Company.cs:
```csharp
public partial class Company
{
    public int CompanyId { get; set; }
    public string Name { get; set; } = null!;
    public string? HomepageUrl { get; set; }
    public string? CategoryList { get; set; }
    public decimal? FundingTotalUsd { get; set; }
    public string? Status { get; set; }
    public string? CountryCode { get; set; }
    public string? StateCode { get; set; }
    public string? City { get; set; }
    public int? FundingRounds { get; set; }
    public DateTime? FoundedAt { get; set; }
    public DateTime? FirstFundingAt { get; set; }
    public DateTime? LastFundingAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Sample Generated StartupDbContext.cs:
```csharp
public partial class StartupDbContext : DbContext
{
    public StartupDbContext()
    {
    }

    public StartupDbContext(DbContextOptions<StartupDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Company> Companies { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=StartupDB;Trusted_Connection=true;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.CompanyId);

            entity.Property(e => e.CategoryList).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CountryCode).HasMaxLength(10);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FundingTotalUsd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("FundingTotalUSD");
            entity.Property(e => e.HomepageUrl)
                .HasMaxLength(500)
                .HasColumnName("HomepageURL");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.StateCode).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
```

## Troubleshooting Common Issues

### Issue 1: "dotnet ef command not found"
**Solution:**
```bash
# Install EF Core tools globally
dotnet tool install --global dotnet-ef
```

### Issue 2: "No DbContext was found"
**Solution:**
- Make sure you're in the correct project directory
- Verify EF Core packages are installed
- Check that the project builds successfully

### Issue 3: "Cannot connect to database"
**Solutions:**
- Verify SQL Server is running
- Check connection string
- Ensure Windows Authentication is working
- Try connecting via SQL Server Management Studio first

### Issue 4: "Access denied" or "Login failed"
**Solutions:**
- Run Visual Studio as Administrator
- Check SQL Server authentication settings
- Verify your Windows user has access to the database

### Issue 5: PMC shows "The term 'Scaffold-DbContext' is not recognized"
**Solution:**
```powershell
# In PMC, first run:
Update-Package Microsoft.EntityFrameworkCore.Tools
```

## Post-Scaffolding Steps

### 1. Clean up the generated DbContext
Remove the hardcoded connection string from `OnConfiguring` method:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    // Remove this method or make it empty
    // Connection string should come from DI
}
```

### 2. Update Program.cs registration
```csharp
builder.Services.AddDbContext<StartupDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### 3. Verify generated models match your database schema

### 4. Test the connection
```csharp
// In a controller or service
var companyCount = await _context.Companies.CountAsync();
```

## Command Reference

### PMC Commands (Package Manager Console):
```powershell
# Basic scaffold
Scaffold-DbContext "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer

# With options
Scaffold-DbContext "ConnectionString" Provider -OutputDir Models -Context MyContext -ContextDir Data -Force -DataAnnotations -UseDatabaseNames

# Specific tables
Scaffold-DbContext "ConnectionString" Provider -Tables Table1,Table2
```

### dotnet ef Commands (Terminal/CMD):
```bash
# Basic scaffold
dotnet ef dbcontext scaffold "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer

# With options  
dotnet ef dbcontext scaffold "ConnectionString" Provider -o Models -c MyContext --context-dir Data --force --data-annotations --use-database-names

# Specific tables
dotnet ef dbcontext scaffold "ConnectionString" Provider --table Companies --table Users
```

## Success Indicators

✅ **You'll know it worked when:**
- No error messages appear
- `Models/Company.cs` file is created
- `Data/StartupDbContext.cs` file is created  
- Files contain your database schema
- Project builds without errors

Choose the method that works best for your Visual Studio setup!