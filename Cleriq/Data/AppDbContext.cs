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

        modelBuilder.Entity<ProcesVerbal>()
            .HasOne(pv => pv.AprobatInSedinta)
            .WithMany()
            .HasForeignKey(pv => pv.AprobatInSedintaId)
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

        modelBuilder.Entity<Persoana>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MandatFunctie>()
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
            .ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Document_ExactUnContext",
                    "(CASE WHEN [SedintaId] IS NULL THEN 0 ELSE 1 END + " +
                    " CASE WHEN [PunctId] IS NULL THEN 0 ELSE 1 END + " +
                    " CASE WHEN [HclId] IS NULL THEN 0 ELSE 1 END) = 1");

                t.HasCheckConstraint(
                    "CK_Document_AnexaMetadata",
                    "[TipDocumentHcl] IS NULL OR [TipDocumentHcl] != 1 OR [NumarOrdinAnexa] IS NOT NULL");
            });

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

        // MandatFunctie: FK polimorfic spre Persoana SAU Consilier (XOR).
        // Ambele FK Restrict — istoricul rămâne în DB chiar dacă persoana sau
        // consilierul ar fi șterși fizic (imposibil oricum prin app).
        modelBuilder.Entity<MandatFunctie>()
            .HasOne(mf => mf.Persoana)
            .WithMany(p => p.MandateFunctie)
            .HasForeignKey(mf => mf.PersoanaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MandatFunctie>()
            .HasOne(mf => mf.Consilier)
            .WithMany()
            .HasForeignKey(mf => mf.ConsilierId)
            .OnDelete(DeleteBehavior.Restrict);

        // 3 check constraints — defense in depth la nivel DB pentru regulile
        // de mapare TipFunctie↔FK (din OUG 57/2019) și perioadă validă.
        modelBuilder.Entity<MandatFunctie>()
            .ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_MandatFunctie_ExactUnSubject",
                    "(CASE WHEN [PersoanaId] IS NULL THEN 0 ELSE 1 END + " +
                    " CASE WHEN [ConsilierId] IS NULL THEN 0 ELSE 1 END) = 1");

                t.HasCheckConstraint(
                    "CK_MandatFunctie_FkCorectaPerTip",
                    "([TipFunctie] IN (1, 3) AND [PersoanaId] IS NOT NULL AND [ConsilierId] IS NULL) " +
                    "OR ([TipFunctie] = 2 AND [ConsilierId] IS NOT NULL AND [PersoanaId] IS NULL)");

                t.HasCheckConstraint(
                    "CK_MandatFunctie_PerioadaValida",
                    "[DataSfarsit] IS NULL OR [DataSfarsit] >= [DataInceput]");
            });

        modelBuilder.Entity<ComisieMembru>()
            .HasIndex(cm => new { cm.ComisieId, cm.ConsilierId })
            .IsUnique()
            .HasFilter("[EsteSters] = 0 AND [DataSfarsit] IS NULL");

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

        // MandatFunctie — 3 filtered unique distincte (Primar=1, Viceprimar=2, SecretarUat=3):
        //  - Primar / SecretarUat: exclusiv per tenant la un moment dat
        //  - Viceprimar: același consilier nu poate fi viceprimar de două ori simultan
        //    (pot fi totuși 2-3 viceprimari diferiți activi în paralel)
        modelBuilder.Entity<MandatFunctie>()
            .HasIndex(mf => mf.InstitutieId, "IX_MandatFunctie_PrimarActiv")
            .IsUnique()
            .HasFilter("[EsteSters] = 0 AND [DataSfarsit] IS NULL AND [TipFunctie] = 1");

        modelBuilder.Entity<MandatFunctie>()
            .HasIndex(mf => mf.InstitutieId, "IX_MandatFunctie_SecretarUatActiv")
            .IsUnique()
            .HasFilter("[EsteSters] = 0 AND [DataSfarsit] IS NULL AND [TipFunctie] = 3");

        modelBuilder.Entity<MandatFunctie>()
            .HasIndex(mf => new { mf.InstitutieId, mf.ConsilierId }, "IX_MandatFunctie_ViceprimarActivPerConsilier")
            .IsUnique()
            .HasFilter("[EsteSters] = 0 AND [DataSfarsit] IS NULL AND [TipFunctie] = 2");

        // Index secundar pentru picker UI (alegere persoană la creare mandat).
        // NU unique — pot exista doi „Ion Popescu" în aceeași instituție.
        modelBuilder.Entity<Persoana>()
            .HasIndex(p => new { p.InstitutieId, p.NumeComplet });

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

        // === FK Institutie cu Restrict — Modul A HCL ===
        modelBuilder.Entity<Hcl>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SemnatarHcl>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ComunicareHclPrefect>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RelatieHcl>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        // === IstoricActiuneAct → Institutie (Restrict). Referință slabă (TipAct, ActId) către
        // act — FĂRĂ FK/navigație, ca jurnalul să supraviețuiască soft-delete-ului actului. ===
        modelBuilder.Entity<IstoricActiuneAct>()
            .HasOne(i => i.Institutie)
            .WithMany()
            .HasForeignKey(i => i.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        // === Hcl → PunctOrdineZi (FK Restrict, IMUTABIL post-Numerotat) ===
        modelBuilder.Entity<Hcl>()
            .HasOne(h => h.PunctOrdineZi)
            .WithMany()
            .HasForeignKey(h => h.PunctOrdineZiId)
            .OnDelete(DeleteBehavior.Restrict);

        // === SemnatarHcl → Hcl + XOR Persoana/Consilier ===
        modelBuilder.Entity<SemnatarHcl>()
            .HasOne(s => s.Hcl)
            .WithMany(h => h.Semnatari)
            .HasForeignKey(s => s.HclId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SemnatarHcl>()
            .HasOne(s => s.Persoana)
            .WithMany()
            .HasForeignKey(s => s.PersoanaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SemnatarHcl>()
            .HasOne(s => s.Consilier)
            .WithMany()
            .HasForeignKey(s => s.ConsilierId)
            .OnDelete(DeleteBehavior.Restrict);

        // === ComunicareHclPrefect → Hcl ===
        modelBuilder.Entity<ComunicareHclPrefect>()
            .HasOne(c => c.Hcl)
            .WithMany(h => h.Comunicari)
            .HasForeignKey(c => c.HclId)
            .OnDelete(DeleteBehavior.Restrict);

        // === RelatieHcl → HclSursa NOT NULL + HclTinta? (NU cascadează la HCL) ===
        modelBuilder.Entity<RelatieHcl>()
            .HasOne(r => r.HclSursa)
            .WithMany(h => h.RelatiiSursa)
            .HasForeignKey(r => r.HclSursaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RelatieHcl>()
            .HasOne(r => r.HclTinta)
            .WithMany(h => h.RelatiiTinta)
            .HasForeignKey(r => r.HclTintaId)
            .OnDelete(DeleteBehavior.Restrict);

        // === Document → Hcl? (cascade soft-delete prin AplicaCascadaSoftDelete) ===
        modelBuilder.Entity<Document>()
            .HasOne(d => d.Hcl)
            .WithMany(h => h.Documente)
            .HasForeignKey(d => d.HclId)
            .OnDelete(DeleteBehavior.Restrict);

        // === Sedinta → PresedinteSedinta (Consilier, FK Restrict) ===
        modelBuilder.Entity<Sedinta>()
            .HasOne(s => s.PresedinteSedinta)
            .WithMany()
            .HasForeignKey(s => s.PresedinteSedintaConsilierId)
            .OnDelete(DeleteBehavior.Restrict);

        // === Filtered unique — Modul A HCL ===

        // Numerotare unică per (Institutie, AnNumerotare, Numar) — compare-and-swap atomic
        modelBuilder.Entity<Hcl>()
            .HasIndex(h => new { h.InstitutieId, h.AnNumerotare, h.Numar })
            .IsUnique()
            .HasFilter("[EsteSters] = 0 AND [Numar] IS NOT NULL");

        // Un punct → maxim un HCL activ (filtered unique pe PunctOrdineZiId)
        modelBuilder.Entity<Hcl>()
            .HasIndex(h => h.PunctOrdineZiId)
            .IsUnique()
            .HasFilter("[EsteSters] = 0");

        // Registru cronologic comunicări prefect per (Institutie, AnRegistru)
        modelBuilder.Entity<ComunicareHclPrefect>()
            .HasIndex(c => new { c.InstitutieId, c.AnRegistru, c.NumarOrdineInRegistru })
            .IsUnique()
            .HasFilter("[EsteSters] = 0");

        // Anexă unică per HCL (TipDocumentHcl = 1 = Anexa)
        modelBuilder.Entity<Document>()
            .HasIndex(d => new { d.HclId, d.NumarOrdinAnexa })
            .IsUnique()
            .HasFilter("[EsteSters] = 0 AND [TipDocumentHcl] = 1 AND [HclId] IS NOT NULL");

        // RelatieHcl — unic pe țintă internă (sursa, ținta, tip); textul extern nu intră
        modelBuilder.Entity<RelatieHcl>()
            .HasIndex(r => new { r.HclSursaId, r.HclTintaId, r.TipRelatie })
            .IsUnique()
            .HasFilter("[EsteSters] = 0 AND [HclTintaId] IS NOT NULL");

        // SemnatarHcl — max 1 PresedinteSedinta activ per HCL (RolSemnatar = 1)
        modelBuilder.Entity<SemnatarHcl>()
            .HasIndex(s => s.HclId, "IX_SemnatarHcl_PresedinteSedintaActiv")
            .IsUnique()
            .HasFilter("[EsteSters] = 0 AND [RolSemnatar] = 1");

        // SemnatarHcl — max 1 SecretarUat activ per HCL (RolSemnatar = 2)
        modelBuilder.Entity<SemnatarHcl>()
            .HasIndex(s => s.HclId, "IX_SemnatarHcl_SecretarUatActiv")
            .IsUnique()
            .HasFilter("[EsteSters] = 0 AND [RolSemnatar] = 2");

        // === Check constraints — Modul A HCL ===

        modelBuilder.Entity<RelatieHcl>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_RelatieHcl_ExactUnaTinta",
                "(CASE WHEN [HclTintaId] IS NULL THEN 0 ELSE 1 END + " +
                " CASE WHEN [ReferintaActExternText] IS NULL THEN 0 ELSE 1 END) = 1"));

        modelBuilder.Entity<SemnatarHcl>()
            .ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_SemnatarHcl_ExactUnSubject",
                    "(CASE WHEN [PersoanaId] IS NULL THEN 0 ELSE 1 END + " +
                    " CASE WHEN [ConsilierId] IS NULL THEN 0 ELSE 1 END) = 1");

                t.HasCheckConstraint(
                    "CK_SemnatarHcl_FkCorectaPerRol",
                    "([RolSemnatar] = 2 AND [PersoanaId] IS NOT NULL AND [ConsilierId] IS NULL) " +
                    "OR ([RolSemnatar] IN (1, 3) AND [ConsilierId] IS NOT NULL AND [PersoanaId] IS NULL)");
            });

        // === Modul C Dispoziții (paritar HCL) ===
        modelBuilder.Entity<Dispozitie>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        // Dispoziția de convocare → Sedinta (opțional, Restrict). NU intră în cascada `case Sedinta`:
        // un act numerotat supraviețuiește ședinței (paritar Hcl → PunctOrdineZi).
        modelBuilder.Entity<Dispozitie>()
            .HasOne(d => d.Sedinta)
            .WithMany()
            .HasForeignKey(d => d.SedintaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SemnatarDispozitie>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SemnatarDispozitie>()
            .HasOne(s => s.Dispozitie)
            .WithMany(d => d.Semnatari)
            .HasForeignKey(s => s.DispozitieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SemnatarDispozitie>()
            .HasOne(s => s.Persoana)
            .WithMany()
            .HasForeignKey(s => s.PersoanaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SemnatarDispozitie>()
            .HasOne(s => s.Consilier)
            .WithMany()
            .HasForeignKey(s => s.ConsilierId)
            .OnDelete(DeleteBehavior.Restrict);

        // === ComunicareDispozitiePrefect → Institutie + Dispoziție (Restrict, paritar HCL) ===
        modelBuilder.Entity<ComunicareDispozitiePrefect>()
            .HasOne(x => x.Institutie)
            .WithMany()
            .HasForeignKey(x => x.InstitutieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ComunicareDispozitiePrefect>()
            .HasOne(c => c.Dispozitie)
            .WithMany(d => d.Comunicari)
            .HasForeignKey(c => c.DispozitieId)
            .OnDelete(DeleteBehavior.Restrict);

        // Registru propriu de numerotare — ambele tipuri (Normativ/Individual) în aceeași secvență
        modelBuilder.Entity<Dispozitie>()
            .HasIndex(d => new { d.InstitutieId, d.AnNumerotare, d.Numar })
            .IsUnique()
            .HasFilter("[EsteSters] = 0 AND [Numar] IS NOT NULL");

        // Max 1 Emitent activ + max 1 SecretarContrasemnatura activ per dispoziție
        modelBuilder.Entity<SemnatarDispozitie>()
            .HasIndex(s => s.DispozitieId, "IX_SemnatarDispozitie_EmitentActiv")
            .IsUnique()
            .HasFilter("[EsteSters] = 0 AND [RolSemnatar] = 1");

        modelBuilder.Entity<SemnatarDispozitie>()
            .HasIndex(s => s.DispozitieId, "IX_SemnatarDispozitie_SecretarContrasemnaturaActiv")
            .IsUnique()
            .HasFilter("[EsteSters] = 0 AND [RolSemnatar] = 2");

        // XOR subiect + FK-corectă-per-rol: Emitent = Persoana SAU Consilier (viceprimar-înlocuitor);
        // SecretarContrasemnatura = doar Persoana
        modelBuilder.Entity<SemnatarDispozitie>()
            .ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_SemnatarDispozitie_ExactUnSubject",
                    "(CASE WHEN [PersoanaId] IS NULL THEN 0 ELSE 1 END + " +
                    " CASE WHEN [ConsilierId] IS NULL THEN 0 ELSE 1 END) = 1");

                t.HasCheckConstraint(
                    "CK_SemnatarDispozitie_FkCorectaPerRol",
                    "([RolSemnatar] = 2 AND [PersoanaId] IS NOT NULL AND [ConsilierId] IS NULL) " +
                    "OR ([RolSemnatar] = 1)");
            });

        // Registru cronologic comunicări prefect dispoziții per (Institutie, AnRegistru)
        modelBuilder.Entity<ComunicareDispozitiePrefect>()
            .HasIndex(c => new { c.InstitutieId, c.AnRegistru, c.NumarOrdineInRegistru })
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
        var institutieId = InstitutieIdCurenta;

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
                CascadaPeColectie(SemnatariHcl.Where(s => s.ConsilierId == c.Id), acum, userId, coada);
                CascadaPeColectie(SemnatariDispozitie.Where(s => s.ConsilierId == c.Id), acum, userId, coada);
                break;

            case Sedinta s:
                CascadaPeColectie(Prezente.Where(p => p.SedintaId == s.Id), acum, userId, coada);
                CascadaPeColectie(ProceseVerbale.Where(pv => pv.SedintaId == s.Id), acum, userId, coada);
                CascadaPeColectie(PuncteOrdineZi.Where(po => po.SedintaId == s.Id), acum, userId, coada);
                CascadaPeColectie(Convocari.Where(co => co.SedintaId == s.Id), acum, userId, coada);
                CascadaPeColectie(Documente.Where(d => d.SedintaId == s.Id), acum, userId, coada);
                CascadaPeColectie(Transcrieri.Where(t => t.SedintaId == s.Id), acum, userId, coada);
                // Dispozitii.SedintaId NU se cascadează: dispoziția de convocare e act numerotat, cu
                // viață proprie, care supraviețuiește ședinței (paritar Hcl → PunctOrdineZi).
                break;

            case PunctOrdineZi p:
                CascadaPeColectie(Voturi.Where(v => v.PunctId == p.Id), acum, userId, coada);
                CascadaPeColectie(Documente.Where(d => d.PunctId == p.Id), acum, userId, coada);
                break;

            case Hcl h:
                CascadaPeColectie(SemnatariHcl.Where(s => s.HclId == h.Id), acum, userId, coada);
                CascadaPeColectie(ComunicariHclPrefect.Where(c => c.HclId == h.Id), acum, userId, coada);
                CascadaPeColectie(Documente.Where(d => d.HclId == h.Id), acum, userId, coada);
                // Auditul (IstoricActiuneAct) NU se cascadează — log imutabil care supraviețuiește actului.
                break;

            case Dispozitie d:
                CascadaPeColectie(SemnatariDispozitie.Where(s => s.DispozitieId == d.Id), acum, userId, coada);
                CascadaPeColectie(ComunicariDispozitiePrefect.Where(c => c.DispozitieId == d.Id), acum, userId, coada);
                // Auditul (IstoricActiuneAct) NU se cascadează — log imutabil care supraviețuiește actului.
                break;

            case Persoana pers:
                CascadaPeColectie(MandateFunctie.Where(mf => mf.PersoanaId == pers.Id), acum, userId, coada);
                CascadaPeColectie(SemnatariHcl.Where(s => s.PersoanaId == pers.Id), acum, userId, coada);
                CascadaPeColectie(SemnatariDispozitie.Where(s => s.PersoanaId == pers.Id), acum, userId, coada);
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
    public DbSet<Persoana> Persoane { get; set; }
    public DbSet<MandatFunctie> MandateFunctie { get; set; }
    public DbSet<Hcl> Hcluri { get; set; }
    public DbSet<SemnatarHcl> SemnatariHcl { get; set; }
    public DbSet<ComunicareHclPrefect> ComunicariHclPrefect { get; set; }
    public DbSet<RelatieHcl> RelatiiHcl { get; set; }
    public DbSet<IstoricActiuneAct> IstoricActiuniAct { get; set; }
    public DbSet<Dispozitie> Dispozitii { get; set; }
    public DbSet<SemnatarDispozitie> SemnatariDispozitie { get; set; }
    public DbSet<ComunicareDispozitiePrefect> ComunicariDispozitiePrefect { get; set; }
}