# GDShrapt.Validator

Compiler-style AST validation for GDScript 2.0 (Godot 4.x) with type inference.

## Features

- **Compiler-style diagnostics** - Error codes (GD1xxx-GD7xxx) with precise locations
- **Type inference** - Infer expression types with `GDTypeInferenceEngine`
- **Custom runtime providers** - Integrate with Godot's type system via `IGDRuntimeProvider`
- **Two-pass validation** - Full support for forward references
- **Configurable checks** - Enable/disable validation categories

## Diagnostic Categories

| Category | Code | Description |
|----------|------|-------------|
| Syntax | GD1xxx | Invalid tokens, missing brackets, unexpected tokens |
| Scope | GD2xxx | Undefined variables, duplicate declarations |
| Type | GD3xxx | Type mismatches, invalid operand types |
| Call | GD4xxx | Wrong argument counts for built-in functions |
| Control Flow | GD5xxx | break/continue outside loops, return outside functions |
| Indentation | GD6xxx | Mixed tabs/spaces, inconsistent indentation |
| Await | GD7xxx | Await expression structure issues |

## Quick Start

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

## Validation Options

```csharp
var options = new GDValidationOptions
{
    CheckSyntax = true,       // GD1xxx errors
    CheckScope = true,        // GD2xxx errors
    CheckTypes = true,        // GD3xxx warnings
    CheckCalls = true,        // GD4xxx errors
    CheckControlFlow = true,  // GD5xxx errors
    CheckIndentation = true,  // GD6xxx warnings
    CheckAwait = true         // GD7xxx errors
};

var result = validator.Validate(tree, options);
```

## Custom Runtime Provider

Integrate with Godot's actual type system:

```csharp
public class GodotRuntimeProvider : IGDRuntimeProvider
{
    public bool IsKnownType(string typeName) => /* check Godot types */;
    public GDRuntimeTypeInfo GetTypeInfo(string typeName) => /* return type info */;
    // ... other methods
}

// Use with caching for performance
var provider = new GDCachingRuntimeProvider(new GodotRuntimeProvider());
var options = new GDValidationOptions { RuntimeProvider = provider };
var result = validator.Validate(tree, options);
```

## Related Packages

| Package | Description |
|---------|-------------|
| [GDShrapt.Reader](https://www.nuget.org/packages/GDShrapt.Reader) | Core parser and AST (required) |
| [GDShrapt.Builder](https://www.nuget.org/packages/GDShrapt.Builder) | Fluent API for code generation |
| [GDShrapt.Linter](https://www.nuget.org/packages/GDShrapt.Linter) | Style checking and naming conventions |
| [GDShrapt.Formatter](https://www.nuget.org/packages/GDShrapt.Formatter) | Code formatting with type inference |

## Documentation

Full documentation and examples: [GitHub Repository](https://github.com/elamaunt/GDShrapt)

## License

MIT License - see [LICENSE](https://github.com/elamaunt/GDShrapt/blob/main/LICENSE) for details.
