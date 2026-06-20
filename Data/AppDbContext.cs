using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Models.Core;

namespace DACS.Data; 
public class AppDbContext : IdentityDbContext<Account, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<TenantRoom> TenantRooms { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<ContractDetail> ContractDetails { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ContractService> ContractServices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>()
            .HasOne(t => t.Account)
            .WithOne(a => a.Tenant)
            .HasForeignKey<Tenant>(t => t.AccountId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Invoice>().Property(i => i.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceDetail>().Property(id => id.Quantity).HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceDetail>().Property(id => id.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceDetail>().Property(id => id.SubTotal).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Room>().Property(r => r.MonthlyPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Service>().Property(s => s.Price).HasPrecision(18, 2);
        
        modelBuilder.Entity<ContractService>().Property(cs => cs.PriceAtContract).HasPrecision(18, 2);
    }
}