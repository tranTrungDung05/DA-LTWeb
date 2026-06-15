using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Models.Core;

namespace smart_hostel_management_system.Data;

public class AppDbContext : IdentityDbContext<AppUser, Role, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomAsset> RoomAssets => Set<RoomAsset>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantRoom> TenantRooms => Set<TenantRoom>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractFile> ContractFiles => Set<ContractFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Room>()
            .HasIndex(room => room.Code)
            .IsUnique();

        modelBuilder.Entity<Contract>()
            .HasIndex(contract => contract.ContractNumber)
            .IsUnique();

        modelBuilder.Entity<Property>()
            .HasMany(property => property.Rooms)
            .WithOne(room => room.Property)
            .HasForeignKey(room => room.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Room>()
            .HasMany(room => room.Assets)
            .WithOne(asset => asset.Room)
            .HasForeignKey(asset => asset.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Room>()
            .HasMany(room => room.TenantRooms)
            .WithOne(tenantRoom => tenantRoom.Room)
            .HasForeignKey(tenantRoom => tenantRoom.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tenant>()
            .HasMany(tenant => tenant.TenantRooms)
            .WithOne(tenantRoom => tenantRoom.Tenant)
            .HasForeignKey(tenantRoom => tenantRoom.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Room>()
            .HasMany(room => room.Contracts)
            .WithOne(contract => contract.Room)
            .HasForeignKey(contract => contract.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tenant>()
            .HasMany(tenant => tenant.Contracts)
            .WithOne(contract => contract.Tenant)
            .HasForeignKey(contract => contract.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Contract>()
            .HasMany(contract => contract.Files)
            .WithOne(file => file.Contract)
            .HasForeignKey(file => file.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Owner", NormalizedName = "OWNER", Description = "Chu tro" },
            new Role { Id = 2, Name = "Tenant", NormalizedName = "TENANT", Description = "Khach thue" },
            new Role { Id = 3, Name = "Admin", NormalizedName = "ADMIN", Description = "Quan tri he thong" });

        modelBuilder.Entity<Property>().HasData(
            new Property
            {
                Id = 1,
                Name = "Nha tro mac dinh",
                Address = "Cap nhat dia chi nha tro",
                PhoneNumber = "0900000000"
            });
    }
}
