using System.Runtime.InteropServices;

using Oppo;

namespace UnfoldedCircle.OppoBluRay.Configuration;

[StructLayout(LayoutKind.Auto)]
public record struct EntityIdDeviceId(string EntityId, string? DeviceId, in OppoModel Model);