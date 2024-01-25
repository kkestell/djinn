namespace Djinn.Metadata.Tests;

public abstract class AudioFileTests
{
    protected Stream LoadResource(string fileName)
    {
        var resourcePath = $"Djinn.Metadata.Tests.TestData.{fileName}";
        var assembly = typeof(FlacTests).Assembly;
        var resourceStream = assembly.GetManifestResourceStream(resourcePath);

        if (resourceStream == null)
        {
            throw new Exception($"Resource '{resourcePath}' not found.");
        }

        var memoryStream = new MemoryStream();
        resourceStream.CopyTo(memoryStream);
        
        return memoryStream;
    }
}