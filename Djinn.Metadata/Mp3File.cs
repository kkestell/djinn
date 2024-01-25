using System.Text;

namespace Djinn.Metadata;

public class Mp3File(Stream stream) : AudioFile(stream)
{
    private long _audioDataStart;
    private byte[]? _id3v2Data;
    
    public static void ParseID3v2(byte[] data)
    {
        if (data.Length < 10)
        {
            Console.WriteLine("Data is too short to be a valid ID3v2 tag.");
            return;
        }

        string identifier = System.Text.Encoding.ASCII.GetString(data, 0, 3);
        if (identifier != "ID3")
        {
            Console.WriteLine("Invalid ID3v2 identifier.");
            return;
        }

        var version = data[3];
        var revision = data[4];
        var flags = data[5];
        var size = SynchsafeToInt(data, 6);

        Console.WriteLine($"Identifier: {identifier}");
        Console.WriteLine($"Version: {version}.{revision}");
        Console.WriteLine($"Flags: {Convert.ToString(flags, 2).PadLeft(8, '0')}");
        Console.WriteLine($"Size: {size}");
        
        int position = 10; // Starting position after the header
        while (position + 10 <= data.Length)
        {
            string frameId = Encoding.ASCII.GetString(data, position, 4);
            int frameSize;
            if (version < 4)
            {
                // ID3v2.3 and earlier: size is in big-endian
                frameSize = BitConverter.ToInt32(new byte[] { data[position + 7], data[position + 6], data[position + 5], data[position + 4] }, 0);
            }
            else
            {
                // ID3v2.4: size is a synchsafe integer
                frameSize = SynchsafeToInt(data, position + 4);
            }
            
            position += 10; // Move past the frame header

            if (position + frameSize > data.Length)
                break;

            // Read frame data
            byte[] frameData = new byte[frameSize];
            Array.Copy(data, position, frameData, 0, frameSize);

            // Process frame
            ProcessFrame(frameId, frameData);

            position += frameSize;
        }
    }
    
    private static void ProcessFrame(string frameId, byte[] frameData)
    {
        // For simplicity, this example only handles text information frames
        if (frameId.StartsWith("T"))
        {
            string text = Encoding.UTF8.GetString(frameData, 1, frameData.Length - 1); // Skip first byte (text encoding)
            Console.WriteLine($"{frameId}: {text}");
        }
    }
    
    private static int ReverseBytes(int value)
    {
        byte[] intBytes = BitConverter.GetBytes(value);
        Array.Reverse(intBytes);
        return BitConverter.ToInt32(intBytes, 0);
    }
    
    private static int SynchsafeToInt(byte[] data, int startIndex)
    {
        return ((data[startIndex] & 0x7F) << 21) |
               ((data[startIndex + 1] & 0x7F) << 14) |
               ((data[startIndex + 2] & 0x7F) << 7) |
               (data[startIndex + 3] & 0x7F);
    }
    
    public override void Load()
    {
        AudioStream.Seek(0, SeekOrigin.Begin);

        using var reader = new BinaryReader(AudioStream, Encoding.Default, leaveOpen: true);

        var header = new byte[10];
        if (reader.Read(header, 0, 10) == 10 && header[0] == 'I' && header[1] == 'D' && header[2] == '3')
        {
            var size = CalculateSize(header[6], header[7], header[8], header[9]);

            // Include the header in the ID3v2 data
            _id3v2Data = new byte[size + 10];
            Array.Copy(header, _id3v2Data, 10);

            // Read the rest of the ID3v2 tag
            if (reader.Read(_id3v2Data, 10, size) != size)
            {
                throw new FormatException("Not a valid ID3v2 tag.");
            }
        }
        else
        {
            // No ID3v2 tag, reset stream position
            AudioStream.Seek(0, SeekOrigin.Begin);
        }

        ParseID3v2(_id3v2Data);
        
        _audioDataStart = reader.BaseStream.Position;
        
        AudioStream.Seek(0, SeekOrigin.Begin);
    }

    public override Stream Save()
    {
        var outputStream = new MemoryStream();
        
        AudioStream.Seek(_audioDataStart, SeekOrigin.Begin);
        AudioStream.CopyTo(outputStream);
        
        return outputStream;
    }
    
    public static (byte[] id3v2Data, byte[] audioData) SplitMp3(Stream mp3Stream)
    {
        byte[] id3v2Data = Array.Empty<byte>();
        byte[] audioData;

        // Check if the stream supports seeking
        if (!mp3Stream.CanSeek)
        {
            throw new InvalidOperationException("Stream must support seeking.");
        }

        try
        {
            // Check for ID3v2 header
            byte[] header = new byte[10];
            if (mp3Stream.Read(header, 0, 10) == 10 && header[0] == 'I' && header[1] == 'D' && header[2] == '3')
            {
                // Calculate the size of the ID3v2 tag
                int size = CalculateSize(header[6], header[7], header[8], header[9]);

                // Include the header in the ID3v2 data
                id3v2Data = new byte[size + 10];
                Array.Copy(header, id3v2Data, 10);

                // Read the rest of the ID3v2 tag
                mp3Stream.Read(id3v2Data, 10, size);
            }
            else
            {
                // No ID3v2 tag, reset stream position
                mp3Stream.Seek(0, SeekOrigin.Begin);
            }

            // Read the remaining audio data
            using (MemoryStream audioDataStream = new MemoryStream())
            {
                mp3Stream.CopyTo(audioDataStream);
                audioData = audioDataStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error processing MP3 stream.", ex);
        }

        return (id3v2Data, audioData);
    }

    private static int CalculateSize(byte byte1, byte byte2, byte byte3, byte byte4)
    {
        // Size is calculated using 7 bits from each byte
        return ((byte1 & 0x7F) << 21) | ((byte2 & 0x7F) << 14) | ((byte3 & 0x7F) << 7) | (byte4 & 0x7F);
    }
}