using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Oppo;

[StructLayout(LayoutKind.Auto)]
public readonly record struct OppoResult<TResult>
{
    [MemberNotNullWhen(true, nameof(Result))]
    public required bool Success { get; init; }

    public TResult? Result { get; init; }

    public static implicit operator OppoResult<TResult>(in bool result) => new() { Success = result };
    public static implicit operator bool(in OppoResult<TResult> result) => result.Success;
}
