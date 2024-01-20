public static class BinaryReaderExtensions
{
    public static int ReadInt24BE(this BinaryReader reader)
    {
        var bytes = reader.ReadBytes(3);
        if (bytes.Length != 3)
        {
            throw new EndOfStreamException("Could not read 3 bytes from stream.");
        }

        int result = (bytes[0] << 16) | (bytes[1] << 8) | bytes[2];

        return result;
    }
}

public static class BinaryWriterExtensions
{
    public static void WriteInt24BE(this BinaryWriter writer, int value)
    {
        if (value < 0 || value > 0xFFFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 16777215.");
        }

        var bytes = new byte[3];
        bytes[0] = (byte)((value >> 16) & 0xFF);
        bytes[1] = (byte)((value >> 8) & 0xFF);
        bytes[2] = (byte)(value & 0xFF);

        writer.Write(bytes);
    }

    public static void WriteInt32LE(this BinaryWriter writer, int value)
    {
        var bytes = BitConverter.GetBytes(value);

        if (BitConverter.IsLittleEndian == false)
        {
            Array.Reverse(bytes);
        }

        writer.Write(bytes);
    }
}
