using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cleriq.Data;

public class AppDbContext : IdentityDbContext<Utilizator, Rol, int>
{
    private readonly IFurnizorTenant _tenant;
    private readonly IFurnizorUtilizator _utilizator;

    // Computed properties: citesc dinamic de la furnizori la fiecare acces.
    // Esențial pentru ca SlugTenantMiddleware să poată seta tenant-ul DUPĂ
    // construirea DbContext-ului (pe rutele publice).
    public int InstitutieIdCurenta => _tenant.InstitutieId;
    public int? UserIdCurent => _utilizator.UserId;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IFurnizorTenant tenant,
        IFurnizorUtilizator utilizator)
        : base(options)
    {
        _tenant = tenant;
        _utilizator = utilizator;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Forțează ca toate DateTime/DateTime? citite din SQL să fie marcate Kind=Utc.
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

        modelBuilder.Entity<Institutie>()
            .Property(i => i.FusOrar)
            .HasDefaultValue("Europe/Bucharest");

        modelBuilder.Entity<Institutie>()
            .Property(i => i.SmtpSecuritate)
            .HasDefaultValue(SmtpSecuritate.Auto)
            .HasSentinel(SmtpSecuritate.Auto);

        modelBuilder.Entity<PunctOrdineZi>()
            .Property(p => p.TipVot)
            .HasDefaultValue(TipVot.Nominal)
            .HasSentinel(TipVot.Nominal);

        // Slug: index unic GLOBAL — fără HasFilter pe soft-delete.
        modelBuilder.Entity<Institutie>()
            .HasIndex(i => i.Slug)
            .IsUnique();

        // FK Institutie cu Restrict pe toate entitățile tenant.
        // Restrict (NU Cascade) pentru două motive:
        //  1. SQL Server interzice multiple cascade paths spre aceeași entitate;
        //  2. Filosofia proiectului: soft-delete peste tot, niciodată DELETE fizic pe Institutie.
        // Protecție defensivă DB: un DELETE fizic accidental e refuzat de DB.

        modelBuilder.Entity<Consilier>()
            .HasOne(x => x.Institutie)
            .WithMany(i => i.Consilieri)
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Comisie>()
            .HasOne(x => x.Institutie)
            .WithMany(i => i.Comisii)
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Sedinta>()
            .HasOne(x => x.Institutie)
            .WithMany(i => i.Sedinte)
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ComisieMembru>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Mandat>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prezenta>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vot>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PunctOrdineZi>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProcesVerbal>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Convocare>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IncercareTrimitere>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transcriere>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transcriere>()
            .HasOne(t => t.Sedinta)
            .WithOne(s => s.Transcriere)
            .HasForeignKey<Transcriere>(t => t.SedintaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IncercareTrimitere>()
            .HasOne(i => i.Convocare)
            .WithMany(co => co.Incercari)
            .HasForeignKey(i => i.ConvocareId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IncercareTrimitere>()
            .HasIndex(i => new { i.ConvocareId, i.Canal, i.CreatLa });

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Sedinta)
            .WithMany(s => s.Documente)
            .HasForeignKey(d => d.SedintaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Punct)
            .WithMany(p => p.Documente)
            .HasForeignKey(d => d.PunctId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Document_ExactUnContext",
                "(CASE WHEN [SedintaId] IS NULL THEN 0 ELSE 1 END + " +
                " CASE WHEN [PunctId] IS NULL THEN 0 ELSE 1 END) = 1"));

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

        modelBuilder.Entity<Utilizator>()
            .HasOne(u => u.Consilier)
            .WithMany()
            .HasForeignKey(u => u.ConsilierId)
            .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<Utilizator>()
            .HasIndex(u => u.ConsilierId)
            .IsUnique()
            .HasFilter("[ConsilierId] IS NOT NULL");

        modelBuilder.Entity<Transcriere>()
            .HasIndex(t => t.SedintaId)
            .IsUnique()
            .HasFilter("[EsteSters] = 0");

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Familie);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.Utilizator)
            .WithMany()
            .HasForeignKey(rt => rt.UtilizatorId)
            .OnDelete(DeleteBehavior.Cascade);

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
        var institutieId = InstitutieIdCurenta;  // citește o singură dată per SaveChanges

        var intrari = ChangeTracker.Entries<EntitateDeBaza>().ToList();
        var coadaCascada = new Queue<EntitateDeBaza>();

        foreach (var intrare in intrari)
        {
            switch (intrare.State)
            {
                case EntityState.Added:
                    intrare.Entity.CreatLa = acum;
                    intrare.Entity.CreatDe = userId;

                    if (!_tenant.EsteModSystem
                        && institutieId != 0
                        && intrare.Entity is IEntitateCuTenant tenantNou)
                        tenantNou.InstitutieId = institutieId;
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
                CascadaPeColectie(Documente.Where(d => d.SedintaId == s.Id), acum, userId, coada);
                CascadaPeColectie(Transcrieri.Where(t => t.SedintaId == s.Id), acum, userId, coada);
                break;

            case PunctOrdineZi p:
                CascadaPeColectie(Voturi.Where(v => v.PunctId == p.Id), acum, userId, coada);
                CascadaPeColectie(Documente.Where(d => d.PunctId == p.Id), acum, userId, coada);   // NOU
                break;
        }
    }

    private void CascadaPeColectie<T>(
        IQueryable<T> query, DateTime acum, int? userId, Queue<EntitateDeBaza> coada)
        where T : EntitateDeBaza
    {
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
    public DbSet<Document> Documente { get; set; }
    public DbSet<IncercareTrimitere> IncercariTrimitere { get; set; }
    public DbSet<Transcriere> Transcrieri { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
}