using LCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LCM.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<LabelType> LabelTypes { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<TemplateField> TemplateFields { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }

    public DbSet<DigitalLabelSpec> DigitalLabelSpecs { get; set; }
    public DbSet<EslField> EslFields { get; set; }
    public DbSet<FieldMapping> FieldMappings { get; set; }
    public DbSet<EslApiSetting> EslApiSettings { get; set; }

    public DbSet<Store> Stores { get; set; }
    public DbSet<Layout> Layouts { get; set; }


    public DbSet<CampaignStore> CampaignStores { get; set; }
    public DbSet<EslJob> EslJobs { get; set; }
    public DbSet<EslGonderimLog> EslGonderimLogs { get; set; }

    public DbSet<UserStore> UserStores { get; set; }
    public DbSet<EslEslestirme> EslEslestirmeler { get; set; }
    public DbSet<Font> Fonts { get; set; }
    public DbSet<CampaignLog> CampaignLogs { get; set; }
    public DbSet<SmtpSetting> SmtpSettings { get; set; }

    public DbSet<EslTask> EslTasks { get; set; }
    public DbSet<EslTaskLog> EslTaskLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User - unique indexler
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.KullaniciAdi).IsUnique();

        // User - Role ilişkisi
        modelBuilder.Entity<User>()
            .HasOne(u => u.Rol)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RolId)
            .OnDelete(DeleteBehavior.Restrict);

        // Campaign - unique indexler
        modelBuilder.Entity<Campaign>()
            .HasIndex(c => c.Sku).IsUnique();
        modelBuilder.Entity<Campaign>()
            .HasIndex(c => c.Barkod).IsUnique();

        // Campaign - Template ilişkisi
        modelBuilder.Entity<Campaign>()
            .HasOne(c => c.Sablon)
            .WithMany(t => t.Kampanyalar)
            .HasForeignKey(c => c.SablonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Campaign - User ilişkisi
        modelBuilder.Entity<Campaign>()
            .HasOne(c => c.EkleyenKullanici)
            .WithMany()
            .HasForeignKey(c => c.EkleyenKullaniciId)
            .OnDelete(DeleteBehavior.Restrict);

        //Campaing - Sotre
        // Campaign - Store (many-to-many)
        modelBuilder.Entity<CampaignStore>()
            .HasKey(cs => new { cs.CampaignId, cs.StoreId });

        modelBuilder.Entity<CampaignStore>()
            .HasOne(cs => cs.Campaign)
            .WithMany(c => c.CampaignStores)
            .HasForeignKey(cs => cs.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CampaignStore>()
            .HasOne(cs => cs.Store)
            .WithMany()
            .HasForeignKey(cs => cs.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        // Template - User ilişkisi
        modelBuilder.Entity<Template>()
            .HasOne(t => t.EkleyenKullanici)
            .WithMany()
            .HasForeignKey(t => t.EkleyenKullaniciId)
            .OnDelete(DeleteBehavior.Restrict);

        // Fiyat için decimal hassasiyeti
        modelBuilder.Entity<Campaign>()
            .Property(c => c.OriginalPrice)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Campaign>()
            .Property(c => c.DiscountedPrice)
            .HasColumnType("decimal(18,2)");
        // FieldMapping ilişkileri
        modelBuilder.Entity<FieldMapping>()
            .HasOne(f => f.Sablon)
            .WithMany()
            .HasForeignKey(f => f.SablonId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FieldMapping>()
            .HasOne(f => f.EslField)
            .WithMany(e => e.Mappings)
            .HasForeignKey(f => f.EslFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        // Aynı şablonda aynı alan iki kere eşleştirilemez
        modelBuilder.Entity<FieldMapping>()
            .HasIndex(f => new { f.SablonId, f.BizimAlanAdi })
            .IsUnique();
        // Template - EtiketTip unique (bir etiket tipine sadece bir şablon)
        modelBuilder.Entity<Template>()
            .HasIndex(t => t.EtiketTipId)
            .IsUnique();

        // Seed Data — Roller
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, RolAdi = "Admin" },
            new Role { Id = 2, RolAdi = "KampanyaYonetici" },
            new Role { Id = 3, RolAdi = "VeriGirisi" },
            new Role { Id = 4, RolAdi = "Operator" },
            new Role { Id = 5, RolAdi = "Goruntuleyici" }
        );

        // Seed Data — Sistem Ayarları
        modelBuilder.Entity<SystemSetting>().HasData(
            new SystemSetting { Id = 1, AyarAdi = "KampanyaEkleCaptcha", AktifMi = false },
            new SystemSetting { Id = 2, AyarAdi = "SablonEkleCaptcha", AktifMi = false },
            new SystemSetting { Id = 3, AyarAdi = "KullaniciEkleCaptcha", AktifMi = false }
        );

        // Seed Data — Etiket Tipleri
        modelBuilder.Entity<LabelType>().HasData(
            new LabelType { Id = 1, EtiketTipi = "E50" },
            new LabelType { Id = 2, EtiketTipi = "E60" },
            new LabelType { Id = 3, EtiketTipi = "E75" }
        );
        
        modelBuilder.Entity<EslApiSetting>().HasData(
    new EslApiSetting
    {
        Id = 1,
        ApiUrl = "http://192.168.1.1:8080/prismart/integration",
        AccessKey = "",
        SecretKey = "",
        CustomerStoreCode = "",
        Algorithm = "HS256",
        HeaderPrefix = "HSIAM1"
    }
);

        //Etiket Özellikleri
        modelBuilder.Entity<DigitalLabelSpec>()
    .HasOne(d => d.EtiketTip)
    .WithMany()
    .HasForeignKey(d => d.EtiketTipId)
    .OnDelete(DeleteBehavior.Restrict);

        // EslJob - User ilişkisi
        modelBuilder.Entity<EslJob>()
            .HasOne(j => j.OlusturanKullanici)
            .WithMany()
            .HasForeignKey(j => j.OlusturanKullaniciId)
            .OnDelete(DeleteBehavior.Restrict);

        // EslGonderimLog - EslJob ilişkisi
        modelBuilder.Entity<EslGonderimLog>()
            .HasOne(l => l.EslJob)
            .WithMany()
            .HasForeignKey(l => l.EslJobId)
            .OnDelete(DeleteBehavior.SetNull);

        // EslGonderimLog - User ilişkisi
        modelBuilder.Entity<EslGonderimLog>()
            .HasOne(l => l.Kullanici)
            .WithMany()
            .HasForeignKey(l => l.KullaniciId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UserStore>()
    .HasKey(us => new { us.UserId, us.StoreId });

        modelBuilder.Entity<UserStore>()
            .HasOne(us => us.User)
            .WithMany(u => u.YetkiliStoreler)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserStore>()
            .HasOne(us => us.Store)
            .WithMany()
            .HasForeignKey(us => us.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EslEslestirme>()
            .HasOne(e => e.Kampanya)
            .WithMany()
            .HasForeignKey(e => e.KampanyaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EslEslestirme>()
            .HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EslEslestirme>()
            .HasOne(e => e.Kullanici)
            .WithMany()
            .HasForeignKey(e => e.KullaniciId)
            .OnDelete(DeleteBehavior.Restrict);
        // Campaign - CurrentOwner ilişkisi
        modelBuilder.Entity<Campaign>()
            .HasOne(c => c.CurrentOwner)
            .WithMany()
            .HasForeignKey(c => c.CurrentOwnerKullaniciId)
            .OnDelete(DeleteBehavior.SetNull);

        // CampaignLog - Campaign ilişkisi
        modelBuilder.Entity<CampaignLog>()
            .HasOne(l => l.Campaign)
            .WithMany(c => c.Loglar)
            .HasForeignKey(l => l.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        // CampaignLog - User ilişkisi
        modelBuilder.Entity<CampaignLog>()
            .HasOne(l => l.Kullanici)
            .WithMany()
            .HasForeignKey(l => l.KullaniciId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EslTask>()
    .HasOne(t => t.Store)
    .WithMany()
    .HasForeignKey(t => t.StoreId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EslTaskLog>()
            .HasOne(l => l.EslTask)
            .WithMany(t => t.Logs)
            .HasForeignKey(l => l.EslTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SystemSetting>().HasData(
            new SystemSetting { Id = 4, AyarAdi = "EslOnayPopupAktif", AktifMi = true },
            new SystemSetting { Id = 5, AyarAdi = "EslOverrideAktif", AktifMi = true }
        );
        modelBuilder.Entity<SystemSetting>().HasData(
    new SystemSetting { Id = 6, AyarAdi = "DashboardOtomatikGuncelleme", AktifMi = true },
    new SystemSetting { Id = 7, AyarAdi = "KampanyaOnayaGonderilsinMi", AktifMi = false },
    new SystemSetting { Id = 8, AyarAdi = "MailBildirimleriAktif", AktifMi = false },
    new SystemSetting { Id = 9, AyarAdi = "OnayMailiGonderilsinMi", AktifMi = false }
);

        modelBuilder.Entity<SmtpSetting>().HasData(
            new SmtpSetting
            {
                Id = 1,
                Host = "",
                Port = 587,
                KullaniciAdi = "",
                Sifre = "",
                GonderenAdi = "LCM Sistem",
                GonderenEmail = "",
                SslAktif = true
            }
        );
    }

}