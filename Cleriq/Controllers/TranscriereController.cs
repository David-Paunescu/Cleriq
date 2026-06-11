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
[Route("api/Sedinte/{sedintaId}/Transcriere")]
[Authorize]
public class TranscriereController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IStocareAudio _stocare;
    private readonly string _modelFolosit;

    public TranscriereController(
        AppDbContext context,
        IStocareAudio stocare,
        IConfiguration config)
    {
        _context = context;
        _stocare = stocare;
        _modelFolosit = config["Whisper:ModelFolosit"] ?? "large-v2";
    }

    [HttpGet]
    public async Task<IActionResult> Detalii(int sedintaId)
    {
        var sedintaExista = await _context.Sedinte.AnyAsync(s => s.Id == sedintaId);
        if (!sedintaExista) return NotFound("Ședința nu există.");

        var t = await _context.Transcrieri
            .FirstOrDefaultAsync(t => t.SedintaId == sedintaId);
        if (t is null) return NotFound("Nu există transcriere pentru această ședință.");

        return Ok(MapeazaSpreDto(t));
    }

    [HttpGet("Continut")]
    public async Task<IActionResult> ObtineContinut(int sedintaId)
    {
        var t = await _context.Transcrieri
            .FirstOrDefaultAsync(t => t.SedintaId == sedintaId);
        if (t is null) return NotFound();

        return Ok(new TranscriereContinutDto(
            t.Id, t.SedintaId, t.Status, t.ContinutBrut, t.ContinutEditat));
    }

    [HttpGet("Audio")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> DescarcaAudio(int sedintaId, CancellationToken ct)
    {
        var t = await _context.Transcrieri
            .FirstOrDefaultAsync(t => t.SedintaId == sedintaId);
        if (t is null) return NotFound();
        if (string.IsNullOrEmpty(t.CaleStocareAudio)) return NotFound("Audio lipsă.");

        Stream stream;
        try
        {
            stream = await _stocare.DeschideAsync(t.CaleStocareAudio, ct);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Fișierul audio nu mai există pe disk.");
        }

        var nume = Path.GetFileName(t.CaleStocareAudio);
        var mime = ValidareAudio.TipMimePentru(nume);
        return File(stream, mime, nume);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Secretar")]
    [RequestSizeLimit(ValidareAudio.MarimeMaxima)]
    public async Task<IActionResult> Incarca(
        int sedintaId,
        [FromForm] IFormFile fisier,
        CancellationToken ct)
    {
        if (fisier is null || fisier.Length == 0)
            return BadRequest("Fișier audio lipsă.");

        var eroare = ValidareAudio.Valideaza(fisier.FileName, fisier.Length);
        if (eroare is not null) return BadRequest(eroare);

        var sedintaExista = await _context.Sedinte.AnyAsync(s => s.Id == sedintaId, ct);
        if (!sedintaExista) return NotFound("Ședința nu există.");

        // Caut transcriere existentă (inclusiv soft-deleted din același tenant)
        var existenta = await _context.Transcrieri
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.SedintaId == sedintaId
                                   && t.InstitutieId == _context.InstitutieIdCurenta, ct);

        // Activă (nu soft-deleted) + status diferit de Esuata → 409
        // Pentru re-upload pe Esuata sau pe soft-deleted: restore complet.
        if (existenta is not null
            && !existenta.EsteSters
            && existenta.Status != StatusTranscriere.Esuata)
        {
            return Conflict(
                $"Există deja o transcriere pentru această ședință cu status {existenta.Status}. " +
                "Pentru a o înlocui, șterge-o întâi (DELETE).");
        }

        var caleAudioVeche = existenta?.CaleStocareAudio;

        // Salvare audio nou
        FisierAudio stocat;
        await using (var stream = fisier.OpenReadStream())
        {
            stocat = await _stocare.SalveazaAsync(
                _context.InstitutieIdCurenta, fisier.FileName, stream, ct);
        }

        Transcriere transcriere;

        if (existenta is null)
        {
            transcriere = new Transcriere
            {
                SedintaId = sedintaId,
                Status = StatusTranscriere.InAsteptare,
                CaleStocareAudio = stocat.Cheie,
                DimensiuneAudio = stocat.Marime,
                ModelFolosit = _modelFolosit
            };
            _context.Transcrieri.Add(transcriere);
        }
        else
        {
            // Restore + reset complet (acoperă atât Esuata cât și soft-deleted)
            existenta.EsteSters = false;
            existenta.StersLa = null;
            existenta.StersDe = null;
            existenta.Status = StatusTranscriere.InAsteptare;
            existenta.CaleStocareAudio = stocat.Cheie;
            existenta.DimensiuneAudio = stocat.Marime;
            existenta.DurataAudioSecunde = null;
            existenta.ContinutBrut = null;
            existenta.ContinutEditat = null;
            existenta.DataPrimireBrut = null;
            existenta.DataUltimeiEditari = null;
            existenta.PromptFolosit = null;
            existenta.NumarEsecuri = 0;
            existenta.UrmatoareaIncercareDupa = null;
            existenta.UltimaEroare = null;
            existenta.ModelFolosit = _modelFolosit;
            transcriere = existenta;
        }

        await _context.SaveChangesAsync(ct);

        // Post-commit: ștergere audio vechi (dacă e diferit de cel nou)
        if (!string.IsNullOrEmpty(caleAudioVeche) && caleAudioVeche != stocat.Cheie)
        {
            try
            {
                await _stocare.StergeAsync(caleAudioVeche, ct);
            }
            catch
            {
                // Audio vechi orfan acceptabil; cleanup viitor
            }
        }

        return Ok(MapeazaSpreDto(transcriere));
    }

    [HttpPut("Continut")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> EditeazaContinut(
        int sedintaId, EditareTranscriereDto dto)
    {
        var t = await _context.Transcrieri
            .FirstOrDefaultAsync(t => t.SedintaId == sedintaId);
        if (t is null) return NotFound();

        if (t.Status != StatusTranscriere.Finalizata)
            return Conflict(
                $"Editarea conținutului e posibilă doar la status Finalizata (curent: {t.Status}).");

        t.ContinutEditat = dto.ContinutEditat;
        t.DataUltimeiEditari = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(t));
    }

    [HttpPost("Retry")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Retry(int sedintaId)
    {
        var t = await _context.Transcrieri
            .FirstOrDefaultAsync(t => t.SedintaId == sedintaId);
        if (t is null) return NotFound();

        if (t.Status != StatusTranscriere.Esuata)
            return Conflict(
                $"Retry e posibil doar la status Esuata (curent: {t.Status}).");

        t.Status = StatusTranscriere.InAsteptare;
        t.NumarEsecuri = 0;
        t.UrmatoareaIncercareDupa = null;
        t.UltimaEroare = null;

        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(t));
    }

    [HttpDelete]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sterge(int sedintaId)
    {
        var t = await _context.Transcrieri
            .FirstOrDefaultAsync(t => t.SedintaId == sedintaId);
        if (t is null) return NotFound();

        // Soft-delete prin Remove; AplicaAuditSiSoftDelete face conversia.
        // Audio NU se șterge fizic — cleanup orphans = job mentenanță viitor.
        _context.Transcrieri.Remove(t);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static TranscriereDto MapeazaSpreDto(Transcriere t) => new(
        t.Id, t.SedintaId, t.Status,
        t.DataPrimireBrut, t.DataUltimeiEditari,
        t.DimensiuneAudio, t.DurataAudioSecunde,
        t.ModelFolosit, t.NumarEsecuri, t.UrmatoareaIncercareDupa,
        t.UltimaEroare, t.InstitutieId, t.CreatLa);
}