namespace UnfoldedCircle.Models.Sync;

public record EntityStates<TAttribute> : CommonRespRequired<EntityStateChanged<TAttribute>[]>
    where TAttribute : struct, Enum;