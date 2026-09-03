namespace Idasletten.Features.Common;

/// <summary>
/// A friendly, user-facing validation/domain error raised by command handlers.
/// Pages catch it and show <see cref="Message"/> in an alert.
/// </summary>
public sealed class FeatureException : Exception
{
    public FeatureException(string message) : base(message) { }
}
