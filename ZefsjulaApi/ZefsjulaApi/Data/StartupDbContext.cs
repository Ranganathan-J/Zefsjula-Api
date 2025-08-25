using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZefsjulaApi.Models;

namespace ZefsjulaApi.Data;

public partial class StartupDbContext : IdentityDbContext<User, Role, int>
{
    public StartupDbContext()
    {
    }

    public StartupDbContext(DbContextOptions<StartupDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Company> Companies { get; set; }
    public virtual DbSet<VwCompanySummary> VwCompanySummaries { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=StartupDB;Trusted_Connection=true;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Important: Call base method for Identity

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.CategoryList).IsUnicode(false);
            entity.Property(e => e.City).IsUnicode(false);
            entity.Property(e => e.CountryCode).IsUnicode(false);
            entity.Property(e => e.FundingTotalUsd).HasColumnName("FundingTotalUSD");
            entity.Property(e => e.HomepageUrl)
                .IsUnicode(false)
                .HasColumnName("HomepageURL");
            entity.Property(e => e.Name).IsUnicode(false);
            entity.Property(e => e.StateCode).IsUnicode(false);
            entity.Property(e => e.Status).IsUnicode(false);
        });

        modelBuilder.Entity<VwCompanySummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CompanySummary");

            entity.Property(e => e.CategoryList).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CompanyId)
                .ValueGeneratedOnAdd()
                .HasColumnName("CompanyID");
            entity.Property(e => e.CountryCode).HasMaxLength(10);
            entity.Property(e => e.FundingCategory)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.FundingTotalUsd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("FundingTotalUSD");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        modelBuilder
        .Entity<Company>()
        .HasNoKey()              // ← this stops EF Core from demanding a PK
        .ToTable("Companies");

        // Seed default roles
        SeedRoles(modelBuilder);

        OnModelCreatingPartial(modelBuilder);
    }

    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin", NormalizedName = "ADMIN" },
            new Role { Id = 2, Name = "User", NormalizedName = "USER" },
            new Role { Id = 3, Name = "Manager", NormalizedName = "MANAGER" }
        );
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
