namespace Idasletten.Shared.Auth;

/// Whether real Azure AD credentials are configured (the "AzureAD" auth scheme is only
/// registered when true, since OpenIdConnectOptions validation throws on every request
/// otherwise). Lets the login page hide/disable the Microsoft button when not configured.
public record AzureAdAvailability(bool Configured);
