using System.Text;
using System.Text.RegularExpressions;
using File = Soulseek.File;

namespace Djinn.Utils;

internal static class PathUtils
{
    public static string SanitizePath(string path) => 
        Regex.Replace(path, "[\\\\/:*?\"<>|]", "_");

    public static string SoulseekFilenameWithoutExtension(File file) => 
        Path.GetFileNameWithoutExtension(SoulseekFilename(file));
    
    public static string SoulseekFilename(File file) => 
        file.Filename.Replace('\\', '/');
    
    public static string NormalizePath(string path) =>
        path.Normalize(NormalizationForm.FormC);
}