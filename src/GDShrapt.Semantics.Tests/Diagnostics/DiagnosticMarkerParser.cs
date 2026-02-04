using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GDShrapt.Semantics.Tests.Diagnostics;

/// <summary>
/// Parses diagnostic verification markers from GDScript files.
/// Format: # LINE:COL-CODE-SUFFIX (e.g., # 33:5-GD3001-OK)
/// </summary>
public class DiagnosticMarkerParser
{
    /// <summary>
    /// Regex pattern for diagnostic markers.
    /// Format: LINE:COL-CODE-SUFFIX
    /// Examples: 33:5-GD3001-OK, 45:6-GDL201-FP, 50:10-GD7007-SKIP
    /// </summary>
    private static readonly Regex MarkerPattern =
        new(@"(\d+):(\d+)-(GD[L]?\d{3,4})-(OK|FP|SKIP)", RegexOptions.Compiled);

    /// <summary>
    /// Type of verification marker.
    /// </summary>
    public enum MarkerType
    {
        /// <summary>Verified as correct (true positive)</summary>
        OK,

        /// <summary>False positive - bug in analyzer</summary>
        FP,

        /// <summary>Skipped - needs further analysis</summary>
        Skip
    }

    /// <summary>
    /// A single diagnostic marker.
    /// </summary>
    public record DiagnosticMarker(
        int Line,
        int Column,
        string Code,
        MarkerType Type
    );

    /// <summary>
    /// Parses all markers from a GDScript file.
    /// Returns dictionary: (line, col, code) -> MarkerType
    /// </summary>
    public Dictionary<(int Line, int Col, string Code), MarkerType> ParseFile(string filePath)
    {
        var result = new Dictionary<(int, int, string), MarkerType>();

        if (!File.Exists(filePath))
            return result;

        var lines = File.ReadAllLines(filePath);

        foreach (var line in lines)
        {
            // Find comment with markers
            var commentIndex = line.IndexOf('#');
            if (commentIndex < 0) continue;

            var comment = line.Substring(commentIndex);
            var matches = MarkerPattern.Matches(comment);

            foreach (Match match in matches)
            {
                var lineNum = int.Parse(match.Groups[1].Value);
                var colNum = int.Parse(match.Groups[2].Value);
                var code = match.Groups[3].Value;
                var typeStr = match.Groups[4].Value;

                var type = typeStr switch
                {
                    "OK" => MarkerType.OK,
                    "FP" => MarkerType.FP,
                    "SKIP" => MarkerType.Skip,
                    _ => MarkerType.Skip
                };

                result[(lineNum, colNum, code)] = type;
            }
        }

        return result;
    }

    /// <summary>
    /// Parses all markers from all GDScript files in a directory (recursively).
    /// Returns dictionary: filePath -> (line, col, code) -> MarkerType
    /// </summary>
    public Dictionary<string, Dictionary<(int Line, int Col, string Code), MarkerType>> ParseDirectory(string directoryPath)
    {
        var result = new Dictionary<string, Dictionary<(int, int, string), MarkerType>>();

        if (!Directory.Exists(directoryPath))
            return result;

        var gdFiles = Directory.GetFiles(directoryPath, "*.gd", SearchOption.AllDirectories);

        foreach (var file in gdFiles)
        {
            var markers = ParseFile(file);
            if (markers.Count > 0)
            {
                result[file] = markers;
            }
        }

        return result;
    }
}
