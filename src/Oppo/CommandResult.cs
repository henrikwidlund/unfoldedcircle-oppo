using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Oppo;

internal readonly record struct FailureResult(string? Response)
{
    public static readonly FailureResult Empty = new(null);
}

internal readonly record struct SuccessResult(string Response);

/// <summary>
/// Hand-written union to avoid boxing the case structs. <see langword="default"/> is <see cref="FailureResult.Empty"/>.
/// </summary>
[Union]
[StructLayout(LayoutKind.Auto)]
internal readonly struct CommandResult : IUnion
{
    private readonly string? _response;
    private readonly ResultKind _kind;

    public CommandResult(FailureResult failure)
    {
        _response = failure.Response;
        _kind = ResultKind.Failure;
    }

    // ReSharper disable once MemberCanBePrivate.Global Implicitly used
    public CommandResult(SuccessResult success)
    {
        _response = success.Response;
        _kind = ResultKind.Success;
    }

    // ReSharper disable once UnusedMember.Global // Implicitly used
    public bool HasValue => true;

    public object Value => _kind switch
    {
        ResultKind.Success => new SuccessResult(_response!),
        _ => new FailureResult(_response)
    };

    // ReSharper disable once UnusedMember.Global // Implicitly used
    public bool TryGetValue(out FailureResult value)
    {
        value = new FailureResult(_response);
        return _kind == ResultKind.Failure;
    }

    // ReSharper disable once UnusedMember.Global // Implicitly used
    public bool TryGetValue(out SuccessResult value)
    {
        value = new SuccessResult(_response!);
        return _kind == ResultKind.Success;
    }

    private enum ResultKind : byte
    {
        Failure,
        Success
    }
}
