using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tarot.Core.Entities;

namespace Tarot.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Service> Services { get; set; } = null!;
    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<Consultation> Consultations { get; set; } = null!;
    public DbSet<Card> Cards { get; set; } = null!;
    public DbSet<DailyDrawRecord> DailyDrawRecords { get; set; } = null!;
    public DbSet<BlogPost> BlogPosts { get; set; } = null!;
    public DbSet<SiteSetting> SiteSettings { get; set; } = null!;
    public DbSet<EmailTemplate> EmailTemplates { get; set; } = null!;
    public DbSet<ContactMessage> ContactMessages { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<BlockedSlot> BlockedSlots { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Global Query Filter for Soft Delete
        builder.Entity<Service>().HasQueryFilter(e => e.DeletedAt == null);
        builder.Entity<Appointment>().HasQueryFilter(e => e.DeletedAt == null);
        builder.Entity<Consultation>().HasQueryFilter(e => e.DeletedAt == null);
        builder.Entity<Card>().HasQueryFilter(e => e.DeletedAt == null);
        builder.Entity<DailyDrawRecord>().HasQueryFilter(e => e.DeletedAt == null);
        builder.Entity<BlogPost>().HasQueryFilter(e => e.DeletedAt == null);
        builder.Entity<SiteSetting>().HasQueryFilter(e => e.DeletedAt == null);
        builder.Entity<EmailTemplate>().HasQueryFilter(e => e.DeletedAt == null);
        builder.Entity<ContactMessage>().HasQueryFilter(e => e.DeletedAt == null);
        builder.Entity<AuditLog>().HasQueryFilter(e => e.DeletedAt == null);

        // Configure JSONB columns (PostgreSQL specific)
        // Note: In EF Core 8/9, we can map JSON columns directly or use HasColumnType("jsonb")
        // For simplicity in this phase, we treat them as strings with "jsonb" type hint for migration.

        builder.Entity<AppUser>(b =>
        {
            b.Property(u => u.Permissions).HasColumnType("jsonb");
            b.Property(u => u.Tags).HasColumnType("jsonb");
        });

        builder.Entity<Consultation>(b =>
        {
            b.Property(c => c.UserImages).HasColumnType("jsonb");
            b.Property(c => c.ReplyImages).HasColumnType("jsonb");
        });

        builder.Entity<Card>(b =>
        {
            b.Property(c => c.Keywords).HasColumnType("jsonb");
        });
        
        builder.Entity<BlogPost>(b =>
        {
            b.Property(p => p.SeoMeta).HasColumnType("jsonb");
            b.HasIndex(p => p.Slug).IsUnique();
        });

        builder.Entity<SiteSetting>(b =>
        {
            b.Property(s => s.Value).HasColumnType("jsonb");
        });

        builder.Entity<EmailTemplate>(b =>
        {
            b.HasIndex(e => e.Slug).IsUnique();
        });

        // Relationships
        builder.Entity<Appointment>()
            .HasOne(a => a.Consultation)
            .WithOne(c => c.Appointment)
            .HasForeignKey<Consultation>(c => c.AppointmentId);
    }
}
