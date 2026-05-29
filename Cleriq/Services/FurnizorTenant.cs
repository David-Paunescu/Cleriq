namespace Cleriq.Services;

public class FurnizorTenant : IFurnizorTenant
{
    public const string CheieHttpItems = "InstitutieId";

    private readonly IHttpContextAccessor _accessor;

    public FurnizorTenant(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public int InstitutieId
    {
        get
        {
            var ctx = _accessor.HttpContext;

            // 1. Priority: Items setat de SlugTenantMiddleware (rute publice cu slug)
            if (ctx?.Items != null
                && ctx.Items.TryGetValue(CheieHttpItems, out var item)
                && item is int idItems
                && idItems > 0)
            {
                return idItems;
            }

            // 2. Fallback: claim JWT (rute autentificate)
            var claim = ctx?.User?.FindFirst("InstitutieId")?.Value;
            return int.TryParse(claim, out var idClaim) ? idClaim : 0;
        }
    }

    public bool EsteModSystem => false;
}