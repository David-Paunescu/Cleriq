namespace Cleriq.Services;

public class FurnizorTenant : IFurnizorTenant
{
    public int InstitutieId { get; }

    public FurnizorTenant(IHttpContextAccessor accessor)
    {
        var claim = accessor.HttpContext?.User?.FindFirst("InstitutieId")?.Value;
        InstitutieId = int.TryParse(claim, out var id) ? id : 0;
    }
}