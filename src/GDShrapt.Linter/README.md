# GDShrapt.Linter

Configurable style checking for GDScript 2.0 (Godot 4.x) with gdtoolkit-compatible suppression.

## Features

- **Naming conventions** - PascalCase, snake_case, SCREAMING_SNAKE_CASE
- **Best practices** - Unused variables, empty functions, type hints
- **Comment suppression** - gdtoolkit compatible `gdlint:ignore`, `gdlint:disable/enable`
- **Strict typing** - Per-element configurable severity for type hints
- **Presets** - Default, Strict, Minimal configurations

## Quick Start

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

## Linter Options

```csharp
var options = new GDLinterOptions
{
    // Naming conventions
    ClassNameCase = NamingCase.PascalCase,
    FunctionNameCase = NamingCase.SnakeCase,
    VariableNameCase = NamingCase.SnakeCase,
    ConstantNameCase = NamingCase.ScreamingSnakeCase,
    RequireUnderscoreForPrivate = true,

    // Style
    MaxLineLength = 100,

    // Best practices
    WarnUnusedVariables = true,
    WarnEmptyFunctions = true
};

var linter = new GDLinter(options);
```

## Strict Typing (GDL215)

Require type hints with configurable severity per element:

```csharp
var options = new GDLinterOptions();
options.StrictTypingClassVariables = GDLintSeverity.Warning;
options.StrictTypingParameters = GDLintSeverity.Error;
options.StrictTypingReturnTypes = GDLintSeverity.Error;

var linter = new GDLinter(options);
```

## Comment Suppression

Suppress warnings using gdtoolkit-compatible comments:

```gdscript
# gdlint:ignore = variable-name
var my_Var = 10  # No warning

# gdlint: disable=function-name
func BadName():
    pass
# gdlint: enable=function-name
```

## Presets

```csharp
var linter = new GDLinter(GDLinterOptions.Default);  // Standard rules
var linter = new GDLinter(GDLinterOptions.Strict);   // All rules enabled
var linter = new GDLinter(GDLinterOptions.Minimal);  // Critical rules only
```

## Related Packages

| Package | Description |
|---------|-------------|
| [GDShrapt.Reader](https://www.nuget.org/packages/GDShrapt.Reader) | Core parser and AST (required) |
| [GDShrapt.Builder](https://www.nuget.org/packages/GDShrapt.Builder) | Fluent API for code generation |
| [GDShrapt.Validator](https://www.nuget.org/packages/GDShrapt.Validator) | Compiler-style AST validation |
| [GDShrapt.Formatter](https://www.nuget.org/packages/GDShrapt.Formatter) | Code formatting with type inference |

## Documentation

Full documentation and examples: [GitHub Repository](https://github.com/elamaunt/GDShrapt)

## License

MIT License - see [LICENSE](https://github.com/elamaunt/GDShrapt/blob/main/LICENSE) for details.
