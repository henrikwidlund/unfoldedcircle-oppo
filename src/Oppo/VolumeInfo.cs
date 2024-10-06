using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace Oppo;

[StructLayout(LayoutKind.Auto)]
public record struct VolumeInfo([Range(0, 100)] ushort? Volume, bool Muted);