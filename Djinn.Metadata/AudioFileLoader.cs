namespace Djinn.Metadata;

public class AudioFileLoader
{
    public static AudioFile Load(string path)
    {
        var extension = Path.GetExtension(path);

        var audioFile = extension switch
        {
            ".flac" => new FlacFile(path),
            ".mp3" => throw new NotImplementedException(),
            _ => throw new NotSupportedException($"The file extension {extension} is not supported.")
        };

        audioFile.Load();

        return audioFile;
    }
}
