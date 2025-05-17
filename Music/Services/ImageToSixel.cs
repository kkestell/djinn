using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixPix;

namespace Music.Services;

public static class ImageToSixel
{
    public static ReadOnlySpan<char> Encode(FileInfo imageFile, int width)
    {
        // resize the image
        using var image = Image.Load(imageFile.FullName);
        var originalWidth = image.Width;
        var originalHeight = image.Height;
        var targetWidth = width;
        var targetHeight = (int)(width * (double)originalHeight / originalWidth);
        image.Mutate(x => x.Resize(targetWidth, targetHeight));
        // save to temp file with the same format and extension as the original
        var tempFile = Path.GetTempFileName();
        var tempFileExtension = Path.GetExtension(imageFile.FullName);
        var tempFileName = Path.ChangeExtension(tempFile, tempFileExtension);
        File.Move(tempFile, tempFileName);
        tempFile = tempFileName;
        image.Save(tempFile);
        // close the image
        // convert to sixel
        var tempFileStream = File.OpenRead(tempFile);
        var sixel = Sixel.Encode(tempFileStream);
        tempFileStream.Close();
        File.Delete(tempFile);
        // return sixel
        return sixel;
    }
}