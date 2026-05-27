using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("public/{slug}/sedinte/{sedintaId:int}/procesverbal")]
public class PublicProcesVerbalController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicProcesVerbalController(AppDbContext context)
    {
        _context = context;
    }

    // GET /public/{slug}/sedinte/{sedintaId}/procesverbal
    [HttpGet]
    public async Task<IActionResult> Obtine(int sedintaId)
    {
        var pv = await ObtinePvPublic(sedintaId);
        if (pv is null) return NotFound();

        return Ok(new PublicProcesVerbalDto(
            pv.SedintaId, pv.Continut, pv.DataFinalizare));
    }

    // GET /public/{slug}/sedinte/{sedintaId}/procesverbal/markdown
    [HttpGet("markdown")]
    public async Task<IActionResult> ObtineMarkdown(int sedintaId)
    {
        var pv = await ObtinePvPublic(sedintaId);
        if (pv is null) return NotFound();

        return Content(pv.Continut ?? "", "text/markdown; charset=utf-8");
    }

    // PV public dacă: ședința e Convocata/InDesfasurare/Finalizata (NU Planificata/Anulata)
    //              și PV are Status=Finalizat
    private async Task<ProcesVerbal?> ObtinePvPublic(int sedintaId)
    {
        var sedintaPublica = await _context.Sedinte.AnyAsync(s =>
            s.Id == sedintaId
            && s.Status >= StatusSedinta.Convocata
            && s.Status != StatusSedinta.Anulata);
        if (!sedintaPublica) return null;

        return await _context.ProceseVerbale.FirstOrDefaultAsync(p =>
            p.SedintaId == sedintaId
            && p.Status == StatusProcesVerbal.Finalizat);
    }
}