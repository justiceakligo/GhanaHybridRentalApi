using Microsoft.EntityFrameworkCore;
using GhanaHybridRentalApi.Models;

namespace GhanaHybridRentalApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<OwnerProfile> OwnerProfiles => Set<OwnerProfile>();
    public DbSet<RenterProfile> RenterProfiles => Set<RenterProfile>();
    public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<CarCategory> CarCategories => Set<CarCategory>();
    public DbSet<InsurancePlan> InsurancePlans => Set<InsurancePlan>();
    public DbSet<ProtectionPlan> ProtectionPlans => Set<ProtectionPlan>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<GlobalSetting> GlobalSettings => Set<GlobalSetting>();
    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<IntegrationPartner> IntegrationPartners => Set<IntegrationPartner>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<RegionalPricing> RegionalPricings => Set<RegionalPricing>();
    public DbSet<RefundPolicy> RefundPolicies => Set<RefundPolicy>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<ProfileChangeAudit> ProfileChangeAudits => Set<ProfileChangeAudit>();
    public DbSet<RentalAgreementTemplate> RentalAgreementTemplates => Set<RentalAgreementTemplate>();
    public DbSet<RentalAgreementAcceptance> RentalAgreementAcceptances => Set<RentalAgreementAcceptance>();
    public DbSet<PostRentalChargeType> PostRentalChargeTypes => Set<PostRentalChargeType>();
    public DbSet<BookingCharge> BookingCharges => Set<BookingCharge>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<PartnerPhoto> PartnerPhotos => Set<PartnerPhoto>();
    public DbSet<PartnerClick> PartnerClicks => Set<PartnerClick>();

    // Notifications for renters
    public DbSet<Notification> Notifications => Set<Notification>();

    // Notification jobs scheduler
    public DbSet<NotificationJob> NotificationJobs => Set<NotificationJob>();

    // Deposit refunds and instant withdrawals
    public DbSet<DepositRefund> DepositRefunds => Set<DepositRefund>();
    public DbSet<RefundAuditLog> RefundAuditLogs => Set<RefundAuditLog>();
    public DbSet<InstantWithdrawal> InstantWithdrawals => Set<InstantWithdrawal>();

    // Payout audit logs
    public DbSet<PayoutAuditLog> PayoutAuditLogs => Set<PayoutAuditLog>();

    // Email templates
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();

    // Receipt templates
    public DbSet<ReceiptTemplate> ReceiptTemplates => Set<ReceiptTemplate>();

    // Promo codes and referrals
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<PromoCodeUsage> PromoCodeUsage => Set<PromoCodeUsage>();
    public DbSet<UserReferral> UserReferrals => Set<UserReferral>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all DateTime properties to use timestamp without time zone
        // This works better with the AppContext.SetSwitch for legacy timestamp behavior
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp without time zone");
                }
            }
        }

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("email IS NOT NULL");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Phone)
            .IsUnique()
            .HasFilter("phone IS NOT NULL");

        modelBuilder.Entity<OwnerProfile>()
            .HasKey(o => o.UserId);

        modelBuilder.Entity<RenterProfile>()
            .HasKey(r => r.UserId);

        modelBuilder.Entity<DriverProfile>()
            .HasKey(d => d.UserId);

        modelBuilder.Entity<GlobalSetting>()
            .HasKey(g => g.Key);

        modelBuilder.Entity<AppConfig>()
            .HasKey(a => a.ConfigKey);

        modelBuilder.Entity<CarCategory>()
            .Property(c => c.DefaultDailyRate)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<CarCategory>()
            .Property(c => c.MinDailyRate)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<CarCategory>()
            .Property(c => c.MaxDailyRate)
            .HasColumnType("decimal(18,2)");

        // Per-vehicle explicit daily rate (nullable)
        modelBuilder.Entity<Vehicle>()
            .Property(v => v.DailyRate)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<InsurancePlan>()
            .Property(i => i.DailyPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ProtectionPlan>()
            .Property(p => p.DailyPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ProtectionPlan>()
            .Property(p => p.FixedPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ProtectionPlan>()
            .Property(p => p.MinFee)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ProtectionPlan>()
            .Property(p => p.MaxFee)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Booking>()
            .Property(b => b.RentalAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Booking>()
            .Property(b => b.DepositAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Booking>()
            .Property(b => b.InsuranceAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Booking>()
            .Property(b => b.ProtectionAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Booking>()
            .Property(b => b.TotalAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<PaymentTransaction>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<PaymentTransaction>()
            .Property(p => p.CapturedAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Payout>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Referral>()
            .Property(r => r.RewardAmount)
            .HasColumnType("decimal(18,2)");

        // Notifications
        modelBuilder.Entity<Notification>()
            .HasKey(n => n.Id);
        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.UserId);

        modelBuilder.Entity<NotificationJob>()
            .HasKey(nj => nj.Id);
        modelBuilder.Entity<NotificationJob>()
            .HasIndex(nj => nj.Status);
        modelBuilder.Entity<NotificationJob>()
            .HasIndex(nj => nj.ScheduledAt);
        modelBuilder.Entity<NotificationJob>()
            .Property(nj => nj.ChannelsJson).HasColumnType("text");
        modelBuilder.Entity<NotificationJob>()
            .Property(nj => nj.MetadataJson).HasColumnType("text");


        modelBuilder.Entity<IntegrationPartner>()
            .HasIndex(i => i.ApiKey)
            .IsUnique();

        modelBuilder.Entity<IntegrationPartner>()
            .HasIndex(i => i.ReferralCode)
            .IsUnique()
            .HasFilter("referral_code IS NOT NULL");

        modelBuilder.Entity<RegionalPricing>()
            .Property(r => r.PriceMultiplier)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<RegionalPricing>()
            .Property(r => r.ExtraHoldAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<RegionalPricing>()
            .Property(r => r.MinDailyRate)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<RegionalPricing>()
            .Property(r => r.MaxDailyRate)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<RefundPolicy>()
            .Property(r => r.RefundPercentage)
            .HasColumnType("decimal(5,2)");

        // Configure Booking-Inspection relationships
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.PickupInspection)
            .WithMany()
            .HasForeignKey(b => b.PickupInspectionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.ReturnInspection)
            .WithMany()
            .HasForeignKey(b => b.ReturnInspectionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Inspection>()
            .HasOne(i => i.Booking)
            .WithMany()
            .HasForeignKey(i => i.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure DriverProfile-User relationships
        modelBuilder.Entity<DriverProfile>()
            .HasOne(d => d.User)
            .WithOne(u => u.DriverProfile)
            .HasForeignKey<DriverProfile>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DriverProfile>()
            .HasOne(d => d.OwnerEmployer)
            .WithMany()
            .HasForeignKey(d => d.OwnerEmployerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
