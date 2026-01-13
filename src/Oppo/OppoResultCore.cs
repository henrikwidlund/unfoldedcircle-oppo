using System.Diagnostics.CodeAnalysis;

namespace Oppo;

internal readonly record struct OppoResultCore(
    bool Success,
    string? Response)
{
    public string? Response { get; } = Response;

    [MemberNotNullWhen(true, nameof(Response))]
    public bool Success { get; } = Success;

    public bool InvalidVerboseLevel { get; private init; }

    public static readonly OppoResultCore FalseResult = new(false, null);
    public static readonly OppoResultCore InvalidVerboseLevelResult = new(false, null) { InvalidVerboseLevel = true };

    public static OppoResultCore SuccessResult(string response) => new(true, response);
}