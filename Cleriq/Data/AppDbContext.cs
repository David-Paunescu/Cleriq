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
        
        modelBuilder.Entity<Convocare>()
            .HasOne(co => co.Consilier)
            .WithMany(c => c.Convocari)
            .HasForeignKey(co => co.ConsilierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexuri unice filtrate: aplică unicitatea doar pe rândurile active.
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

        modelBuilder.Entity<Convocare>()
            .HasIndex(co => new { co.SedintaId, co.ConsilierId })
            .IsUnique()
            .HasFilter("[EsteSters] = 0");

        // Filtru global automat: soft-delete + tenant
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(EntitateDeBaza).IsAssignableFrom(clrType))
                continue;

            var e = Expression.Parameter(clrType, "e");

            Expression predicat = Expression.Not(
                Expression.Property(e, nameof(EntitateDeBaza.EsteSters)));

            var institutieContext = Expression.Property(
                Expression.Constant(this), nameof(InstitutieIdCurenta));

            if (typeof(IEntitateCuTenant).IsAssignableFrom(clrType))
            {
                var institutieEntitate = Expression.Property(
                    e, nameof(IEntitateCuTenant.InstitutieId));
                predicat = Expression.AndAlso(predicat,
                    Expression.Equal(institutieEntitate, institutieContext));
            }
            else if (clrType == typeof(Institutie))
            {
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

        var intrari = ChangeTracker.Entries<EntitateDeBaza>().ToList();

        // Coadă pentru propagarea cascadei (worklist pattern).
        // Permite cascadă N-level: copiii marcați în timpul procesării
        // sunt enqueued și li se aplică propria cascadă la rândul lor.
        var coadaCascada = new Queue<EntitateDeBaza>();

        foreach (var intrare in intrari)
        {
            switch (intrare.State)
            {
                case EntityState.Added:
                    intrare.Entity.CreatLa = acum;
                    intrare.Entity.CreatDe = userId;

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
                    coadaCascada.Enqueue(intrare.Entity);
                    break;
            }
        }

        // Procesează cascada până se golește coada.
        // Notă: modelul nu are cicluri (Consilier → ComisieMembru/Mandat,
        // Sedinta → Prezenta/ProcesVerbal/PunctOrdineZi → Vot), deci nu
        // e nevoie de HashSet de protecție. De adăugat dacă apar cicluri.
        while (coadaCascada.Count > 0)
        {
            var parinte = coadaCascada.Dequeue();
            AplicaCascadaSoftDelete(parinte, acum, userId, coadaCascada);
        }
    }

    private void AplicaCascadaSoftDelete(
        EntitateDeBaza parinte, DateTime acum, int? userId, Queue<EntitateDeBaza> coada)
    {
        switch (parinte)
        {
            case Consilier c:
                CascadaPeColectie(ComisieMembri.Where(m => m.ConsilierId == c.Id), acum, userId, coada);
                CascadaPeColectie(Mandate.Where(m => m.ConsilierId == c.Id), acum, userId, coada);
                break;

            case Sedinta s:
                CascadaPeColectie(Prezente.Where(p => p.SedintaId == s.Id), acum, userId, coada);
                CascadaPeColectie(ProceseVerbale.Where(pv => pv.SedintaId == s.Id), acum, userId, coada);
                CascadaPeColectie(PuncteOrdineZi.Where(po => po.SedintaId == s.Id), acum, userId, coada);
                CascadaPeColectie(Convocari.Where(co => co.SedintaId == s.Id), acum, userId, coada);
                break;

            case PunctOrdineZi p:
                CascadaPeColectie(Voturi.Where(v => v.PunctId == p.Id), acum, userId, coada);
                break;
        }
    }

    private void CascadaPeColectie<T>(
        IQueryable<T> query, DateTime acum, int? userId, Queue<EntitateDeBaza> coada)
        where T : EntitateDeBaza
    {
        // Filtrul global elimină automat cele deja soft-deleted + alt tenant.
        var copii = query.ToList();
        foreach (var copil in copii)
        {
            copil.EsteSters = true;
            copil.StersLa = acum;
            copil.StersDe = userId;
            Entry(copil).State = EntityState.Modified;
            coada.Enqueue(copil);
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
    public DbSet<Convocare> Convocari { get; set; }
}