using System.Globalization;
using Cleriq.Data;
using Cleriq.Helpers;
using Cleriq.Models;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Services;

// Auto-generarea dispoziției de convocare (Pas 12). Best-effort + idempotentă: nu blochează
// convocarea și nu creează duplicate. Dispoziția rămâne Draft — secretarul o numerotează + semnează
// prin fluxul normal (numerotarea e act deliberat cu anti-lacună, nu o facem automat aici).
public class ServiciuDispozitieConvocare : IServiciuDispozitieConvocare
{
    private static readonly CultureInfo CulturaRo = new("ro-RO");

    private readonly AppDbContext _context;
    private readonly IServiciuFunctiiIstorice _functiiIstorice;
    private readonly IGeneratorDispozitie _generator;

    public ServiciuDispozitieConvocare(
        AppDbContext context,
        IServiciuFunctiiIstorice functiiIstorice,
        IGeneratorDispozitie generator)
    {
        _context = context;
        _functiiIstorice = functiiIstorice;
        _generator = generator;
    }

    public async Task CreeazaDacaLipsesteAsync(Sedinta sedinta, CancellationToken ct = default)
    {
        // Idempotent: o singură dispoziție de convocare per ședință (persistă peste reset-uri de convocare).
        if (await _context.Dispozitii.AnyAsync(d => d.SedintaId == sedinta.Id, ct))
            return;

        var dataEmitere = DateOnly.FromDateTime(DateTime.UtcNow);

        // Best-effort: fără primar sau secretar valid la dată → nu blocăm convocarea.
        var primar = await _functiiIstorice.CinEPrimarulLa(dataEmitere);
        var secretar = await _functiiIstorice.CinESecretarulUatLa(dataEmitere);
        if (primar is null || secretar is null)
            return;

        var dataEmitereUtc = dataEmitere.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
        var dataSedintaLocala = sedinta.DataOra.LaFusOrar(sedinta.Institutie.FusOrar);
        var tipText = sedinta.Tip.Eticheta().ToLower(CulturaRo);

        var dispozitie = new Dispozitie
        {
            TipDispozitie = TipDispozitie.Individual,
            Titlu = $"convocarea Consiliului Local în ședință {tipText} din {dataSedintaLocala:dd.MM.yyyy}",
            DataEmitere = dataEmitereUtc,
            Status = StatusActRedactional.Draft,
            EstePublicat = false,
            SedintaId = sedinta.Id
        };
        dispozitie.Semnatari.Add(new SemnatarDispozitie
        {
            RolSemnatar = RolSemnatarDispozitie.Emitent,
            PersoanaId = primar.Id,
            DataSemnare = dataEmitere,
            OrdineAfisare = 1
        });
        dispozitie.Semnatari.Add(new SemnatarDispozitie
        {
            RolSemnatar = RolSemnatarDispozitie.SecretarContrasemnatura,
            PersoanaId = secretar.Id,
            DataSemnare = dataEmitere,
            OrdineAfisare = 2
        });

        _context.Dispozitii.Add(dispozitie);
        await _context.SaveChangesAsync(ct);

        // Reload cu navigările necesare generatorului (Institutie + semnatari cu Persoana/Consilier).
        var completa = await _context.Dispozitii
            .Include(d => d.Institutie)
            .Include(d => d.Semnatari).ThenInclude(s => s.Persoana)
            .Include(d => d.Semnatari).ThenInclude(s => s.Consilier)
            .FirstAsync(d => d.Id == dispozitie.Id, ct);

        completa.Continut = _generator.GenereazaConvocare(completa, sedinta);
        await _context.SaveChangesAsync(ct);
    }
}
