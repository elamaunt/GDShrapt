# GDShrapt.Reader

A high-performance, object-oriented one-pass parser for GDScript 2.0 (Godot 4.x).

## Features

- **Full GDScript 4.x support** - Lambdas, await, typed arrays/dictionaries, pattern matching, all annotations
- **One-pass parsing** - High-performance character-by-character parsing with no backtracking
- **Error recovery** - Invalid syntax creates `GDInvalidToken` nodes instead of throwing exceptions
- **Format preservation** - Comments and whitespace are preserved in the AST
- **Position tracking** - All tokens have `StartLine`, `EndLine`, `StartColumn`, `EndColumn` properties
- **Cloning support** - Clone entire syntax trees or individual nodes

## Quick Start

```csharp
using GDShrapt.Reader;

var reader = new GDScriptReader();
var code = @"
extends Node2D

@export var health: int = 100

func _ready():
    print(""Hello, Godot 4!"")
";

var classDecl = reader.ParseFileContent(code);

// Access class information
Console.WriteLine(classDecl.Extends?.Type);           // "Node2D"
Console.WriteLine(classDecl.Variables.First().Identifier); // "health"
Console.WriteLine(classDecl.Methods.First().Identifier);   // "_ready"
```

## Parsing Methods

```csharp
var reader = new GDScriptReader();

// Parse complete class files
var classDecl = reader.ParseFileContent(code);

// Parse single expression
var expr = reader.ParseExpression("10 + 20 * 3");

// Parse statements
var statements = reader.ParseStatements("var x = 10\nprint(x)");
```

## Helper Classes

```csharp
// Check annotation types
if (GDAnnotationHelper.IsExport(attr))
    Console.WriteLine("Exported variable");

// Check virtual method names
if (GDSpecialMethodHelper.IsReady(method))
    Console.WriteLine("This is _ready()");

// Analyze expressions
if (GDExpressionHelper.IsPreload(expr))
    Console.WriteLine("Preload call detected");
```

## Related Packages

| Package | Description |
|---------|-------------|
| [GDShrapt.Builder](https://www.nuget.org/packages/GDShrapt.Builder) | Fluent API for code generation |
| [GDShrapt.Validator](https://www.nuget.org/packages/GDShrapt.Validator) | Compiler-style AST validation |
| [GDShrapt.Linter](https://www.nuget.org/packages/GDShrapt.Linter) | Style checking and naming conventions |
| [GDShrapt.Formatter](https://www.nuget.org/packages/GDShrapt.Formatter) | Code formatting with type inference |

## Documentation

Full documentation and examples: [GitHub Repository](https://github.com/elamaunt/GDShrapt)

## License

MIT License - see [LICENSE](https://github.com/elamaunt/GDShrapt/blob/main/LICENSE) for details.
