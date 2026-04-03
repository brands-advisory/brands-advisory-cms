using System.Security.Claims;

namespace BrandsAdvisory.Core.Interfaces;

/// <summary>
/// Determines whether a given user is the site owner.
/// </summary>
public interface IOwnerService
{
    /// <summary>
    /// Returns <c>true</c> if the user's <c>oid</c> claim matches the
    /// configured owner OID.
    /// </summary>
    /// <param name="user">The claims principal from the current HTTP context.</param>
    bool IsOwner(ClaimsPrincipal user);
}
