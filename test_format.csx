#r "src/GDShrapt.Reader/bin/Debug/netstandard2.0/GDShrapt.Reader.dll"
using GDShrapt.Reader;
using System;
using System.IO;
using System.Linq;

var reader = new GDScriptReader();
var formatter = new GDFormatter();
var path = Path.Combine("src/GDShrapt.Reader.Tests/Scripts", "Sample.gd");
var code = File.ReadAllText(path);

// Parse original
var tree = reader.ParseFileContent(code);
Console.WriteLine($"Original invalid tokens: {tree.AllInvalidTokens.Count()}");

// Format 
var formatted = formatter.Format(tree.ToString());
Console.WriteLine($"\n=== Formatted around line 110 ===");
var lines = formatted.Split('\n');
for (int i = 107; i < Math.Min(115, lines.Length); i++)
{
    Console.WriteLine($"{i+1}: {lines[i]}");
}

// Parse formatted
var tree2 = reader.ParseFileContent(formatted);
Console.WriteLine($"\nFormatted invalid tokens: {tree2.AllInvalidTokens.Count()}");
foreach (var invalid in tree2.AllInvalidTokens.Take(10))
{
    Console.WriteLine($"  {invalid.StartLine}.{invalid.StartColumn}: {invalid.Sequence}");
}
