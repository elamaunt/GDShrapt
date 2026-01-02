# GDShrapt.Builder

Fluent API for programmatic GDScript 2.0 (Godot 4.x) code generation.

## Features

- **Fluent building API** - Create complete GDScript AST nodes with chainable methods
- **Factory methods** - All declarations, expressions, and statements
- **Three building styles** - Short, Fluent, and Token-based approaches
- **Full GDScript 4.x** - Lambdas, annotations, typed arrays, enums

## Quick Start

```csharp
using GDShrapt.Reader;

// Create a simple class
var classDecl = GD.Declaration.Class(
    GD.Atribute.Extends("Node2D"),
    GD.Atribute.Export(),
    GD.Declaration.Variable("health", "int", GD.Expression.Number(100)),
    GD.Declaration.Method(GD.Syntax.Identifier("_ready"),
        GD.Expression.Call(GD.Expression.Identifier("print"),
            GD.Expression.String("Hello, Godot 4!")).ToStatement()
    )
);

classDecl.UpdateIntendation();
Console.WriteLine(classDecl.ToString());
```

## Building Styles

### Short Style
```csharp
var variable = GD.Declaration.Variable("health", "int", GD.Expression.Number(100));
```

### Fluent Style
```csharp
var classDecl = GD.Declaration.Class()
    .WithExtends("Node2D")
    .AddVariable("health", "int")
    .AddMethod("_ready");
```

### Token-based Style
```csharp
var variable = new GDVariableDeclaration();
variable.VarKeyword = new GDVarKeyword();
variable.Identifier = new GDIdentifier { Sequence = "health" };
```

## Common Patterns

### Annotations
```csharp
GD.Atribute.Export()                    // @export
GD.Atribute.ExportRange(0, 100)         // @export_range(0, 100)
GD.Atribute.Onready()                   // @onready
GD.Atribute.Tool()                      // @tool
```

### Expressions
```csharp
GD.Expression.GetNode("Sprite")         // $Sprite
GD.Expression.GetUniqueNode("Player")   // %Player
GD.Expression.Lambda(body)              // func(): body
GD.Expression.Await(signal)             // await signal
```

### Statements
```csharp
GD.Statement.If(condition, thenStatements)
GD.Statement.For("i", range, body)
GD.Statement.Match(expression, cases)
```

## Related Packages

| Package | Description |
|---------|-------------|
| [GDShrapt.Reader](https://www.nuget.org/packages/GDShrapt.Reader) | Core parser and AST (required) |
| [GDShrapt.Validator](https://www.nuget.org/packages/GDShrapt.Validator) | Compiler-style AST validation |
| [GDShrapt.Linter](https://www.nuget.org/packages/GDShrapt.Linter) | Style checking and naming conventions |
| [GDShrapt.Formatter](https://www.nuget.org/packages/GDShrapt.Formatter) | Code formatting with type inference |

## Documentation

Full documentation and examples: [GitHub Repository](https://github.com/elamaunt/GDShrapt)

## License

MIT License - see [LICENSE](https://github.com/elamaunt/GDShrapt/blob/main/LICENSE) for details.
