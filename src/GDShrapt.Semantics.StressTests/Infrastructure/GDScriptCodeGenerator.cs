namespace GDShrapt.Semantics.StressTests.Infrastructure;

/// <summary>
/// Generates syntactically valid GDScript code for stress testing.
/// Uses builder pattern for flexible code construction.
/// </summary>
public class GDScriptCodeGenerator
{
    private readonly StringBuilder _code = new();
    private int _indentLevel;

    /// <summary>
    /// Adds an extends clause.
    /// </summary>
    public GDScriptCodeGenerator WithExtends(string baseClass)
    {
        AppendIndented($"extends {baseClass}");
        _code.AppendLine();
        return this;
    }

    /// <summary>
    /// Adds a class_name declaration.
    /// </summary>
    public GDScriptCodeGenerator WithClassName(string name)
    {
        AppendIndented($"class_name {name}");
        _code.AppendLine();
        return this;
    }

    /// <summary>
    /// Adds a signal declaration.
    /// </summary>
    public GDScriptCodeGenerator WithSignal(string name, params string[] parameters)
    {
        var paramList = string.Join(", ", parameters);
        AppendIndented($"signal {name}({paramList})");
        _code.AppendLine();
        return this;
    }

    /// <summary>
    /// Adds a variable declaration.
    /// </summary>
    public GDScriptCodeGenerator WithVariable(string name, string? type = null, string? initializer = null)
    {
        var typePart = type != null ? $": {type}" : "";
        var initPart = initializer != null ? $" = {initializer}" : "";
        AppendIndented($"var {name}{typePart}{initPart}");
        _code.AppendLine();
        return this;
    }

    /// <summary>
    /// Adds a constant declaration.
    /// </summary>
    public GDScriptCodeGenerator WithConstant(string name, string? type, string value)
    {
        var typePart = type != null ? $": {type}" : "";
        AppendIndented($"const {name}{typePart} = {value}");
        _code.AppendLine();
        return this;
    }

    /// <summary>
    /// Adds a method declaration using a builder.
    /// </summary>
    public GDScriptCodeGenerator WithMethod(string name, Action<MethodBuilder> configure)
    {
        var builder = new MethodBuilder(name, _indentLevel);
        configure(builder);
        _code.Append(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds an inner class using a nested generator.
    /// </summary>
    public GDScriptCodeGenerator WithInnerClass(string name, Action<GDScriptCodeGenerator> configure)
    {
        _code.AppendLine();
        AppendIndented($"class {name}:");
        _code.AppendLine();
        _indentLevel++;
        configure(this);
        _indentLevel--;
        return this;
    }

    /// <summary>
    /// Adds a blank line.
    /// </summary>
    public GDScriptCodeGenerator WithBlankLine()
    {
        _code.AppendLine();
        return this;
    }

    /// <summary>
    /// Adds a comment.
    /// </summary>
    public GDScriptCodeGenerator WithComment(string comment)
    {
        AppendIndented($"# {comment}");
        _code.AppendLine();
        return this;
    }

    /// <summary>
    /// Builds the final GDScript code.
    /// </summary>
    public string Build() => _code.ToString();

    private void AppendIndented(string text)
    {
        _code.Append(new string('\t', _indentLevel));
        _code.Append(text);
    }

    #region Static Factory Methods

    /// <summary>
    /// Generates an entity-like class with common patterns.
    /// </summary>
    public static string GenerateEntityClass(int id, string baseClass = "Node")
    {
        return new GDScriptCodeGenerator()
            .WithExtends(baseClass)
            .WithClassName($"Entity{id}")
            .WithBlankLine()
            .WithSignal("health_changed", "new_health: int")
            .WithSignal("died")
            .WithBlankLine()
            .WithVariable("health", "int", "100")
            .WithVariable("max_health", "int", "100")
            .WithVariable("speed", "float", "10.0")
            .WithVariable("is_alive", "bool", "true")
            .WithBlankLine()
            .WithMethod("_ready", m => m
                .AddStatement("health = max_health")
                .AddStatement("is_alive = true"))
            .WithMethod("take_damage", m => m
                .WithParameter("amount", "int")
                .AddStatement("if not is_alive:")
                .AddStatement("\treturn")
                .AddStatement("health -= amount")
                .AddStatement("health_changed.emit(health)")
                .AddStatement("if health <= 0:")
                .AddStatement("\tdie()"))
            .WithMethod("heal", m => m
                .WithParameter("amount", "int")
                .AddStatement("if not is_alive:")
                .AddStatement("\treturn")
                .AddStatement("health = min(health + amount, max_health)")
                .AddStatement("health_changed.emit(health)"))
            .WithMethod("die", m => m
                .AddStatement("is_alive = false")
                .AddStatement("died.emit()"))
            .WithMethod("get_health_percentage", m => m
                .WithReturnType("float")
                .AddStatement("return float(health) / float(max_health)"))
            .Build();
    }

    /// <summary>
    /// Generates a class that is part of a deep inheritance chain.
    /// </summary>
    public static string GenerateDeepInheritanceClass(int level, int totalLevels)
    {
        var generator = new GDScriptCodeGenerator();

        if (level == 0)
            generator.WithExtends("Node");
        else
            generator.WithExtends($"Level{level - 1}");

        generator
            .WithClassName($"Level{level}")
            .WithBlankLine()
            .WithVariable($"level_{level}_var", "int", level.ToString())
            .WithVariable($"level_{level}_data", "String", $"\"{level}\"")
            .WithBlankLine()
            .WithMethod($"level_{level}_method", m => m
                .WithReturnType("int")
                .AddStatement($"return level_{level}_var"))
            .WithMethod($"get_level_{level}_info", m => m
                .WithReturnType("String")
                .AddStatement($"return \"Level {level}: \" + level_{level}_data"));

        // Add method that calls parent methods to exercise inheritance resolution
        if (level > 0)
        {
            generator.WithBlankLine();
            generator.WithMethod("call_all_parents", m =>
            {
                m.WithReturnType("int");
                m.AddStatement("var total = 0");
                for (int i = 0; i <= level; i++)
                {
                    m.AddStatement($"total += level_{i}_method()");
                }
                m.AddStatement("return total");
            });
        }

        // Add override pattern
        generator.WithMethod("get_level", m => m
            .WithReturnType("int")
            .AddStatement($"return {level}"));

        return generator.Build();
    }

    /// <summary>
    /// Generates a class with a symbol that has many references.
    /// </summary>
    public static string GenerateManyReferencesClass(string symbolName, int referenceCount)
    {
        var generator = new GDScriptCodeGenerator()
            .WithExtends("Node")
            .WithClassName("ManyReferences")
            .WithBlankLine()
            .WithVariable(symbolName, "int", "0")
            .WithBlankLine();

        // Create methods that reference the symbol many times
        int refsPerMethod = 50;
        int methodCount = (referenceCount + refsPerMethod - 1) / refsPerMethod;

        for (int m = 0; m < methodCount; m++)
        {
            generator.WithMethod($"use_symbol_{m}", method =>
            {
                int startRef = m * refsPerMethod;
                int endRef = Math.Min(startRef + refsPerMethod, referenceCount);
                for (int r = startRef; r < endRef; r++)
                {
                    if (r % 3 == 0)
                        method.AddStatement($"{symbolName} += 1");
                    else if (r % 3 == 1)
                        method.AddStatement($"var temp_{r} = {symbolName}");
                    else
                        method.AddStatement($"print({symbolName})");
                }
            });
        }

        // Add a method that reads the symbol
        generator.WithMethod("get_symbol_value", m => m
            .WithReturnType("int")
            .AddStatement($"return {symbolName}"));

        return generator.Build();
    }

    /// <summary>
    /// Generates a class with a very long method.
    /// </summary>
    public static string GenerateLongMethod(int lineCount)
    {
        var generator = new GDScriptCodeGenerator()
            .WithExtends("Node")
            .WithClassName("LongMethod")
            .WithBlankLine();

        generator.WithMethod("very_long_method", m =>
        {
            m.WithReturnType("int");
            m.AddStatement("var total: int = 0");

            // Generate realistic-looking computation
            for (int i = 1; i < lineCount - 1; i++)
            {
                switch (i % 7)
                {
                    case 0:
                        m.AddStatement($"var temp_{i}: int = {i} * 2");
                        break;
                    case 1:
                        m.AddStatement($"total += {i}");
                        break;
                    case 2:
                        m.AddStatement($"if total > {i * 10}:");
                        m.AddStatement($"\ttotal = total / 2");
                        break;
                    case 3:
                        m.AddStatement($"for j in range({i % 5}):");
                        m.AddStatement($"\ttotal += j");
                        break;
                    case 4:
                        m.AddStatement($"var arr_{i}: Array = [1, 2, 3, total]");
                        break;
                    case 5:
                        m.AddStatement($"var str_{i}: String = \"value_\" + str(total)");
                        break;
                    case 6:
                        m.AddStatement($"total = total % {Math.Max(1, i)}");
                        break;
                }
            }

            m.AddStatement("return total");
        });

        return generator.Build();
    }

    /// <summary>
    /// Generates a class with complex union types and variant handling.
    /// </summary>
    public static string GenerateComplexTypesScript(int variantCount)
    {
        var generator = new GDScriptCodeGenerator()
            .WithExtends("Node")
            .WithClassName("ComplexTypes")
            .WithBlankLine();

        // Generate Variant variables that get assigned different types
        for (int i = 0; i < variantCount; i++)
        {
            generator.WithVariable($"variant_{i}"); // No type = Variant
        }

        generator.WithBlankLine();

        // Generate methods that assign different types
        generator.WithMethod("assign_ints", m =>
        {
            for (int i = 0; i < variantCount; i++)
                m.AddStatement($"variant_{i} = {i}");
        });

        generator.WithMethod("assign_strings", m =>
        {
            for (int i = 0; i < variantCount; i++)
                m.AddStatement($"variant_{i} = \"str_{i}\"");
        });

        generator.WithMethod("assign_arrays", m =>
        {
            for (int i = 0; i < variantCount; i++)
                m.AddStatement($"variant_{i} = [{i}, {i + 1}]");
        });

        generator.WithMethod("assign_dicts", m =>
        {
            for (int i = 0; i < variantCount; i++)
                m.AddStatement($"variant_{i} = {{\"key\": {i}}}");
        });

        // Method that uses the variants with type guards
        generator.WithMethod("use_with_guards", m =>
        {
            m.WithReturnType("int");
            m.AddStatement("var count: int = 0");
            for (int i = 0; i < Math.Min(variantCount, 10); i++) // Limit to prevent huge methods
            {
                m.AddStatement($"if variant_{i} is int:");
                m.AddStatement($"\tcount += variant_{i}");
                m.AddStatement($"elif variant_{i} is String:");
                m.AddStatement($"\tcount += variant_{i}.length()");
            }
            m.AddStatement("return count");
        });

        return generator.Build();
    }

    /// <summary>
    /// Generates an entity class with cross-references to other entities.
    /// </summary>
    public static string GenerateEntityWithCrossReferences(int id, string baseClass, int existingEntityCount)
    {
        var generator = new GDScriptCodeGenerator()
            .WithExtends(baseClass)
            .WithClassName($"Entity{id}")
            .WithBlankLine();

        // Add references to other entities (type annotations)
        int refCount = Math.Min(5, existingEntityCount);
        for (int i = 0; i < refCount; i++)
        {
            int refId = i % existingEntityCount;
            generator.WithVariable($"ref_{i}", $"Entity{refId}", "null");
        }

        generator.WithBlankLine();

        // Add method that interacts with referenced entities
        generator.WithMethod("interact_with_others", m =>
        {
            for (int i = 0; i < refCount; i++)
            {
                m.AddStatement($"if ref_{i} != null:");
                m.AddStatement($"\tref_{i}.take_damage(10)");
            }
        });

        // Add method that checks types
        generator.WithMethod("count_alive_refs", m =>
        {
            m.WithReturnType("int");
            m.AddStatement("var count: int = 0");
            for (int i = 0; i < refCount; i++)
            {
                m.AddStatement($"if ref_{i} != null and ref_{i}.is_alive:");
                m.AddStatement("\tcount += 1");
            }
            m.AddStatement("return count");
        });

        return generator.Build();
    }

    #endregion
}

/// <summary>
/// Builder for GDScript method declarations.
/// </summary>
public class MethodBuilder
{
    private readonly string _name;
    private readonly int _baseIndent;
    private readonly List<(string Name, string? Type)> _parameters = new();
    private string? _returnType;
    private readonly List<string> _statements = new();

    public MethodBuilder(string name, int baseIndent)
    {
        _name = name;
        _baseIndent = baseIndent;
    }

    /// <summary>
    /// Adds a parameter to the method.
    /// </summary>
    public MethodBuilder WithParameter(string name, string? type = null)
    {
        _parameters.Add((name, type));
        return this;
    }

    /// <summary>
    /// Sets the return type of the method.
    /// </summary>
    public MethodBuilder WithReturnType(string type)
    {
        _returnType = type;
        return this;
    }

    /// <summary>
    /// Adds a statement to the method body.
    /// </summary>
    public MethodBuilder AddStatement(string statement)
    {
        _statements.Add(statement);
        return this;
    }

    /// <summary>
    /// Builds the method as a string.
    /// </summary>
    public string Build()
    {
        var sb = new StringBuilder();
        var indent = new string('\t', _baseIndent);

        var paramList = string.Join(", ", _parameters.Select(p =>
            p.Type != null ? $"{p.Name}: {p.Type}" : p.Name));
        var returnPart = _returnType != null ? $" -> {_returnType}" : "";

        sb.AppendLine($"{indent}func {_name}({paramList}){returnPart}:");

        if (_statements.Count == 0)
        {
            sb.AppendLine($"{indent}\tpass");
        }
        else
        {
            foreach (var stmt in _statements)
            {
                sb.AppendLine($"{indent}\t{stmt}");
            }
        }

        sb.AppendLine();
        return sb.ToString();
    }
}
