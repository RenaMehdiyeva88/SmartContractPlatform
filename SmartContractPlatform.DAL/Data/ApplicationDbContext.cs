using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartContractPlatform.DAL.Entities;
using SmartContractPlatform.DAL.Security;

namespace SmartContractPlatform.DAL.Data;

public class ApplicationDbContext : DbContext
{
    private readonly string _encryptionKey;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _encryptionKey = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key appsettings-də tapılmadı");
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractParty> ContractParties => Set<ContractParty>();
    public DbSet<Signature> Signatures => Set<Signature>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var encryptionConverter = new EncryptedStringConverter(_encryptionKey);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.IdentificationNumber).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.IdentificationNumber)
                .HasMaxLength(256)
                .HasConversion(encryptionConverter);
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.Phone).HasMaxLength(32);
            entity.HasQueryFilter(u => !u.IsDeleted);
        });

        modelBuilder.Entity<Template>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(256);
            entity.HasQueryFilter(t => !t.IsDeleted);
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasOne(c => c.Template)
                .WithMany(t => t.Contracts)
                .HasForeignKey(c => c.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.CreatedByUser)
                .WithMany(u => u.CreatedContracts)
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        modelBuilder.Entity<ContractParty>(entity =>
        {
            entity.HasOne(p => p.Contract)
                .WithMany(c => c.Parties)
                .HasForeignKey(p => p.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.User)
                .WithMany(u => u.ContractParties)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(p => p.IdentificationNumber).HasMaxLength(20);
            entity.Property(p => p.ContactEmail).HasMaxLength(256);
            entity.Property(p => p.ContactPhone).HasMaxLength(32);
            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        modelBuilder.Entity<Signature>(entity =>
        {
            entity.HasOne(s => s.Contract)
                .WithMany(c => c.Signatures)
                .HasForeignKey(s => s.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.User)
                .WithMany(u => u.Signatures)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(s => !s.IsDeleted);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasOne(d => d.Contract)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(d => !d.IsDeleted);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(a => a.Contract)
                .WithMany()
                .HasForeignKey(a => a.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(a => a.Action).HasMaxLength(64);
            entity.Property(a => a.IpAddress).HasMaxLength(64);
            entity.HasQueryFilter(a => !a.IsDeleted);
        });
    }
}