using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Infrastructure.Persistence;

public class ChairlyDbContext(DbContextOptions<ChairlyDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingService> BookingServices => Set<BookingService>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChairlyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
