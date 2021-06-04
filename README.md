# GDShrapt

GDShrapt is object-oriented one-pass parser of GDScript. Also it allows to convert code to C#. 
The project written in C#, consists of two parts **GDShrapt.Reader** and **GDShrapt.Converter** and free to use. 
GDScript is the main language of [Godot Engine](https://github.com/godotengine/godot)

## GDShrapt.Reader

GDShrapt.Reader allows to build a lexical tree or generate a new code from scratch.

### How to install
Currently available prealpha version from [Nuget](https://www.nuget.org/packages/GDShrapt.Reader)

Installation from console:
```
Install-Package GDShrapt.Reader -Version 1.0.0-prealpha
```

### Samples

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

Tree building samples and runtime code generation are coming soon.
For more samples see the [tests](src/GDShrapt.Reader.Tests/ParsingTests.cs).

## GDShrapt.Converter
GDShrapt.Converter allows to convert lexical tree in same C# code. 
This project is at very initial stage.

### Samples
Not ready.

# Current state
The project is in pre-pre-alpha stage.

# Current goals
Prepare the project to pre-alpha stage and publish nuget.
