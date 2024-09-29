using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Events;

public record DisconnectEvent : CommonEventOptional<DeviceIdObject>;