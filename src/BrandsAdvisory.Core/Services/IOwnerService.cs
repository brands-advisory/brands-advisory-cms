using System.Security.Claims;

namespace BrandsAdvisory.Core.Services;

/// <summary>
/// Determines whether a given user is the site owner.
/// </summary>
public interface IOwnerService
{
    /// <summary>
    /// Returns true if the user's "oid" claim matches the configured owner OID.
    /// </summary>
    bool IsOwner(ClaimsPrincipal user);
}
