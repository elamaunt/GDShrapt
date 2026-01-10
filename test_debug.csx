#r "src/GDShrapt.Reader/bin/Debug/net8.0/GDShrapt.Reader.dll"
using GDShrapt.Reader;
using System.Linq;

var reader = new GDScriptReader();
var classDecl = reader.ParseFileContent(@"extends Node
func test():
    $Player
");

Console.WriteLine("All tokens:");
foreach (var token in classDecl.AllTokens)
{
    Console.WriteLine($"  {token.GetType().Name}: '{token}' at L{token.StartLine}:C{token.StartColumn}-{token.EndColumn}");
}

Console.WriteLine("\nAll GDGetNodeExpression nodes:");
foreach (var node in classDecl.AllNodes.OfType<GDGetNodeExpression>())
{
    Console.WriteLine($"  Found at L{node.StartLine}:C{node.StartColumn}-{node.EndColumn}: {node}");
}

Console.WriteLine("\nTrying TryGetTokenByPosition(2, 4):");
if (classDecl.TryGetTokenByPosition(2, 4, out var token1))
    Console.WriteLine($"  Found: {token1.GetType().Name} '{token1}', Parent: {token1.Parent?.GetType().Name}");
else
    Console.WriteLine("  Not found");

Console.WriteLine("\nTrying TryGetTokenByPosition(2, 5):");
if (classDecl.TryGetTokenByPosition(2, 5, out var token2))
    Console.WriteLine($"  Found: {token2.GetType().Name} '{token2}', Parent: {token2.Parent?.GetType().Name}");
else
    Console.WriteLine("  Not found");

Console.WriteLine("\nTrying TryGetTokenByPosition(2, 6):");
if (classDecl.TryGetTokenByPosition(2, 6, out var token3))
    Console.WriteLine($"  Found: {token3.GetType().Name} '{token3}', Parent: {token3.Parent?.GetType().Name}");
else
    Console.WriteLine("  Not found");
