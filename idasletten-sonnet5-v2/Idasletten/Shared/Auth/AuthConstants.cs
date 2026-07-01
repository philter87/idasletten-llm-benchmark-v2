namespace Idasletten.Shared.Auth;

public static class AuthConstants
{
    /// <summary>Custom claim carrying the domain User.Id (Guid), distinct from the Azure AD object id.</summary>
    public const string UserIdClaimType = "idasletten_user_id";
}
