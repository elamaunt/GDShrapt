# GDShrapt

[![NuGet](https://img.shields.io/nuget/v/GDShrapt.Reader.svg)](https://www.nuget.org/packages/GDShrapt.Reader)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Tests](https://img.shields.io/badge/tests-1052%20passed-brightgreen.svg)]()

**GDShrapt** is a high-performance, object-oriented one-pass parser for GDScript 2.0 (Godot 4.x). It provides a complete toolkit for parsing, validating, linting, and formatting GDScript code programmatically.

GDScript is the main scripting language of [Godot Engine](https://github.com/godotengine/godot).

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| **GDShrapt.Reader** | Core parser and AST | [![NuGet](https://img.shields.io/nuget/v/GDShrapt.Reader.svg)](https://www.nuget.org/packages/GDShrapt.Reader) |
| **GDShrapt.Builder** | Fluent API for code generation | [![NuGet](https://img.shields.io/nuget/v/GDShrapt.Builder.svg)](https://www.nuget.org/packages/GDShrapt.Builder) |
| **GDShrapt.Validator** | AST validation with diagnostics | [![NuGet](https://img.shields.io/nuget/v/GDShrapt.Validator.svg)](https://www.nuget.org/packages/GDShrapt.Validator) |
| **GDShrapt.Linter** | Style checking and naming conventions | [![NuGet](https://img.shields.io/nuget/v/GDShrapt.Linter.svg)](https://www.nuget.org/packages/GDShrapt.Linter) |
| **GDShrapt.Formatter** | Code formatting with type inference | [![NuGet](https://img.shields.io/nuget/v/GDShrapt.Formatter.svg)](https://www.nuget.org/packages/GDShrapt.Formatter) |

## Features

- **Full GDScript 4.x Support** - Lambdas, await, typed arrays/dictionaries, pattern matching, all annotations
- **AST Validation** - Compiler-style diagnostics with error codes (GD1xxx-GD5xxx)
- **Style Linter** - GDScript style guide enforcement with configurable naming conventions
- **Code Formatter** - Automatic code formatting with style extraction from samples
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

### Linting GDScript

```csharp
using GDShrapt.Reader;

var code = @"
var MyVariable = 10  # Should be snake_case
const lowercase = 5  # Should be SCREAMING_SNAKE_CASE

func BadName():  # Should be snake_case
    pass
";

var linter = new GDLinter();
var result = linter.LintCode(code);

foreach (var issue in result.Issues)
{
    // Output: "warning GDL101: Variable 'MyVariable' should use snake_case (2:4)"
    Console.WriteLine(issue.ToString());
}
```

### Formatting GDScript

```csharp
using GDShrapt.Reader;

var code = @"func test():
	var x=10+5
	print(x)
";

var formatter = new GDFormatter();
var formatted = formatter.FormatCode(code);
// Result: properly spaced "var x = 10 + 5"

// Format using style extracted from sample code
var sampleCode = @"func sample():
    var y = 20  # Uses 4-space indentation
";
var formattedWithStyle = formatter.FormatCodeWithStyle(code, sampleCode);
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
| Indentation | GD6xxx | Inconsistent indentation (mixed tabs/spaces) |
| Await | GD7xxx | Await structure issues |

**Note:** The validator uses two-pass analysis for scope validation, which fully supports forward references. Methods, variables, signals, and enums can be used before they are declared in the file.

### Validation Options

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

### Custom Runtime Provider

For advanced type checking, you can provide custom runtime type information via `IGDRuntimeProvider`. This allows integration with Godot's actual type system or custom interpreters.

```csharp
// Implement custom provider for your runtime environment
public class GodotRuntimeProvider : IGDRuntimeProvider
{
    public bool IsKnownType(string typeName) => /* check Godot types */;
    public GDRuntimeTypeInfo GetTypeInfo(string typeName) => /* return type info */;
    public GDRuntimeMemberInfo GetMember(string typeName, string memberName) => /* return member */;
    public string GetBaseType(string typeName) => /* return base type */;
    public bool IsAssignableTo(string sourceType, string targetType) => /* check compatibility */;
    public GDRuntimeFunctionInfo GetGlobalFunction(string name) => /* return function info */;
    public GDRuntimeTypeInfo GetGlobalClass(string name) => /* return singleton info */;
    public bool IsBuiltIn(string identifier) => /* check built-in identifiers */;
}

// Use with caching for better performance
var provider = new GDCachingRuntimeProvider(new GodotRuntimeProvider());
var options = new GDValidationOptions { RuntimeProvider = provider };
var result = validator.Validate(tree, options);
```

The default `GDDefaultRuntimeProvider` includes basic GDScript types (int, float, String, Array, Dictionary, Vector2, Vector3, etc.) and common global functions (print, range, load, etc.).

## Linter

The `GDLinter` class enforces the GDScript style guide with configurable rules.

### Lint Rules

| Category | Rules |
|----------|-------|
| Naming | ClassNameCaseRule, FunctionNameCaseRule, VariableNameCaseRule, ConstantNameCaseRule, SignalNameCaseRule, EnumNameCaseRule, EnumValueCaseRule, PrivatePrefixRule |
| Style | LineLengthRule, MemberOrderingRule |
| Best Practices | UnusedVariableRule, UnusedParameterRule, UnusedSignalRule, EmptyFunctionRule, TypeHintRule, MaxParametersRule, MaxFunctionLengthRule, StrictTypingRule |

### Strict Typing Rule (GDL215)

Enforce strict type hints with configurable severity per element:

```csharp
var options = new GDLinterOptions();
options.StrictTypingClassVariables = GDLintSeverity.Warning;
options.StrictTypingLocalVariables = GDLintSeverity.Hint;
options.StrictTypingParameters = GDLintSeverity.Error;
options.StrictTypingReturnTypes = GDLintSeverity.Error;

var linter = new GDLinter(options);
```

### Comment-Based Suppression

Suppress linter warnings using inline comments (gdtoolkit compatible):

```gdscript
# gdlint:ignore = variable-name
var my_Var = 10  # No warning

# gdlint: disable=function-name
func BadName():
    pass
# gdlint: enable=function-name
```

### Linter Options

```csharp
var options = new GDLinterOptions
{
    // Naming conventions
    ClassNameCase = NamingCase.PascalCase,      // MyClass
    FunctionNameCase = NamingCase.SnakeCase,    // my_function
    VariableNameCase = NamingCase.SnakeCase,    // my_variable
    ConstantNameCase = NamingCase.ScreamingSnakeCase,  // MY_CONSTANT
    RequireUnderscoreForPrivate = true,         // _private_member

    // Style
    MaxLineLength = 100,

    // Best practices
    WarnUnusedVariables = true,
    WarnUnusedParameters = true,
    WarnEmptyFunctions = true,
    SuggestTypeHints = false
};

var linter = new GDLinter(options);
```

### Presets

```csharp
// Default: Standard GDScript style guide
var linter = new GDLinter(GDLinterOptions.Default);

// Strict: All rules enabled including type hints
var linter = new GDLinter(GDLinterOptions.Strict);

// Minimal: Only critical rules
var linter = new GDLinter(GDLinterOptions.Minimal);
```

## Formatter

The `GDFormatter` class provides automatic code formatting with style extraction.

### Format Rules

| Rule ID | Name | Description |
|---------|------|-------------|
| GDF001 | indentation | Format indentation using tabs or spaces |
| GDF002 | blank-lines | Format blank lines between functions and members |
| GDF003 | spacing | Format spacing around operators, commas, colons |
| GDF004 | trailing-whitespace | Remove trailing whitespace, handle EOF newlines |
| GDF005 | newline | Normalize line endings |
| GDF006 | line-wrap | Automatic line wrapping for long lines |
| GDF007 | auto-type-hints | Automatically add inferred type hints (opt-in) |
| GDF008 | code-reorder | Reorder class members by type (opt-in) |

### Formatter Options

```csharp
var options = new GDFormatterOptions
{
    // Indentation
    IndentStyle = IndentStyle.Tabs,  // or IndentStyle.Spaces
    IndentSize = 4,                  // spaces per indent level

    // Line endings
    LineEnding = LineEndingStyle.LF,  // LF, CRLF, or Platform

    // Blank lines
    BlankLinesBetweenFunctions = 2,
    BlankLinesAfterClassDeclaration = 1,
    BlankLinesBetweenMemberTypes = 1,

    // Spacing
    SpaceAroundOperators = true,     // x = 10 + 5
    SpaceAfterComma = true,          // func(a, b, c)
    SpaceAfterColon = true,          // var x: int
    SpaceBeforeColon = false,        // var x: int (not x : int)
    SpaceInsideParentheses = false,  // (a, b) not ( a, b )
    SpaceInsideBrackets = false,     // [1, 2] not [ 1, 2 ]
    SpaceInsideBraces = true,        // { "a": 1 }

    // Trailing whitespace
    RemoveTrailingWhitespace = true,
    EnsureTrailingNewline = true,
    RemoveMultipleTrailingNewlines = true,

    // Line wrapping
    MaxLineLength = 100,             // 0 to disable
    WrapLongLines = true,
    LineWrapStyle = LineWrapStyle.AfterOpeningBracket,
    ContinuationIndentSize = 1
};

var formatter = new GDFormatter(options);
```

### Auto Type Inference (GDF007)

Automatically add type hints using type inference:

```csharp
var options = new GDFormatterOptions
{
    AutoAddTypeHints = true,           // Master switch
    AutoAddTypeHintsToClassVariables = true,
    AutoAddTypeHintsToLocals = true,
    AutoAddTypeHintsToParameters = false,
    UnknownTypeFallback = "Variant"    // Fallback for unknown types
};

var formatter = new GDFormatter(options);
var code = "var x = 10\nvar name = \"hello\"";
var result = formatter.FormatCode(code);
// Result: var x: int = 10
//         var name: String = "hello"
```

### Format by Example

Extract formatting style from sample code and apply to other code:

```csharp
var formatter = new GDFormatter();

// Sample code with 2-space indentation
var sampleCode = @"func sample():
  var x = 10
  print(x)
";

// Code to format (currently uses tabs)
var codeToFormat = @"func test():
	var y = 20
	print(y)
";

// Will format using extracted 2-space indentation style
var result = formatter.FormatCodeWithStyle(codeToFormat, sampleCode);
```

### Presets

```csharp
// Default: Standard formatting
var formatter = new GDFormatter(GDFormatterOptions.Default);

// GDScript style guide compliant
var formatter = new GDFormatter(GDFormatterOptions.GDScriptStyleGuide);

// Minimal: Only essential cleanup (trailing whitespace, EOF newline)
var formatter = new GDFormatter(GDFormatterOptions.Minimal);
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
- `GDDiagnosticCode` - Enum of all diagnostic codes (GD1001-GD7xxx)
- `GDDiagnosticSeverity` - Error, Warning, Hint
- `GDValidationOptions` - Configure which validators to run

### Runtime Provider Classes

- `IGDRuntimeProvider` - Interface for external type information
- `GDDefaultRuntimeProvider` - Built-in GDScript types and functions
- `GDCachingRuntimeProvider` - Caching wrapper for performance
- `GDTypeInferenceEngine` - Type inference for expressions
- `GDRuntimeTypeInfo` - Type information (name, base type, members)
- `GDRuntimeMemberInfo` - Member information (methods, properties, signals)
- `GDRuntimeFunctionInfo` - Global function information
- `GDRuntimeParameterInfo` - Parameter information (name, type, default)

### Linter Classes

- `GDLinter` - Main linter class
- `GDLintResult` - Collection of lint issues
- `GDLintIssue` - Single lint issue with location and message
- `GDLintRule` - Base class for lint rules
- `GDLintSeverity` - Error, Warning, Info, Hint
- `GDLinterOptions` - Configure naming conventions and rules
- `NamingCase` - SnakeCase, PascalCase, CamelCase, ScreamingSnakeCase

### Formatter Classes

- `GDFormatter` - Main formatter class
- `GDFormatRule` - Base class for format rules
- `GDFormatterOptions` - Configure formatting style
- `GDFormatterStyleExtractor` - Extract style from sample code
- `IndentStyle` - Tabs, Spaces
- `LineEndingStyle` - LF, CRLF, Platform
- `LineWrapStyle` - AfterOpeningBracket, BeforeElements

### Exception Classes

- `GDInvalidStateException` - Thrown when parser reaches invalid internal state
- `GDStackOverflowException` - Thrown when parsing depth limits are exceeded (configurable via `GDReadSettings`)

## Examples

For more examples, see the test projects:
- [GDShrapt.Reader.Tests](src/GDShrapt.Reader.Tests/) - Parsing examples
- [GDShrapt.Builder.Tests](src/GDShrapt.Builder.Tests/) - Code generation examples
- [GDShrapt.Validator.Tests](src/GDShrapt.Validator.Tests/) - Validation examples
- [GDShrapt.Linter.Tests](src/GDShrapt.Linter.Tests/) - Linter examples
- [GDShrapt.Formatter.Tests](src/GDShrapt.Formatter.Tests/) - Formatter examples
- [GDShrapt.Integration.Tests](src/GDShrapt.Integration.Tests/) - Cross-component examples

## Changelog

### 5.0.0

**Breaking Changes:**
- Split into multiple NuGet packages for modularity:
  - `GDShrapt.Reader` - Core parsing and AST
  - `GDShrapt.Builder` - Fluent code generation API
  - `GDShrapt.Validator` - Compiler-style AST validation
  - `GDShrapt.Linter` - Style checking
  - `GDShrapt.Formatter` - Code formatting

**New Package: GDShrapt.Validator**
- AST validation with compiler-style diagnostics (GD1xxx-GD7xxx)
- Syntax validation (GD1xxx): Invalid tokens, missing brackets, unexpected tokens
- Scope validation (GD2xxx): Undefined variables, duplicate declarations, forward references
- Type validation (GD3xxx): Type mismatches, invalid operand types
- Call validation (GD4xxx): Wrong argument counts for built-in functions
- Control flow validation (GD5xxx): break/continue outside loops, return outside functions
- Indentation validation (GD6xxx): Mixed tabs/spaces, inconsistent indentation
- Await validation (GD7xxx): Await expression structure issues
- Type inference system with `GDTypeInferenceEngine`
- `IGDRuntimeProvider` interface for custom type information (integrate with Godot)
- `GDDefaultRuntimeProvider` with built-in GDScript types and global functions
- `GDCachingRuntimeProvider` wrapper for performance optimization
- Two-pass validation with full forward reference support

**New Package: GDShrapt.Linter**
- Style guide enforcement with configurable naming conventions
- Naming rules: ClassNameCaseRule, FunctionNameCaseRule, VariableNameCaseRule, ConstantNameCaseRule
- Signal/enum naming: SignalNameCaseRule, EnumNameCaseRule, EnumValueCaseRule
- Private member prefix checking (PrivatePrefixRule)
- Best practices: UnusedVariableRule, UnusedParameterRule, UnusedSignalRule, EmptyFunctionRule
- Code quality: TypeHintRule, MaxParametersRule, MaxFunctionLengthRule
- Style rules: LineLengthRule, MemberOrderingRule
- Strict typing rule (GDL215): Per-element configurable severity for required type hints
- Comment-based rule suppression (gdtoolkit compatible): `gdlint:ignore`, `gdlint:disable/enable`
- Support for both rule IDs (GDL001) and names (variable-name) in suppressions
- Presets: Default, Strict, Minimal

**New Package: GDShrapt.Formatter**
- Rule-based code formatting (GDF001-GDF008)
- Indentation (GDF001): Tabs or spaces with configurable size
- Blank lines (GDF002): Between functions, after class declaration, between member types
- Spacing (GDF003): Around operators, after commas/colons, inside brackets
- Trailing whitespace (GDF004): Remove trailing spaces, ensure EOF newline
- Line endings (GDF005): Normalize to LF, CRLF, or Platform
- Line wrapping (GDF006): Automatic wrapping for long lines
- Auto type hints (GDF007): Automatically add inferred type hints using type inference
- Code reorder (GDF008): Reorder class members by type (opt-in)
- Style extraction from sample code ("format by example")
- LSP compatible options (tabSize, insertSpaces, trimTrailingWhitespace, etc.)
- Presets: Default, GDScriptStyleGuide, Minimal

**Parser Improvements:**
- Full GDScript 4.x support including typed dictionaries (Godot 4.4)
- Fixed property get/set parsing (Issues #10, #11)
- Custom `GDStackOverflowException` for controlled stack depth limits
- Configurable parsing limits via `GDReadSettings`

**New Features:**
- Helper classes: `GDAnnotationHelper`, `GDSpecialMethodHelper`, `GDExpressionHelper`
- Extended Building API: `GetUniqueNode`, `Enum`, `EnumValue`, Export annotations
- Comprehensive test coverage (1052 tests across all packages)

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
