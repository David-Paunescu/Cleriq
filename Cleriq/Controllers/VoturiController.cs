using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cleriq.Helpers;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi")]
[Authorize]
public class VoturiController : ControllerBase
{
    private readonly AppDbContext _context;

    public VoturiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Lista(int sedintaId, int punctId)
    {
        var punct = await _context.PuncteOrdineZi
            .FirstOrDefaultAsync(p => p.Id == punctId && p.SedintaId == sedintaId);
        if (punct is null)
            return NotFound("Punctul nu există în această ședință.");

        var voturi = await _context.Voturi
            .Where(v => v.PunctId == punctId)
            .Include(v => v.Consilier)
            .ToListAsync();

        var rezumat = punct.Rezumat(voturi);

        var voturiNominale = rezumat.VoturiNominale
            .Select(v => new VotDto(
                v.Id, punctId, v.ConsilierId, v.Consilier.NumeComplet,
                v.Optiune, v.DataOra, v.InstitutieId))
            .ToList();

        OptiuneVot? votulMeu = null;
        var consilierIdClaim = User.FindFirst("ConsilierId")?.Value;
        if (int.TryParse(consilierIdClaim, out var cidLogat))
            votulMeu = voturi.FirstOrDefault(v => v.ConsilierId == cidLogat)?.Optiune;

        return Ok(new VoturiPunctDto(
            punctId,
            punct.TipVot,
            rezumat.Pentru,
            rezumat.Impotriva,
            rezumat.Abtineri,
            rezumat.TotalExprimate,
            voturiNominale,
            rezumat.Participanti.ToList(),
            votulMeu));
    }

    // Vot introdus manual de secretar/admin (ex: consilier fără cont, ședință hibridă).
    // Blocat la vot secret — vezi InregistreazaVotIntern.
    [HttpPost]
    [Authorize(Roles = "Admin,Secretar")]
    public Task<IActionResult> InregistreazaVot(int sedintaId, int punctId, InregistrareVotDto dto)
        => InregistreazaVotIntern(sedintaId, punctId, dto.ConsilierId, dto.Optiune, esteManual: true);

    // Vot individual: consilierul logat votează pentru el însuși.
    // ConsilierId vine din claim — nu poate vota pentru altcineva. Funcționează la nominal ȘI secret.
    [HttpPost("Self")]
    [Authorize(Roles = "Consilier")]
    public async Task<IActionResult> InregistreazaVotSelf(int sedintaId, int punctId, InregistrareVotSelfDto dto)
    {
        var claim = User.FindFirst("ConsilierId")?.Value;
        if (!int.TryParse(claim, out var consilierId))
            return BadRequest("Contul tău nu este legat de un consilier. Contactează administratorul.");

        return await InregistreazaVotIntern(sedintaId, punctId, consilierId, dto.Optiune, esteManual: false);
    }

    // Logica comună: validări (punct deschis, consilier activ, prezent) + upsert cu restore-on-re-add.
    private async Task<IActionResult> InregistreazaVotIntern(
        int sedintaId, int punctId, int consilierId, OptiuneVot optiune, bool esteManual)
    {
        var punct = await _context.PuncteOrdineZi
            .FirstOrDefaultAsync(p => p.Id == punctId && p.SedintaId == sedintaId);
        if (punct is null)
            return NotFound("Punctul nu există în această ședință.");

        if (!punct.NecesitaVot)
            return BadRequest("Acest punct nu necesită vot.");

        if (punct.Rezultat.HasValue)
            return Conflict("Votul pe acest punct este închis. Nu se mai poate modifica.");

        // La vot secret, secretarul NU poate introduce voturi manual — secretul trebuie 100% secret.
        // Singura cale e self-vote-ul consilierului.
        if (esteManual && punct.TipVot == TipVot.Secret)
            return Conflict(
                "La vot secret, voturile se înregistrează exclusiv de către consilieri prin endpoint-ul propriu (.../Voturi/Self).");

        var consilier = await _context.Consilieri
            .FirstOrDefaultAsync(c => c.Id == consilierId);
        if (consilier is null)
            return NotFound("Consilierul nu există.");

        if (!consilier.Activ)
            return BadRequest("Consilierul nu este activ.");

        // OUG 57/2019: doar consilierii Prezenți/OnlinePrezenți pot vota.
        var prezenta = await _context.Prezente
            .FirstOrDefaultAsync(p => p.SedintaId == sedintaId && p.ConsilierId == consilierId);
        if (prezenta is null
            || (prezenta.Status != StatusPrezenta.Prezent
                && prezenta.Status != StatusPrezenta.OnlinePrezent))
            return BadRequest("Consilierul nu este marcat ca prezent. Doar prezenții pot vota.");

        // Upsert + restore-on-re-add
        var existent = await _context.Voturi
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.PunctId == punctId
                                   && v.ConsilierId == consilierId
                                   && v.InstitutieId == _context.InstitutieIdCurenta);

        var acum = DateTime.UtcNow;

        if (existent is null)
        {
            var votNou = new Vot
            {
                PunctId = punctId,
                ConsilierId = consilierId,
                Optiune = optiune,
                DataOra = acum
            };
            _context.Voturi.Add(votNou);
            await _context.SaveChangesAsync();

            return Ok(new VotDto(
                votNou.Id, punctId, consilier.Id, consilier.NumeComplet,
                votNou.Optiune, votNou.DataOra, votNou.InstitutieId));
        }

        existent.Optiune = optiune;
        existent.DataOra = acum;
        if (existent.EsteSters)
        {
            existent.EsteSters = false;
            existent.StersLa = null;
        }
        await _context.SaveChangesAsync();

        return Ok(new VotDto(
            existent.Id, punctId, consilier.Id, consilier.NumeComplet,
            existent.Optiune, existent.DataOra, existent.InstitutieId));
    }

    [HttpDelete("{consilierId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AnuleazaVot(int sedintaId, int punctId, int consilierId)
    {
        var punct = await _context.PuncteOrdineZi
            .FirstOrDefaultAsync(p => p.Id == punctId && p.SedintaId == sedintaId);
        if (punct is null)
            return NotFound("Punctul nu există în această ședință.");

        if (punct.Rezultat.HasValue)
            return Conflict("Votul pe acest punct este închis.");

        // La secret, adminul nu poate șterge voturi (ar putea targeta o persoană anume).
        if (punct.TipVot == TipVot.Secret)
            return Conflict("La vot secret, voturile nu pot fi șterse manual.");

        var vot = await _context.Voturi
            .FirstOrDefaultAsync(v => v.PunctId == punctId && v.ConsilierId == consilierId);
        if (vot is null)
            return NotFound();

        _context.Voturi.Remove(vot);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}