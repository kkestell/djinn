namespace Djinn.Services;

using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

public static class ImageToAnsiConverter
{
    private const double CharacterAspectRatio = 0.4;

    public static string ImageToAnsi(FileInfo imageFileInfo, int desiredSize)
    {
        using var image = Image.Load<Rgba32>(imageFileInfo.FullName);
        
        var originalWidth = image.Width;
        var originalHeight = image.Height;

        var targetWidth = desiredSize;
        var targetHeight = (int)(desiredSize * CharacterAspectRatio);
        var targetAspectRatio = (double)targetWidth / targetHeight;
        var originalAspectRatio = (double)originalWidth / originalHeight;
        if (originalAspectRatio > targetAspectRatio)
        {
            var newWidth = (int)(originalHeight * targetAspectRatio);
            var xOffset = (originalWidth - newWidth) / 2;
            image.Mutate(x => x.Crop(new Rectangle(xOffset, 0, newWidth, originalHeight)));
        }
        else if (originalAspectRatio < targetAspectRatio)
        {
            var newHeight = (int)(originalWidth / targetAspectRatio);
            var yOffset = (originalHeight - newHeight) / 2;
            image.Mutate(x => x.Crop(new Rectangle(0, yOffset, originalWidth, newHeight)));
        }
        image.Mutate(x => x.Resize(targetWidth, targetHeight));

        Console.OutputEncoding = Encoding.UTF8;
        var outputBuilder = new StringBuilder();

        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                var brightness = (byte)((pixel.R + pixel.G + pixel.B) / 3);
                var character = CharsByCalculatedDensity[brightness];
                var ansiColor = $"\x1b[38;2;{pixel.R};{pixel.G};{pixel.B};48;2;0;0;0m";
                outputBuilder.Append(ansiColor);
                outputBuilder.Append(character);
            }

            outputBuilder.Append("\x1b[0m");
            outputBuilder.AppendLine();
        }

        return outputBuilder.ToString().Trim();
    }
    
    private static readonly char[] CharsByCalculatedDensity =
    [
        ' ', '·', '˙', '‥', '.', '_', '˛', '\'', '`', '´',
        'ˇ', '˘', ',', '¸', ':', '-', '¯', '¨', '˚', '˜', 
        '°', '∘', '˝', '^', '¬', '¹', '⌐', '†', '‡', ';', 
        '~', '!', 'ⁿ', '₀', '"', '¡', '+', '†', '¤', 'r', 
        '|', '¦', '1', '=', '∞', '÷', '∙', '∆', '∂', '∫', 
        '≠', '<', '>', '²', '⁴', 'º', '˚', 'l', 'τ', 'π', 
        '/', '\\', 'I', 'L', 'T', '*', '³', 'Γ', '≈', '∼',
        '∛', '×', 'ª', '⁵', 'i', 'v', '7', 'Y', 't', '(', 
        ')', 'F', '⌠', '⌡', '∫', '∮', 'f', '?', '¿', '±', 
        'ì', '¦', '∓', 'J', 'í', 'j', '{', '}', 'c', 'u', 
        'n', 'y', '[', ']', 'z', 'x', 'o', '4', '«', '»', 
        'ε', '≡', '◊', '‹', '›', '∈', '≣', 'π', '≥', '≤', 
        '√', 'î', '⊃', '⊂', '∝', 's', 'V', 'σ', 'ï', 'Þ', 
        'C', 'E', 'P', 'h', 'Σ', 'Z', 'e', 'a', 'A', 'U', 
        '2', '∩', '£', 'ƒ', '₧', '∪', 'k', '3', 'µ', '∇', 
        'p', 'X', 'b', 'd', 'q', 'w', 'H', 'α', '¥', 'ò', 
        'ù', 'ó', 'ú', 'S', 'K', 'O', 'G', '5', 'ç', '¼', 
        'ø', 'Ÿ', '⅓', 'D', 'δ', 'æ', 'Æ', 'û', '∞', '∏', 
        'Δ', 'Ð', '9', 'φ', 'É', 'ô', 'þ', 'ž', 'Φ', 'Ç', 
        'ü', 'é', 'à', 'è', 'ÿ', '¢', 'á', 'ñ', 'œ', 'm', 
        '6', '8', 'Φ', 'Θ', 'Ω', 'Å', 'ö', '½', 'ð', 'Œ', 
        'š', 'Ψ', 'Λ', 'Υ', '⅔', 'g', 'B', '¶', 'ß', '#', 
        'å', 'Ð', '∑', '∏', 'R', '0', 'â', 'ê', 'N', 'Q', 
        '&', '¾', '★', 'Ξ', 'ä', 'ë', 'W', '$', 'Ä', 'Ü', 
        'Š', '€', '₩', 'M', 'Ж', '¢', 'Ø', 'Ö', '§', '©', 
        '%', '®', '@', '∏', 'Ñ', '¥'
    ];
}
