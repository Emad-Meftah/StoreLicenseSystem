using Microsoft.EntityFrameworkCore;
using StoreLicense.Core.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace StoreLicense.Infrastructure.Data;
public class AppDbContext : DbContext, IDataProtectionKeyContext
{

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Subscriber> Subscribers => Set<Subscriber>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<ActivationCode> ActivationCodes => Set<ActivationCode>();

    public DbSet<SubscriberDevice> SubscriberDevices => Set<SubscriberDevice>();

    public DbSet<LicenseAuditLog> LicenseAuditLogs => Set<LicenseAuditLog>();

    public DbSet<SubscriptionRenewal> SubscriptionRenewals => Set<SubscriptionRenewal>();

    public DbSet<LicenseValidationNonce> LicenseValidationNonces
    => Set<LicenseValidationNonce>();

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureSubscribers(modelBuilder);
        ConfigureSubscriptionPlans(modelBuilder);
        ConfigureSubscriptions(modelBuilder);
        ConfigureActivationCodes(modelBuilder);
        ConfigureSubscriberDevices(modelBuilder);
        ConfigureLicenseAuditLogs(modelBuilder);
        ConfigureSubscriptionRenewals(modelBuilder);
        ConfigureLicenseValidationNonces(modelBuilder);
        ConfigureAdminUsers(modelBuilder);
    }

    private static void ConfigureSubscribers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscriber>(entity =>
        {
            entity.ToTable("subscribers");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.StoreName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.OwnerName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Phone)
                .HasMaxLength(30);

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.HasIndex(x => x.Status);

            entity.HasIndex(x => x.Phone);

            entity.HasMany(x => x.Subscriptions)
                .WithOne(x => x.Subscriber)
                .HasForeignKey(x => x.SubscriberId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Devices)
                .WithOne(x => x.Subscriber)
                .HasForeignKey(x => x.SubscriberId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.AuditLogs)
                .WithOne(x => x.Subscriber)
                .HasForeignKey(x => x.SubscriberId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSubscriptionPlans(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("subscription_plans", table =>
            {
                table.HasCheckConstraint(
                    "CK_subscription_plans_duration",
                    """
                    ("IsLifetime" = TRUE AND "DurationDays" IS NULL)
                    OR
                    ("IsLifetime" = FALSE AND "DurationDays" > 0)
                    """
                );

                table.HasCheckConstraint(
                    "CK_subscription_plans_max_devices",
                    "\"DefaultMaxDevices\" > 0"
                );
            });

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.HasIndex(x => x.IsActive);
        });
    }

    private static void ConfigureSubscriptions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions", table =>
            {
                table.HasCheckConstraint(
                    "CK_subscriptions_max_devices",
                    "\"MaxDevices\" > 0"
                );

                table.HasCheckConstraint(
                    "CK_subscriptions_dates",
                    """
                    (
                        "IsLifetime" = TRUE
                        AND "DurationDays" IS NULL
                        AND "EndDate" IS NULL
                    )
                    OR
                    (
                        "IsLifetime" = FALSE
                        AND "DurationDays" > 0
                        AND
                        (
                            (
                                "StartDate" IS NULL
                                AND "EndDate" IS NULL
                            )
                            OR
                            (
                                "StartDate" IS NOT NULL
                                AND "EndDate" IS NOT NULL
                                AND "EndDate" > "StartDate"
                            )
                        )
                    )
                    """
                );
            });

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.HasAlternateKey(x => new
            {
                x.Id,
                x.SubscriberId
            });

            entity.HasIndex(x => x.SubscriberId);

            entity.HasIndex(x => x.Status);

            entity.HasIndex(x => new
            {
                x.Status,
                x.EndDate
            });

            entity.HasOne(x => x.Plan)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(x => x.ActivationCodes)
                .WithOne(x => x.Subscription)
                .HasForeignKey(x => x.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Renewals)
                .WithOne(x => x.Subscription)
                .HasForeignKey(x => x.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureActivationCodes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivationCode>(entity =>
        {
            entity.ToTable("activation_codes");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.CodeHash)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.HasIndex(x => x.CodeHash)
                .IsUnique();

            entity.HasIndex(x => x.SubscriptionId);

            entity.HasIndex(x => x.Status);
        });
    }

    private static void ConfigureSubscriberDevices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubscriberDevice>(entity =>
        {
            entity.ToTable("subscriber_devices");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.DeviceName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.DevicePublicKey)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(x => x.DeviceIdentifier)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.HasIndex(x => x.DevicePublicKey)
                .IsUnique()
                .HasFilter("\"Status\" = 1");

            entity.HasIndex(x => new
            {
                x.SubscriptionId,
                x.DeviceIdentifier
            }).IsUnique();

            entity.HasIndex(x => new
            {
                x.SubscriptionId,
                x.Status
            });

            // التأكد من أن الاشتراك والجهاز يعودان لنفس المشترك.
            entity.HasOne(x => x.Subscription)
                .WithMany(x => x.Devices)
                .HasForeignKey(x => new
                {
                    x.SubscriptionId,
                    x.SubscriberId
                })
                .HasPrincipalKey(x => new
                {
                    x.Id,
                    x.SubscriberId
                })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureLicenseAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LicenseAuditLog>(entity =>
        {
            entity.ToTable("license_audit_logs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Details)
                .HasMaxLength(4000);

            entity.Property(x => x.IpAddress)
                .HasMaxLength(45);

            entity.HasIndex(x => x.SubscriberId);

            entity.HasIndex(x => x.SubscriptionId);

            entity.HasIndex(x => x.CreatedAt);

            entity.HasOne(x => x.Subscription)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => new
                {
                    x.SubscriptionId,
                    x.SubscriberId
                })
                .HasPrincipalKey(x => new
                {
                    x.Id,
                    x.SubscriberId
                })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSubscriptionRenewals(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubscriptionRenewal>(entity =>
        {
            entity.ToTable("subscription_renewals");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.HasIndex(x => x.SubscriptionId);

            entity.HasIndex(x => x.RenewedAt);
        });
    }

    private static void ConfigureLicenseValidationNonces(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LicenseValidationNonce>(entity =>
        {
            entity.ToTable("license_validation_nonces");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.DeviceId,
                x.Nonce
            }).IsUnique();

            entity.HasIndex(x => x.ExpiresAt);

            entity.HasOne(x => x.Device)
                .WithMany()
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }



    private static void ConfigureAdminUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("admin_users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(254);

            entity.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(x => x.Email)
                .IsUnique();
        });
    }
}