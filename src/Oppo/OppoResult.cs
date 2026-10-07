using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Oppo;

/// <summary>
/// Failure case of <see cref="OppoResult{TResult}"/>.
/// </summary>
public readonly record struct OppoFailure;

/// <summary>
/// Case of <see cref="OppoResult{TResult}"/> for a command that succeeded, but where the player doesn't report the resulting value.
/// </summary>
public readonly record struct OppoNoResult;

public static class OppoResult
{
    public static OppoFailure Failure => default;

    public static OppoNoResult NoResult => default;

    extension<TResult>(OppoResult<TResult> result)
        where TResult : struct
    {
        /// <summary>
        /// The result, or <see langword="null"/> on failure or when there is no result.
        /// </summary>
        public TResult? ValueOrNull() => result.TryGetValue(out TResult value) ? value : null;
    }

    extension(OppoResult<string> result)
    {
        /// <summary>
        /// The result, or <see langword="null"/> on failure or when there is no result.
        /// </summary>
        public string? ValueOrNull() => result is string value ? value : null;
    }
}

/// <summary>
/// Either a <typeparamref name="TResult"/>, an <see cref="OppoNoResult"/> or an <see cref="OppoFailure"/>.
/// Handwritten union instead of a <c>union</c> declaration, since those box value types into <see cref="object"/>.
/// Pattern matching goes through the non-boxing <c>TryGetValue</c> overloads. <see langword="default"/> is a failure.
/// A successful <see langword="null"/> result has no value, so it only matches <see langword="null"/> and <c>not OppoFailure</c>.
/// </summary>
/// <remarks>
/// <see cref="HasValue"/> and the <c>TryGetValue</c> overloads look unused, but the compiler calls them when lowering patterns.
/// </remarks>
[Union]
[StructLayout(LayoutKind.Auto)]
public readonly struct OppoResult<TResult> : IUnion
{
    private readonly TResult _result;
    private readonly ResultKind _kind;

    public OppoResult(TResult result)
    {
        _result = result;
        _kind = ResultKind.Result;
    }

    // The parameter only selects the no result case of the union
    // ReSharper disable once UnusedParameter.Local
    // ReSharper disable once MemberCanBePrivate.Global
    public OppoResult(OppoNoResult noResult)
    {
        _result = default!;
        _kind = ResultKind.NoResult;
    }

    // The parameter only selects the failure case of the union
    // ReSharper disable once UnusedParameter.Local
    public OppoResult(OppoFailure failure)
    {
        _result = default!;
        _kind = ResultKind.Failure;
    }

    // ReSharper disable once UnusedMember.Global
    public bool HasValue => _kind != ResultKind.Result || _result is not null;

    public object? Value => _kind switch
    {
        ResultKind.Result => _result,
        ResultKind.NoResult => default(OppoNoResult),
        _ => default(OppoFailure)
    };

    public bool TryGetValue([MaybeNullWhen(false)] out TResult value)
    {
        value = _result;
        return _kind == ResultKind.Result && _result is not null;
    }

    // ReSharper disable once UnusedMember.Global
    public bool TryGetValue(out OppoNoResult value)
    {
        value = default;
        return _kind == ResultKind.NoResult;
    }

    // ReSharper disable once UnusedMember.Global
    public bool TryGetValue(out OppoFailure value)
    {
        value = default;
        return _kind == ResultKind.Failure;
    }

    private enum ResultKind : byte
    {
        Failure,
        Result,
        NoResult
    }
}
