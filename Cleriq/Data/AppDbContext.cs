using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cleriq.Data;

public class AppDbContext : IdentityDbContext<Utilizator, Rol, int>
{
    public int InstitutieIdCurenta { get; }

    public AppDbContext(DbContextOptions<AppDbContext> options, IFurnizorTenant tenant)
        : base(options)
    {
        InstitutieIdCurenta = tenant.InstitutieId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Forțează ca toate DateTime/DateTime? citite din SQL să fie marcate Kind=Utc.
        // La scriere, dacă cineva trimite un DateTime non-UTC, îl normalizăm la UTC.
        var converterDateTime = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var converterDateTimeNullable = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime())
                : null,
            v => v.HasValue
                ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(converterDateTime);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(converterDateTimeNullable);
            }
        }

        modelBuilder.Entity<ComisieMembru>()
            .HasOne(cm => cm.Consilier)
            .WithMany(c => c.Apartenente)
            .HasForeignKey(cm => cm.ConsilierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prezenta>()
            .HasOne(p => p.Consilier)
            .WithMany(c => c.Prezente)
            .HasForeignKey(p => p.ConsilierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vot>()
            .HasOne(v => v.Consilier)
            .WithMany(c => c.Voturi)
            .HasForeignKey(v => v.ConsilierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Filtru global automat: soft-delete (toate entitățile)
        // + tenant (cele care au InstitutieId). Orice entitate nouă
        // care moștenește EntitateDeBaza primește filtrul singură.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(EntitateDeBaza).IsAssignableFrom(clrType))
                continue;

            var e = Expression.Parameter(clrType, "e");

            // !e.EsteSters
            Expression predicat = Expression.Not(
                Expression.Property(e, nameof(EntitateDeBaza.EsteSters)));

            var institutieContext = Expression.Property(
                Expression.Constant(this), nameof(InstitutieIdCurenta));

            if (typeof(IEntitateCuTenant).IsAssignableFrom(clrType))
            {
                // && e.InstitutieId == InstitutieIdCurenta
                var institutieEntitate = Expression.Property(
                    e, nameof(IEntitateCuTenant.InstitutieId));
                predicat = Expression.AndAlso(predicat,
                    Expression.Equal(institutieEntitate, institutieContext));
            }
            else if (clrType == typeof(Institutie))
            {
                // Instituția ESTE tenantul: && e.Id == InstitutieIdCurenta
                var idEntitate = Expression.Property(e, nameof(EntitateDeBaza.Id));
                predicat = Expression.AndAlso(predicat,
                    Expression.Equal(idEntitate, institutieContext));
            }

            var lambda = Expression.Lambda(predicat, e);
            modelBuilder.Entity(clrType).HasQueryFilter(lambda);
        }
    }

    public override int SaveChanges()
    {
        AplicaAuditSiSoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AplicaAuditSiSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AplicaAuditSiSoftDelete()
    {
        var acum = DateTime.UtcNow;

        foreach (var intrare in ChangeTracker.Entries<EntitateDeBaza>())
        {
            switch (intrare.State)
            {
                case EntityState.Added:
                    intrare.Entity.CreatLa = acum;

                    // Forțează apartenența la instituția userului logat,
                    // ca să nu se poată insera date în alt tenant.
                    if (InstitutieIdCurenta != 0 && intrare.Entity is IEntitateCuTenant tenantNou)
                        tenantNou.InstitutieId = InstitutieIdCurenta;
                    break;

                case EntityState.Modified:
                    intrare.Entity.ModificatLa = acum;
                    break;

                case EntityState.Deleted:
                    intrare.State = EntityState.Modified;
                    intrare.Entity.EsteSters = true;
                    intrare.Entity.StersLa = acum;
                    break;
            }
        }
    }

    public DbSet<Institutie> Institutii { get; set; }
    public DbSet<Consilier> Consilieri { get; set; }
    public DbSet<Comisie> Comisii { get; set; }
    public DbSet<ComisieMembru> ComisieMembri { get; set; }
    public DbSet<Mandat> Mandate { get; set; }
    public DbSet<Sedinta> Sedinte { get; set; }
    public DbSet<PunctOrdineZi> PuncteOrdineZi { get; set; }
    public DbSet<Prezenta> Prezente { get; set; }
    public DbSet<Vot> Voturi { get; set; }
    public DbSet<ProcesVerbal> ProceseVerbale { get; set; }
}