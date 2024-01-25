namespace Djinn.Metadata;

public class AudioFileLoader
{
    public static AudioFile Load(Stream stream)
    {
        var fileExtension = DetectFileType(stream);

        if (fileExtension == null)
        {
            throw new NotSupportedException("File format not supported.");
        }
        
        AudioFile audioFile = fileExtension switch
        {
            ".flac" => new FlacFile(stream),
            ".mp3" => new Mp3File(stream),
            _ => throw new NotSupportedException($"The file extension {fileExtension} is not supported.")
        };

        audioFile.Load();

        return audioFile;
    }
    
    private static string? DetectFileType(Stream stream)
    {
        var header = new byte[4];
        
        stream.Seek(0, SeekOrigin.Begin);
        var bytesRead = stream.Read(header, 0, 4);
        stream.Seek(0, SeekOrigin.Begin);

        if (bytesRead != 4)
        {
            return null;
        }

        if (IsFlac(header))
        {
            return ".flac";
        }

        if (IsMp3(header))
        {
            return ".mp3";
        }

        return null;
    }
    
    private static bool IsFlac(IReadOnlyList<byte> header)
    {
        return header[0] == 0x66 && header[1] == 0x4C && header[2] == 0x61 && header[3] == 0x43;
    }

    private static bool IsMp3(IReadOnlyList<byte> header)
    {
        if (header[0] == 0x49 && header[1] == 0x44 && header[2] == 0x33)
        {
            return true;
        }
        
        if (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0)
        {
            return true;
        }
        
        return false;
    }
}
