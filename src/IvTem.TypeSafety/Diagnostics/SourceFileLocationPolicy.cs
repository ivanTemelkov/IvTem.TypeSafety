using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace IvTem.TypeSafety.Diagnostics;

internal static class SourceFileLocationPolicy
{
    public static bool IsAnalyzable(Location location)
    {
        if (location.IsInSource == false)
            return false;

        var filePath = location.SourceTree?.FilePath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        if (Path.IsPathRooted(filePath) == false)
            return false;

        return IsGeneratedFilePath(filePath) == false;
    }

    private static bool IsGeneratedFilePath(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            return true;

        if (fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase))
            return true;

        if (fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase))
            return true;

        if (fileName.Equals("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".GlobalUsings.g.cs", StringComparison.OrdinalIgnoreCase))
            return true;

        var normalizedPath = filePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var generatedDirectorySegment = Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar;
        return normalizedPath.IndexOf(generatedDirectorySegment, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
