namespace UnfoldedCircle.Models.Sync;

public record MediaPlayerEntityCommandMsgData<TCommandId> : CommonReq<EntityCommandMsgData<TCommandId, MediaPlayerEntityCommandParams>>
    where TCommandId : struct, Enum;