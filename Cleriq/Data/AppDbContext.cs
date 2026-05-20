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
    public int? UserIdCurent { get; }

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IFurnizorTenant tenant,
        IFurnizorUtilizator utilizator)
        : base(options)
    {
        InstitutieIdCurenta = tenant.InstitutieId;
        UserIdCurent = utilizator.UserId;
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

        // Indexuri unice filtrate: aplică unicitatea doar pe rândurile active.
        // Astfel restore-on-re-add devine opțional (rămâne folosit pentru audit continuu),
        // iar un INSERT direct nu mai eșuează când există un rând soft-deleted.
        modelBuilder.Entity<ComisieMembru>()
            .HasIndex(cm => new { cm.ComisieId, cm.ConsilierId })
            .IsUnique()
            .HasFilter("[EsteSters] = 0");

        modelBuilder.Entity<Prezenta>()
            .HasIndex(p => new { p.SedintaId, p.ConsilierId })
            .IsUnique()
            .HasFilter("[EsteSters] = 0");

        modelBuilder.Entity<Vot>()
            .HasIndex(v => new { v.PunctId, v.ConsilierId })
            .IsUnique()
            .HasFilter("[EsteSters] = 0");
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
        var userId = UserIdCurent;

        // Snapshot — pentru că adăugăm noi tracked entities în timpul cascadei
        var intrari = ChangeTracker.Entries<EntitateDeBaza>().ToList();

        foreach (var intrare in intrari)
        {
            switch (intrare.State)
            {
                case EntityState.Added:
                    intrare.Entity.CreatLa = acum;
                    intrare.Entity.CreatDe = userId;

                    // Forțează apartenența la instituția userului logat,
                    // ca să nu se poată insera date în alt tenant.
                    if (InstitutieIdCurenta != 0 && intrare.Entity is IEntitateCuTenant tenantNou)
                        tenantNou.InstitutieId = InstitutieIdCurenta;
                    break;

                case EntityState.Modified:
                    intrare.Entity.ModificatLa = acum;
                    intrare.Entity.ModificatDe = userId;
                    break;

                case EntityState.Deleted:
                    intrare.State = EntityState.Modified;
                    intrare.Entity.EsteSters = true;
                    intrare.Entity.StersLa = acum;
                    intrare.Entity.StersDe = userId;

                    AplicaCascadaSoftDelete(intrare.Entity, acum, userId);
                    break;
            }
        }
    }

    private void AplicaCascadaSoftDelete(EntitateDeBaza parinte, DateTime acum, int? userId)
    {
        switch (parinte)
        {
            case Consilier c:
                CascadaPeColectie(ComisieMembri.Where(m => m.ConsilierId == c.Id), acum, userId);
                CascadaPeColectie(Mandate.Where(m => m.ConsilierId == c.Id), acum, userId);
                break;

            case Sedinta s:
                CascadaPeColectie(Prezente.Where(p => p.SedintaId == s.Id), acum, userId);
                CascadaPeColectie(ProceseVerbale.Where(pv => pv.SedintaId == s.Id), acum, userId);
                // TODO la implementarea Vot:
                // CascadaPeColectie(PuncteOrdineZi.Where(po => po.SedintaId == s.Id), acum, userId);
                // și extinde switch-ul pentru case PunctOrdineZi → Voturi
                break;
        }
    }

    private void CascadaPeColectie<T>(IQueryable<T> query, DateTime acum, int? userId)
        where T : EntitateDeBaza
    {
        // Filtrul global elimină automat cele deja soft-deleted + alt tenant
        var copii = query.ToList();
        foreach (var copil in copii)
        {
            copil.EsteSters = true;
            copil.StersLa = acum;
            copil.StersDe = userId;
            Entry(copil).State = EntityState.Modified;
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