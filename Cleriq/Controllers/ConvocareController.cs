using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Helpers;
using Cleriq.Models;
using Cleriq.Services;
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

    public ConvocareController(
        AppDbContext context,
        IGeneratorConvocare generator)
    {
        _context = context;
        _generator = generator;
    }

    // POST: înregistrează intenția de convocare.
    // Conținutul (Subiect/EmailHtml/SmsText) e generat și înghețat pe Convocare la NOU/restaurat.
    // La re-POST pe convocare activă, conținutul stocat NU se regenerează — audit cuvânt-cu-cuvânt.
    // Notificările NU se trimit aici. Worker-ul (BackgroundService) preia rândurile InAsteptare.
    [HttpPost("Convocare")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> TrimiteConvocari(int sedintaId, CancellationToken ct)
    {
        var sedinta = await _context.Sedinte
                    .Include(s => s.Institutie)
                    .Include(s => s.Documente)
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
            bool esteNouOriRestaurat = false;

            if (convocare is null)
            {
                convocare = new Convocare
                {
                    SedintaId = sedintaId,
                    ConsilierId = consilier.Id
                };
                _context.Convocari.Add(convocare);
                esteNouOriRestaurat = true;
            }
            else if (convocare.EsteSters)
            {
                // Restore + reset statusuri (rundă nouă completă)
                convocare.EsteSters = false;
                convocare.StersLa = null;
                convocare.StersDe = null;
                convocare.EmailStatus = null;
                convocare.EmailTrimisLa = null;
                convocare.EmailDetalii = null;
                convocare.SmsStatus = null;
                convocare.SmsTrimisLa = null;
                convocare.SmsDetalii = null;
                esteNouOriRestaurat = true;
            }

            // Conținut: îngheț la creare/restaurare. La re-POST pe convocare activă, nu atingem
            // conținutul stocat — agenda comunicată e parte din actul de convocare oficial.
            if (esteNouOriRestaurat)
            {
                var continut = _generator.Genereaza(sedinta, consilier);
                convocare.Subiect = continut.Subiect;
                convocare.EmailHtml = continut.EmailHtml;
                convocare.SmsText = continut.SmsText;
            }

            // ===== Canal Email =====
            if (string.IsNullOrWhiteSpace(consilier.Email))
            {
                if (convocare.EmailStatus is null)
                    convocare.EmailStatus = StatusTrimitere.FaraDestinatie;
            }
            else
            {
                if (convocare.EmailStatus != StatusTrimitere.Trimisa
                    && convocare.EmailStatus != StatusTrimitere.InAsteptare)
                {
                    convocare.EmailStatus = StatusTrimitere.InAsteptare;
                    convocare.EmailTrimisLa = null;
                    convocare.EmailDetalii = null;
                }
            }

            if (string.IsNullOrWhiteSpace(consilier.Telefon))
            {
                if (convocare.SmsStatus is null)
                    convocare.SmsStatus = StatusTrimitere.FaraDestinatie;
            }
            else
            {
                if (convocare.SmsStatus != StatusTrimitere.Trimisa
                    && convocare.SmsStatus != StatusTrimitere.InAsteptare)
                {
                    convocare.SmsStatus = StatusTrimitere.InAsteptare;
                    convocare.SmsTrimisLa = null;
                    convocare.SmsDetalii = null;
                }
            }

            rezultate.Add(convocare.StatusGeneral());
        }

        // Tranziție status ședință: convocarea ca act administrativ.
        // Necondiționat — chiar și pe ZERO consilieri cu coordonate, actul e emis oficial
        // (problema „cum dă seama secretarul telefonic" e administrativă, nu de software).
        // Niciodată retrogradăm o ședință care a avansat dincolo de Planificata.
        if (sedinta.Status == StatusSedinta.Planificata)
            sedinta.Status = StatusSedinta.Convocata;
        if (sedinta.ConvocareTrimisaLa is null)
            sedinta.ConvocareTrimisaLa = acum;

        await _context.SaveChangesAsync(ct);

        return Ok(new RezultatConvocareDto(
            TotalConsilieri: rezultate.Count,
            TotalSucces: rezultate.Count(r => r == StatusConvocare.TotalSucces),
            InCursDeTrimitere: rezultate.Count(r => r == StatusConvocare.InCursDeTrimitere),
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

    [HttpGet("Convocari/{convocareId}/Incercari")]
    public async Task<IActionResult> ListaIncercari(
    int sedintaId, int convocareId, [FromQuery] CanalNotificare? canal, CancellationToken ct)
    {
        var convocareExista = await _context.Convocari
            .AnyAsync(co => co.Id == convocareId && co.SedintaId == sedintaId, ct);
        if (!convocareExista)
            return NotFound("Convocarea nu există în această ședință.");

        var query = _context.IncercariTrimitere
            .Where(i => i.ConvocareId == convocareId);

        if (canal.HasValue)
            query = query.Where(i => i.Canal == canal.Value);

        var rezultat = await query
            .OrderBy(i => i.Canal)
            .ThenBy(i => i.CreatLa)
            .Select(i => new IncercareTrimitereDto(
                i.Id, i.Canal, i.Status, i.Destinatar, i.Detalii, i.CreatLa))
            .ToListAsync(ct);

        return Ok(rezultat);
    }

    [HttpPost("Convocari/{convocareId}/Retry")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> RetryConvocare(
    int sedintaId, int convocareId, CancellationToken ct)
    {
        var convocare = await _context.Convocari
            .Include(co => co.Consilier)
            .Include(co => co.Sedinta)
            .FirstOrDefaultAsync(co => co.Id == convocareId && co.SedintaId == sedintaId, ct);

        if (convocare is null)
            return NotFound("Convocarea nu există în această ședință.");

        if (convocare.Sedinta.Status == StatusSedinta.Finalizata
            || convocare.Sedinta.Status == StatusSedinta.Anulata)
            return Conflict(
                $"Nu se poate retransmite o convocare pentru o ședință cu status {convocare.Sedinta.Status}.");

        var consilier = convocare.Consilier;

        if (string.IsNullOrWhiteSpace(consilier.Email))
        {
            if (convocare.EmailStatus is null)
                convocare.EmailStatus = StatusTrimitere.FaraDestinatie;
        }
        else
        {
            if (convocare.EmailStatus != StatusTrimitere.Trimisa
                && convocare.EmailStatus != StatusTrimitere.InAsteptare)
            {
                convocare.EmailStatus = StatusTrimitere.InAsteptare;
                convocare.EmailTrimisLa = null;
                convocare.EmailDetalii = null;
            }
        }

        if (string.IsNullOrWhiteSpace(consilier.Telefon))
        {
            if (convocare.SmsStatus is null)
                convocare.SmsStatus = StatusTrimitere.FaraDestinatie;
        }
        else
        {
            if (convocare.SmsStatus != StatusTrimitere.Trimisa
                && convocare.SmsStatus != StatusTrimitere.InAsteptare)
            {
                convocare.SmsStatus = StatusTrimitere.InAsteptare;
                convocare.SmsTrimisLa = null;
                convocare.SmsDetalii = null;
            }
        }

        await _context.SaveChangesAsync(ct);

        return Ok(new ConvocareDto(
            convocare.Id,
            convocare.SedintaId,
            convocare.ConsilierId,
            convocare.Consilier.NumeComplet,
            convocare.EmailStatus,
            convocare.EmailTrimisLa,
            convocare.EmailDetalii,
            convocare.SmsStatus,
            convocare.SmsTrimisLa,
            convocare.SmsDetalii,
            convocare.StatusGeneral(),
            convocare.CreatLa));
    }

    [HttpDelete("Convocare")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> ReseteazaConvocari(int sedintaId, CancellationToken ct)
    {
        var sedinta = await _context.Sedinte
            .FirstOrDefaultAsync(s => s.Id == sedintaId, ct);

        if (sedinta is null)
            return NotFound("Ședința nu există.");

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
            _context.Convocari.Remove(co);

        sedinta.ConvocareTrimisaLa = null;
        if (sedinta.Status == StatusSedinta.Convocata)
            sedinta.Status = StatusSedinta.Planificata;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}