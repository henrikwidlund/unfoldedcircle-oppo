namespace UnfoldedCircle.Models.Sync;

public record RemoteEntityCommandMsgData : CommonReq<EntityCommandMsgData<string, RemoteEntityCommandParams>>;