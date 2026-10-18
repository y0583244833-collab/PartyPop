using System.Security.Claims;
using PartyPop.Core.Enums;
using PartyPop.Core.Exceptions;

namespace PartyPop.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>ה-UserId מתוך ה-Claim "sub" של ה-Token.</summary>
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst("sub")?.Value, out var id)
            ? id
            : throw new UnauthorizedException("Token לא תקין");

    public static UserRole GetRole(this ClaimsPrincipal user) =>
        Enum.TryParse<UserRole>(user.FindFirst("role")?.Value, out var role)
            ? role
            : throw new UnauthorizedException("Token לא תקין");
}
