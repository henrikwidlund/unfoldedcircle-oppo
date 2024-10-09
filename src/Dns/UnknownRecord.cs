namespace Makaretu.Dns;

/// <summary>
///   An unknown resource record.
/// </summary>
public class UnknownRecord : ResourceRecord
{
    /// <summary>
    ///    Specific data for the resource.
    /// </summary>
    public byte[]? Data { get; set; }
    
    /// <inheritdoc />
    public override void ReadData(WireReader reader, int length) => Data = reader.ReadBytes(length);

    /// <inheritdoc />
    public override void ReadData(PresentationReader reader) => Data = reader.ReadResourceData();

    /// <inheritdoc />
    public override void WriteData(WireWriter writer) => writer.WriteBytes(Data);
}