# GDShrapt

GDShrapt is an object-oriented one-pass parser of GDScript 2.0. Now the main goal is a production-ready parser, lexical analyzer. 
The project written in C# and free to use. 
GDScript is the main language of [Godot Engine](https://github.com/godotengine/godot)

**The project is created on personal initiative and on enthusiasm.**

## GDShrapt.Reader

GDShrapt.Reader allows to build a lexical tree or generate a new code from scratch.

### How to install

Currently the latest **4.4.0-alpha version** from [Nuget](https://www.nuget.org/packages/GDShrapt.Reader).

Installation from Nuget console:
```
Install-Package GDShrapt.Reader -Version 4.4.0-alpha
```
## Capabilities, plan and what can be parsed

| Capability  | Is completed |
| ------------- | ------------- |
| One pass parsing | YES |
| Lexical tree structure | YES |
| Class declaration parsing | YES |
| Methods declaration parsing | YES |
| Enums parsing | YES |
| Atributes declaration parsing | YES |
| Variables declaration parsing | YES |
| Export block parsing | YES |
| Arrays parsing | YES |
| Dictionary parsing | YES |
| 'Match' statement parsing | YES |
| 'For' statement parsing | YES |
| 'While' statement parsing | YES |
| 'If-Elif-Else' statement parsing | YES |
| 'Yield' statement parsing | YES |
| Ternar 'If' expression parsing | YES |
| Signals parsing | YES |
| Strings parsing | YES |
| Numbers parsing | YES |
| Methods calls parsing | YES |
| Single operators parsing | YES |
| Dual operators parsing | YES |
| Static types parsing | YES |
| Expressions priority sorting | YES |
| Basic tokenization managment | YES |
| Save formatting while parsing | YES |
| Save comments while parsing | YES |
| Moving from tokens to parent | YES |
| NodePath and GetNode short syntax parsing | YES |
| Inner class parsing | YES |
| Tree walking and node visiting | YES |
| Syntax cloning | YES |
| Syntax factory | YES |
| Syntax errors managment and properly handling | IN PLAN |
| Code formatting | IN PLAN |
| Custom formatter | IN PLAN |
| Tree diff tool | IN PLAN |

## Last updates

#### 4.4.0-alpha
Added typed Dictionaries support (thanks to dougVanny).
Some QoL updates and bugfixes.

#### 4.3.2-alpha
Fixed line breaks inside brackets, enums, dictionaries, arrays. Fixed comments parsing inside brackets.

#### 4.3.1-alpha
Fixed ovewflows.
Fixed dictionary parsing if it contains invalid characters.
Fixed multiline string parsing if it starts with new line character.
Fixed some cases in expression and statements resolving.
Fixed yield expression.

## Reading samples

### Parse class

GDScript input:

```gdscript
tool
class_name HTerrainDataSaver
extends ResourceFormatSaver

const HTerrainData = preload("./ hterrain_data.gd")


func get_recognized_extensions(res):
	if res != null and res is HTerrainData:
		return PoolStringArray([HTerrainData.META_EXTENSION])
	return PoolStringArray()


func recognize(res):
	return res is HTerrainData


func save(path, resource, flags):
	resource.save_data(path.get_base_dir())
```

Parser usage:

```csharp
 // Initialize a reader instance
 var reader = new GDScriptReader();
 
 // Parse the raw code
 var @class = reader.ParseFileContent(code); // returns instance of type GDClassDeclaration 
 
 // Get 'extends' atribute information
 Console.WriteLine(@class.Extends.Type.Sequence); // outputs base class name "ResourceFormatSaver"
 
 // Get 'class_name' atribute information
 Console.WriteLine(@class.ClassName.Type.Sequence); // outputs current class name "HTerrainDataSaver"
 
 // Check 'tool' atribute 
 Console.WriteLine(@class.IsTool); // outputs true 
 
 // Enumerates all class variables
 foreach(GDVariableDeclaration variable in @class.Variables)
 {
    Console.WriteLine(method.Identifier.Sequence); // outputs variables's name
 }
 
 // Enumerates all class methods
 foreach(GDMethodDeclaration method in @class.Methods)
 {
    Console.WriteLine(method.Identifier.Sequence); // outputs method's name
    
    // Enumerates all method statements
    foreach(GDStatement st in method.Statements)
    {
        // ... your code
    }
 }
```


### Get comments from GDScript code

```csharp
 var @class = reader.ParseFileContent(code);
 
 // Add 'using System.Linq;'
 var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString()) // Convert token to string
                .ToArray();
```

## Tree building samples or GDScript runtime generation

GDShrapt supports many styles to simplify a code generation process. Just use the GD static class to create a token or a node. 

### Short style

```csharp
// Build a custom class. Safe code generation. Dont control a code style
var declaration = GD.Declaration.Class(
                GD.List.Atributes(
                    GD.Atribute.Tool(),
                    GD.Atribute.ClassName("Generated"),
                    GD.Atribute.Extends("Node2D")),

                GD.Declaration.Const("my_constant", GD.Expression.String("Hello World")),
                GD.Declaration.OnreadyVariable("parameter", GD.Expression.True()),

                GD.Declaration.Method("_start",
                    GD.Expression.Call(GD.Expression.Identifier("print"), GD.Expression.String("Hello world"))
                    )
                );

declaration.UpdateIntendation(); // Auto update tabs (recursively)

var code = declaration.ToString(); // Get the string representation
```

The result is code like:

```gdscript
tool
class_name Generated
extends Node2D

const my_constant = "Hello World"

onready var parameter = true

func _start():
	print("Hello world")
```

### Methods chain style

```csharp
// Build a custom class. Full tokens control, but unsafe for exceptions
var declaration = GD.Declaration.Class()
                .AddAtributes(x => x
                    .AddToolAtribute()
                    .AddNewLine()
                    .AddClassNameAtribute("Generated")
                    .AddNewLine()
                    .AddExtendsAtribute("Node2D"))
                .AddNewLine()
                .AddNewLine()
                .AddMembers(x => x
                    .AddVariable("a")
                    .AddNewLine()
                    .AddConst("message", GD.Expression.String("Hello"))
                    .AddNewLine()
                    .AddNewLine()
                    .AddMethod(x => x
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add("_start")
                        .AddOpenBracket()
                        .AddCloseBracket()
                        .AddStatements(x => x
                            .AddNewLine()
                            .AddNewLine()
                            .AddIntendation()
                            .AddCall(GD.Expression.Identifier("print"), GD.Expression.String("Hello world"))
                            .AddNewLine()
                            .AddNewLine()
                            .AddIntendation()
                            .AddPass())));

declaration.UpdateIntendation(); // Auto update tabs (recursively)

var code = declaration.ToString(); // Get the string representation
```

### Tokens list style

```csharp
// Build a custom class. Full tokens control but unsafe for types
var declaration = GD.Declaration.Class(
                GD.List.Atributes(
                    GD.Atribute.Tool(),
                    GD.Syntax.NewLine,
                    GD.Atribute.ClassName("Generated"),
                    GD.Syntax.NewLine,
                    GD.Atribute.Extends("Node2D")),

                GD.Syntax.NewLine,
                GD.Syntax.NewLine,

                GD.Declaration.Variable(
                     GD.Keyword.Const,
                     GD.Syntax.OneSpace,
                     GD.Syntax.Identifier("my_constant"),
                     GD.Syntax.OneSpace,
                     GD.Syntax.Assign,
                     GD.Syntax.OneSpace,
                     GD.Syntax.String("Hello World")),

                GD.Syntax.NewLine,
                GD.Syntax.NewLine,

                GD.Declaration.Variable(
                    GD.Keyword.Onready,
                    GD.Syntax.OneSpace,
                    GD.Keyword.Var,
                    GD.Syntax.OneSpace,
                    GD.Syntax.Identifier("parameter"),
                    GD.Syntax.OneSpace,
                    GD.Syntax.Assign,
                    GD.Syntax.OneSpace,
                    GD.Expression.True()),

                GD.Syntax.NewLine,
                GD.Syntax.NewLine,

                GD.Declaration.Method(
                    GD.Keyword.Func,
                    GD.Syntax.OneSpace,
                    GD.Syntax.Identifier("_start"),
                    GD.Syntax.OpenBracket,
                    GD.Syntax.CloseBracket,
                    GD.Syntax.Colon,

                    GD.Syntax.NewLine,
                    GD.Syntax.Intendation(1),
                    GD.Expression.Call(
                        GD.Expression.Identifier("print"),
                        GD.Syntax.OpenBracket,
                        GD.List.Expressions(GD.Expression.String("Hello world")),
                        GD.Syntax.CloseBracket)));

var code = declaration.ToString(); // Get the string representation
```

### Custom style initialization

```csharp
// The sample of a For statement initizalization with a predefined style. It is how the GD.Statement.For method works.
// You must know the 'form' to use this format. 
// For example a code line like "[1] = GD.Syntax.Space()" will insert a space token BEFORE the first static point in the nodes form.
// In the code below the first point of the For statement is the iterator's variable name.
public static GDForStatement For(GDIdentifier variable, GDExpression collection, GDExpression body) => new GDForStatement()
            {
                ForKeyword = new GDForKeyword(),
                [1] = GD.Syntax.Space(),
                Variable = variable,
                [2] = GD.Syntax.Space(),
                InKeyword = new GDInKeyword(),
                [3] = GD.Syntax.Space(),
                Collection = collection,
                Colon = new GDColon(),
                [5] = GD.Syntax.Space(),
                Expression = body
            };
```

You may use a combination of the styles.

### Calculating properties

```csharp

GDSyntaxToken token = null; // any token

token.StartLine // calculate the start line of the token in the code
token.EndLine // calculate the end line of the token in the code
token.Length // calculate the length of the token
token.StartColumn // calculate the start column in the line
token.EndColumn // calculate the end column in the line
token.NewLinesCount // calculate new line characters in the token. 

token.ClassMember // find the nearest class member from parents
token.MainClassDeclaration // find the main class contains the token
token.Parents // enumerate all parents of the token
```

For more samples see the [tests](src/GDShrapt.Reader.Tests/ParsingTests.cs).
