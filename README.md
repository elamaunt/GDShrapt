# GDShrapt

GDShrapt is object-oriented one-pass parser of GDScript. Now the main goal is production-ready parser, lexical analyzer and GDScript-to-C# converter. 
The project written in C#, consists of two parts **GDShrapt.Reader** and **GDShrapt.Converter** and free to use. 
GDScript is the main language of [Godot Engine](https://github.com/godotengine/godot)

**The project is created on personal initiative and on enthusiasm.**

## GDShrapt.Reader

GDShrapt.Reader allows to build a lexical tree or generate a new code from scratch.

### How to install

Currently the latest **2.1.0-alpha version** from [Nuget](https://www.nuget.org/packages/GDShrapt.Reader).

Installation from Nuget console:
```
Install-Package GDShrapt.Reader -Version 2.1.0-alpha
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
| Inner class parsing | NOT TESTED |
| Asserts handling | IN PLAN |
| RPC annotations parsing | IN PLAN |
| Syntax errors managment and properly handling | IN PLAN |
| Tree walking and node visiting | IN PLAN |
| Syntax cloning | YES |
| Syntax factory | IN PLAN |
| Tree diff tool | IN PLAN |

## Version history

#### 2.1.0-alpha
Implemented 'clone' methods for every token type.
Changed the project type to Net.Standart 2.0 (Because this version is supported by Godot).
Small fix with 'ToString' method of GDIndexerExpression.

#### 2.0.0-alpha
The project is now in Net.Standart 2.1 and was 
totally reworked with tokenization layer. The parser now performs token extraction and lexical tree construction at the same time.
No style data loss. Possibility to manage every token in code.
Implemented specific node parsing like NodePath, short form of 'get_node'.
Properly handling of comments and spaces.

#### 1.0.0-prealpha
.NET 5.0 version
Implemented all basic nodes and a lexical tree building with a style data loss.
Has limitations in specific situations.

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

Tree building samples and runtime code generation are coming soon.
For more samples see the [tests](src/GDShrapt.Reader.Tests/ParsingTests.cs).


## GDShrapt.Converter

GDShrapt.Converter allows to convert lexical tree in same C# code. 
This project is at very initial stage.

### Conversion samples

Not ready.
