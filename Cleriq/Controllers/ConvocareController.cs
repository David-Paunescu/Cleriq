using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Cleriq.Services;
using Cleriq.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/Sedinte/{sedintaId}")]
[Authorize]
public class ConvocareController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IGeneratorConvocare _generator;
    private readonly IServiciuNotificare _notificare;

    public ConvocareController(
        AppDbContext context,
        IGeneratorConvocare generator,
        IServiciuNotificare notificare)
    {
        _context = context;
        _generator = generator;
        _notificare = notificare;
    }

    [HttpPost("Convocare")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> TrimiteConvocari(int sedintaId, CancellationToken ct)
    {
        var sedinta = await _context.Sedinte
            .Include(s => s.Institutie)
            .Include(s => s.Puncte.OrderBy(p => p.Ordine))
            .FirstOrDefaultAsync(s => s.Id == sedintaId, ct);

        if (sedinta is null)
            return NotFound("Ședința nu există.");

        if (sedinta.Status == StatusSedinta.Finalizata || sedinta.Status == StatusSedinta.Anulata)
            return Conflict($"Nu se poate convoca o ședință cu status {sedinta.Status}.");

        var consilieri = await _context.Consilieri
            .Where(c => c.Activ)
            .OrderBy(c => c.NumeComplet)
            .ToListAsync(ct);

        if (!consilieri.Any())
            return BadRequest("Nu există consilieri activi de convocat.");

        // Încarcă convocările existente într-un singur query (inclusiv soft-deleted pentru restore-on-re-add)
        var convocariExistente = await _context.Convocari
            .IgnoreQueryFilters()
            .Where(co => co.SedintaId == sedintaId
                      && co.InstitutieId == _context.InstitutieIdCurenta)
            .ToListAsync(ct);

        var acum = DateTime.UtcNow;
        var rezultate = new List<StatusConvocare>();

        foreach (var consilier in consilieri)
        {
            var convocare = convocariExistente.FirstOrDefault(co => co.ConsilierId == consilier.Id);

            if (convocare is null)
            {
                convocare = new Convocare
                {
                    SedintaId = sedintaId,
                    ConsilierId = consilier.Id
                };
                _context.Convocari.Add(convocare);
            }
            else if (convocare.EsteSters)
            {
                // Restore + reset statusuri (vom reprocesa de la 0)
                convocare.EsteSters = false;
                convocare.StersLa = null;
                convocare.StersDe = null;
                convocare.EmailStatus = null;
                convocare.EmailTrimisLa = null;
                convocare.EmailDetalii = null;
                convocare.SmsStatus = null;
                convocare.SmsTrimisLa = null;
                convocare.SmsDetalii = null;
            }

            // Generează conținutul o singură dată per consilier
            var continut = _generator.Genereaza(sedinta, consilier);

            // ===== Procesare Email =====
            if (string.IsNullOrWhiteSpace(consilier.Email))
            {
                // Marcăm FaraDestinatie DOAR dacă nu am încercat niciodată — păstrăm istoricul
                if (convocare.EmailStatus is null)
                    convocare.EmailStatus = StatusTrimitere.FaraDestinatie;
            }
            else if (convocare.EmailStatus != StatusTrimitere.Trimisa)
            {
                // Trimitem dacă nu s-a reușit deja (idempotență per canal)
                var rezultat = await _notificare.TrimiteEmailAsync(
                    consilier.Email, continut.Subiect, continut.EmailHtml, ct);
                convocare.EmailStatus = rezultat.Succes ? StatusTrimitere.Trimisa : StatusTrimitere.Esuata;
                convocare.EmailTrimisLa = acum;
                convocare.EmailDetalii = rezultat.Detalii;
            }
            // Dacă EmailStatus era deja Trimisa → idempotență, nu facem nimic

            // ===== Procesare SMS =====
            if (string.IsNullOrWhiteSpace(consilier.Telefon))
            {
                if (convocare.SmsStatus is null)
                    convocare.SmsStatus = StatusTrimitere.FaraDestinatie;
            }
            else if (convocare.SmsStatus != StatusTrimitere.Trimisa)
            {
                var rezultat = await _notificare.TrimiteSmsAsync(
                    consilier.Telefon, continut.SmsText, ct);
                convocare.SmsStatus = rezultat.Succes ? StatusTrimitere.Trimisa : StatusTrimitere.Esuata;
                convocare.SmsTrimisLa = acum;
                convocare.SmsDetalii = rezultat.Detalii;
            }

            rezultate.Add(convocare.StatusGeneral());
        }

        // Tranzitie status ședință: doar dacă măcar un consilier a fost atins cu succes
        var aReusitMacarUna = rezultate.Any(r =>
            r == StatusConvocare.TotalSucces || r == StatusConvocare.PartialSucces);
        if (aReusitMacarUna)
        {
            sedinta.Status = StatusSedinta.Convocata;
            if (sedinta.ConvocareTrimisaLa is null)
                sedinta.ConvocareTrimisaLa = acum;
        }

        await _context.SaveChangesAsync(ct);

        return Ok(new RezultatConvocareDto(
            TotalConsilieri: rezultate.Count,
            TotalSucces: rezultate.Count(r => r == StatusConvocare.TotalSucces),
            PartialSucces: rezultate.Count(r => r == StatusConvocare.PartialSucces),
            Esuata: rezultate.Count(r => r == StatusConvocare.Esuata),
            FaraCoordonate: rezultate.Count(r => r == StatusConvocare.FaraCoordonate),
            ConvocareTrimisaLa: sedinta.ConvocareTrimisaLa));
    }

    [HttpGet("Convocari")]
    public async Task<IActionResult> Lista(int sedintaId, CancellationToken ct)
    {
        var sedintaExista = await _context.Sedinte.AnyAsync(s => s.Id == sedintaId, ct);
        if (!sedintaExista)
            return NotFound("Ședința nu există.");

        var convocari = await _context.Convocari
            .Include(co => co.Consilier)
            .Where(co => co.SedintaId == sedintaId)
            .OrderBy(co => co.Consilier.NumeComplet)
            .ToListAsync(ct);

        var rezultat = convocari.Select(co => new ConvocareDto(
            co.Id,
            co.SedintaId,
            co.ConsilierId,
            co.Consilier.NumeComplet,
            co.EmailStatus,
            co.EmailTrimisLa,
            co.EmailDetalii,
            co.SmsStatus,
            co.SmsTrimisLa,
            co.SmsDetalii,
            co.StatusGeneral(),
            co.CreatLa)).ToList();

        return Ok(rezultat);
    }

    [HttpDelete("Convocare")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> ReseteazaConvocari(int sedintaId, CancellationToken ct)
    {
        var sedinta = await _context.Sedinte
            .FirstOrDefaultAsync(s => s.Id == sedintaId, ct);

        if (sedinta is null)
            return NotFound("Ședința nu există.");

        // Reset valid doar din stadii incipiente (Planificata sau Convocata).
        // Dacă ședința a început/s-a terminat/a fost anulată, reset-ul nu mai are sens.
        if (sedinta.Status != StatusSedinta.Planificata
            && sedinta.Status != StatusSedinta.Convocata)
        {
            return Conflict(
                $"Nu se pot reseta convocările pentru o ședință cu status {sedinta.Status}.");
        }

        var convocari = await _context.Convocari
            .Where(co => co.SedintaId == sedintaId)
            .ToListAsync(ct);

        foreach (var co in convocari)
            _context.Convocari.Remove(co);   // devine soft-delete în SaveChanges

        sedinta.ConvocareTrimisaLa = null;
        if (sedinta.Status == StatusSedinta.Convocata)
            sedinta.Status = StatusSedinta.Planificata;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

}