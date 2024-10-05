﻿namespace Makaretu.Dns;

/// <summary>
///   An unknown EDNS option.
/// </summary>
/// <remarks>
///   When an <see cref="EdnsOption"/> is read with a <see cref="EdnsOption.Type"/> that
///   is not <see cref="EdnsOptionRegistry">registered</see>, then this is used
///   to deserialise the information.
/// </remarks>
public class UnknownEdnsOption : EdnsOption
{
    /// <summary>
    ///   Specfic data for the option.
    /// </summary>
    public byte[]? Data { get; set; }

    /// <inheritdoc />
    public override void ReadData(WireReader reader, int length)
    {
        Data = reader.ReadBytes(length);
    }

    /// <inheritdoc />
    public override void WriteData(WireWriter writer)
    {
        writer.WriteBytes(Data);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Data is null)
            throw new InvalidOperationException("The Data property must be set.");
        
        return $";   Type = {Type}; Data = {Convert.ToBase64String(Data)}";
    }
}