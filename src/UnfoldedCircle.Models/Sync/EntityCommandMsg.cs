namespace UnfoldedCircle.Models.Sync;

public record EntityCommandMsg<TCommandId> : CommonReq<EntityCommandMsgData<TCommandId>>
    where TCommandId : struct, Enum;