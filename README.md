# GDShrapt

[![NuGet](https://img.shields.io/nuget/v/GDShrapt.Reader.svg)](https://www.nuget.org/packages/GDShrapt.Reader)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**GDShrapt** is a high-performance, object-oriented one-pass parser for GDScript 2.0 (Godot 4.x). It can build a lexical tree from GDScript code or generate new code programmatically.

GDScript is the main scripting language of [Godot Engine](https://github.com/godotengine/godot).

## Features

- **Full GDScript 4.x Support** - Lambdas, await, typed arrays/dictionaries, pattern matching, all annotations
- **AST Validation** - Compiler-style diagnostics with error codes (GD1xxx-GD5xxx)
- **One-Pass Parsing** - High-performance lexical analysis
- **Code Generation** - Fluent Building API for creating GDScript code programmatically
- **Helper Classes** - Convenient utilities for annotations, virtual methods, and expressions
- **Format Preservation** - Comments and formatting are preserved during parsing
- **Tree Walking** - Visitor pattern support for AST traversal

## Installation

Install from [NuGet](https://www.nuget.org/packages/GDShrapt.Reader):

```bash
dotnet add package GDShrapt.Reader
```

Or via Package Manager Console:

```powershell
Install-Package GDShrapt.Reader
```

## Quick Start

### Parsing GDScript

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
Console.WriteLine(classDecl.Extends?.Type); // "Node2D"
Console.WriteLine(classDecl.Variables.First().Identifier); // "health"
Console.WriteLine(classDecl.Methods.First().Identifier); // "_ready"
```

### Building GDScript

```csharp
using GDShrapt.Reader;

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

### Using Helper Classes

```csharp
// Check if a method is a lifecycle callback
var method = classDecl.Methods.First();
if (GDSpecialMethodHelper.IsReady(method))
    Console.WriteLine("This is the _ready method");

// Check annotation types
var attr = variable.AttributesDeclaredBefore.First().Attribute;
if (GDAnnotationHelper.IsExport(attr))
    Console.WriteLine("This variable is exported");

// Analyze expressions
var expr = reader.ParseExpression("preload(\"res://scene.tscn\")") as GDCallExpression;
if (GDExpressionHelper.IsPreload(expr))
    Console.WriteLine("This is a preload call");
```

### Validating GDScript

```csharp
using GDShrapt.Reader;

var reader = new GDScriptReader();
var code = @"
func test():
    break  # Error: break outside loop
    print(undefined_var)  # Error: undefined variable
";

var tree = reader.ParseFileContent(code);
var validator = new GDValidator();
var result = validator.Validate(tree);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        // Output: "error GD5001: 'break' can only be used inside a loop (3:4)"
        Console.WriteLine(error.ToString());
    }
}
```

## Supported GDScript Features

| Feature | Status |
|---------|--------|
| Class declarations | Supported |
| Methods (func, static func) | Supported |
| Variables (var, const, static var) | Supported |
| Properties (get/set) | Supported |
| Signals | Supported |
| Enums | Supported |
| Inner classes | Supported |
| Annotations (@export, @onready, @tool, etc.) | Supported |
| Typed arrays and dictionaries | Supported |
| Lambda expressions | Supported |
| Await expressions | Supported |
| Pattern matching (match/when) | Supported |
| All statements (if/elif/else, for, while, match) | Supported |
| All expressions and operators | Supported |
| Comments preservation | Supported |
| GetNode ($) and GetUniqueNode (%) | Supported |

## Validation

The `GDValidator` class provides comprehensive AST validation with compiler-style diagnostics.

### Diagnostic Categories

| Category | Code Range | Description |
|----------|------------|-------------|
| Syntax | GD1xxx | Invalid tokens, missing brackets, unexpected tokens |
| Scope | GD2xxx | Undefined variables, duplicate declarations, undefined functions |
| Type | GD3xxx | Type mismatches, invalid operand types |
| Call | GD4xxx | Wrong argument counts, method not found |
| Control Flow | GD5xxx | break/continue outside loops, return outside functions |

### Validation Options

```csharp
var options = new GDValidationOptions
{
    CheckSyntax = true,      // GD1xxx errors
    CheckScope = true,       // GD2xxx errors
    CheckTypes = true,       // GD3xxx warnings
    CheckCalls = true,       // GD4xxx errors
    CheckControlFlow = true  // GD5xxx errors
};

var result = validator.Validate(tree, options);
```

### Working with Results

```csharp
var result = validator.Validate(tree);

// Check overall status
if (result.IsValid) { /* No errors */ }
if (result.HasErrors) { /* Has errors */ }
if (result.HasWarnings) { /* Has warnings */ }

// Filter by severity
foreach (var error in result.Errors) { ... }
foreach (var warning in result.Warnings) { ... }

// Detailed location info
var diag = result.Diagnostics.First();
Console.WriteLine(diag.CodeString);        // "GD5001"
Console.WriteLine(diag.ToString());        // "error GD5001: message (3:4)"
Console.WriteLine(diag.ToDetailedString()); // "error GD5001 at 3:4-3:9: message"
```

## Building API Examples

### Creating Annotations

```csharp
GD.Atribute.Export()                                    // @export
GD.Atribute.ExportRange(GD.Expression.Number(0),
                        GD.Expression.Number(100))      // @export_range(0, 100)
GD.Atribute.Onready()                                   // @onready
GD.Atribute.ExportGroup("Stats")                        // @export_group("Stats")
GD.Atribute.Rpc(GD.Expression.String("any_peer"))       // @rpc("any_peer")
```

### Creating Declarations

```csharp
// Enum
GD.Declaration.Enum("State",
    GD.Declaration.EnumValue("IDLE"),
    GD.Declaration.EnumValue("RUNNING"),
    GD.Declaration.EnumValue("JUMPING", GD.Expression.Number(10))
);

// Signal
GD.Declaration.Signal("health_changed",
    GD.Declaration.Parameter("new_health", GD.Syntax.TypeNode("int")));
```

### Creating Expressions

```csharp
GD.Expression.GetUniqueNode("Player")                   // %Player
GD.Expression.GetNode(GD.Syntax.Identifier("Sprite"))   // $Sprite
GD.Expression.Lambda(GD.Expression.Identifier("x"))     // func(): x
GD.Expression.Await(GD.Expression.Identifier("signal")) // await(signal)
```

## API Reference

### Core Classes

- `GDScriptReader` - Main parser class
- `GDClassDeclaration` - Represents a GDScript file/class
- `GDMethodDeclaration` - Method declaration
- `GDVariableDeclaration` - Variable/constant declaration
- `GDExpression` - Base class for all expressions
- `GDStatement` - Base class for all statements

### Helper Classes

- `GDAnnotationHelper` - Identify annotation types (@export, @onready, etc.)
- `GDSpecialMethodHelper` - Identify virtual methods (_ready, _process, etc.)
- `GDExpressionHelper` - Analyze expressions (preload, print, math functions, etc.)

### Building API

- `GD.Declaration` - Create declarations (Class, Method, Variable, Enum, Signal)
- `GD.Expression` - Create expressions (Call, Array, Dictionary, Lambda, etc.)
- `GD.Statement` - Create statements (If, For, While, Match, Return, etc.)
- `GD.Atribute` - Create annotations (Export, Onready, Tool, etc.)
- `GD.List` - Create lists (Parameters, Expressions, Statements)
- `GD.Syntax` - Create syntax tokens (Identifiers, Types, Operators)

### Validation Classes

- `GDValidator` - Main validation class
- `GDValidationResult` - Collection of diagnostics with filtering
- `GDDiagnostic` - Single diagnostic with location and message
- `GDDiagnosticCode` - Enum of all diagnostic codes (GD1001-GD5006)
- `GDDiagnosticSeverity` - Error, Warning, Hint
- `GDValidationOptions` - Configure which validators to run

## Examples

For more examples, see the [test files](src/GDShrapt.Reader.Tests/):
- [ParsingTests.cs](src/GDShrapt.Reader.Tests/ParsingTests.cs) - Parsing examples
- [BuildingTests.cs](src/GDShrapt.Reader.Tests/BuildingTests.cs) - Code generation examples
- [HelperTests.cs](src/GDShrapt.Reader.Tests/HelperTests.cs) - Helper classes examples
- [ValidationTests.cs](src/GDShrapt.Reader.Tests/ValidationTests.cs) - Validation examples

## Changelog

### 5.0.0
- **NEW: AST Validation System** with compiler-style diagnostics
  - Syntax validation (GD1xxx): Invalid tokens, missing brackets
  - Scope validation (GD2xxx): Undefined variables, duplicate declarations
  - Type validation (GD3xxx): Type mismatches, invalid operands
  - Call validation (GD4xxx): Wrong argument counts for built-in functions
  - Control flow validation (GD5xxx): break/continue outside loops, return outside functions
- Comprehensive test coverage (198 tests)

### 4.5.0
- Full GDScript 4.x support
- Fixed property get/set parsing (Issues #10, #11)
- Added typed dictionaries support (Godot 4.4)
- Added helper classes: `GDAnnotationHelper`, `GDSpecialMethodHelper`, `GDExpressionHelper`
- Extended Building API: `GetUniqueNode`, `Enum`, `EnumValue`, Export annotations

### 4.4.0-alpha
- Added typed Dictionaries support (thanks to dougVanny)
- Various QoL updates and bugfixes

### 4.3.2-alpha
- Fixed line breaks inside brackets, enums, dictionaries, arrays
- Fixed comments parsing inside brackets

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Godot Engine](https://godotengine.org/) team for the amazing game engine
- All contributors who have helped improve this project
