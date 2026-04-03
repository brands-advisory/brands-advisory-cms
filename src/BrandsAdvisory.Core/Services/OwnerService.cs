using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace BrandsAdvisory.Core.Services;

public class OwnerService(IConfiguration configuration) : IOwnerService
{
    private readonly string _ownerOid = configuration["Owner:Oid"] ?? string.Empty;

    public bool IsOwner(ClaimsPrincipal user)
    {
        if (!user.Identity?.IsAuthenticated ?? true)
            return false;

        var oid = user.FindFirst("oid")?.Value
               ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

        return !string.IsNullOrEmpty(oid)
            && string.Equals(oid, _ownerOid, StringComparison.OrdinalIgnoreCase);
    }
}
