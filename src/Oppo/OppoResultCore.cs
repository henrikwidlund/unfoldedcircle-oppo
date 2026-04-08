using System.Diagnostics.CodeAnalysis;

namespace Oppo;

internal readonly record struct OppoResultCore(
    bool Success,
    bool ShouldRetry,
    string? Response)
{
    public string? Response { get; } = Response;

    [MemberNotNullWhen(true, nameof(Response))]
    public bool Success { get; } = Success;

    public bool ShouldRetry { get; } = ShouldRetry;

    public static readonly OppoResultCore FalseResult = new(false, false, null);
    public static readonly OppoResultCore RetryResult = new(false, true, null);

    public static OppoResultCore SuccessResult(string response) => new(true, false, response);
}
