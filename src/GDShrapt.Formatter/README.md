# GDShrapt.Formatter

Configurable code formatting for GDScript 2.0 (Godot 4.x) with auto type inference.

## Features

- **Rule-based formatting** - 8 configurable formatting rules (GDF001-GDF008)
- **Auto type hints** - Automatically add inferred type hints to untyped variables
- **Style extraction** - Learn formatting style from sample code
- **LSP compatible** - Options map directly to LSP `textDocument/formatting`
- **Presets** - Default, GDScriptStyleGuide, Minimal

## Quick Start

```csharp
using GDShrapt.Reader;

var code = @"func test():
	var x=10+5
	print(x)
";

var formatter = new GDFormatter();
var formatted = formatter.FormatCode(code);
// Result: properly spaced "var x = 10 + 5"
```

## Format Rules

| Rule | Description |
|------|-------------|
| GDF001 | Indentation (tabs/spaces) |
| GDF002 | Blank lines between members |
| GDF003 | Spacing around operators |
| GDF004 | Trailing whitespace removal |
| GDF005 | Line ending normalization |
| GDF006 | Line wrapping for long lines |
| GDF007 | Auto type hints (opt-in) |
| GDF008 | Code member reordering (opt-in) |

## Formatter Options

```csharp
var options = new GDFormatterOptions
{
    IndentStyle = IndentStyle.Tabs,
    IndentSize = 4,
    SpaceAroundOperators = true,
    SpaceAfterComma = true,
    MaxLineLength = 100,
    WrapLongLines = true,
    RemoveTrailingWhitespace = true,
    EnsureTrailingNewline = true
};

var formatter = new GDFormatter(options);
```

## Auto Type Inference (GDF007)

Automatically add type hints using type inference:

```csharp
var options = new GDFormatterOptions
{
    AutoAddTypeHints = true,
    AutoAddTypeHintsToClassVariables = true,
    AutoAddTypeHintsToLocals = true,
    UnknownTypeFallback = "Variant"
};

var formatter = new GDFormatter(options);
var code = "var x = 10\nvar name = \"hello\"";
var result = formatter.FormatCode(code);
// Result: var x: int = 10
//         var name: String = "hello"
```

## Format by Example

Extract style from sample code and apply to other code:

```csharp
var formatter = new GDFormatter();

// Sample with 2-space indentation
var sampleCode = @"func sample():
  var x = 10
";

var codeToFormat = @"func test():
	var y = 20
";

// Will format using 2-space indentation
var result = formatter.FormatCodeWithStyle(codeToFormat, sampleCode);
```

## Presets

```csharp
var formatter = new GDFormatter(GDFormatterOptions.Default);
var formatter = new GDFormatter(GDFormatterOptions.GDScriptStyleGuide);
var formatter = new GDFormatter(GDFormatterOptions.Minimal);
```

## Related Packages

| Package | Description |
|---------|-------------|
| [GDShrapt.Reader](https://www.nuget.org/packages/GDShrapt.Reader) | Core parser and AST (required) |
| [GDShrapt.Validator](https://www.nuget.org/packages/GDShrapt.Validator) | Type inference (required for GDF007) |
| [GDShrapt.Builder](https://www.nuget.org/packages/GDShrapt.Builder) | Fluent API for code generation |
| [GDShrapt.Linter](https://www.nuget.org/packages/GDShrapt.Linter) | Style checking and naming conventions |

## Documentation

Full documentation and examples: [GitHub Repository](https://github.com/elamaunt/GDShrapt)

## License

MIT License - see [LICENSE](https://github.com/elamaunt/GDShrapt/blob/main/LICENSE) for details.
