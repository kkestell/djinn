using System.Text.RegularExpressions;
using File = Soulseek.File;

namespace Music.Utils;

internal static class PathUtils
{
    public static string SanitizePath(string path) => 
        Regex.Replace(path, "[\\\\/:*?\"<>|]", "_");

    public static string SoulseekFilenameWithoutExtension(File file) => 
        Path.GetFileNameWithoutExtension(SoulseekFilename(file));
    
    public static string SoulseekFilename(File file) => 
        file.Filename.Replace('\\', '/');
}