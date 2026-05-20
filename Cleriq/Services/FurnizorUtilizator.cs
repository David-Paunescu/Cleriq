using System.Security.Claims;

namespace Cleriq.Services;

public class FurnizorUtilizator : IFurnizorUtilizator
{
    public int? UserId { get; }

    public FurnizorUtilizator(IHttpContextAccessor accessor)
    {
        var claim = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        UserId = int.TryParse(claim, out var id) ? id : null;
    }
}