using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the list command and its subcommands.
/// </summary>
public static class ListCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("list", "List project-wide entities (classes, signals, methods, etc.).\n\nExamples:\n  gdshrapt list classes                    List all classes\n  gdshrapt list classes --implements Node2D  Find Node2D subclasses\n  gdshrapt list signals --connected        List connected signals\n  gdshrapt list methods --static           List static methods");

        // Shared options
        var nameOption = new Option<string?>("--name", "Filter by name (glob pattern, e.g. Player*)");
        var regexOption = new Option<string?>("--regex", "Filter by name (regex pattern)");
        var fileOption = new Option<string?>("--file", "Filter by file path (substring match)");
        var dirOption = new Option<string?>("--dir", "Filter by directory prefix");
        var globOption = new Option<string?>("--glob", "Filter files by glob pattern");
        var topOption = new Option<int?>("--top", "Limit output to first N items");
        var sortOption = new Option<string?>("--sort", "Sort by: name (default), file");
        var excludeOption = new Option<string[]>("--exclude", "Glob patterns to exclude files") { AllowMultipleArgumentsPerToken = true };

        command.AddCommand(BuildClassesCommand(globalFormatOption, verboseOption, debugOption, quietOption, logLevelOption, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption));
        command.AddCommand(BuildSignalsCommand(globalFormatOption, verboseOption, debugOption, quietOption, logLevelOption, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption));
        command.AddCommand(BuildAutoloadsCommand(globalFormatOption, verboseOption, debugOption, quietOption, logLevelOption, nameOption, regexOption, topOption, sortOption, excludeOption));
        command.AddCommand(BuildEngineCallbacksCommand(globalFormatOption, verboseOption, debugOption, quietOption, logLevelOption, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption));
        command.AddCommand(BuildMethodsCommand(globalFormatOption, verboseOption, debugOption, quietOption, logLevelOption, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption));
        command.AddCommand(BuildVariablesCommand(globalFormatOption, verboseOption, debugOption, quietOption, logLevelOption, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption));
        command.AddCommand(BuildExportsCommand(globalFormatOption, verboseOption, debugOption, quietOption, logLevelOption, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption));
        command.AddCommand(BuildNodesCommand(globalFormatOption, verboseOption, debugOption, quietOption, logLevelOption, nameOption, regexOption, topOption, sortOption));
        command.AddCommand(BuildScenesCommand(globalFormatOption, verboseOption, debugOption, quietOption, logLevelOption, nameOption, regexOption, topOption, sortOption, excludeOption));
        command.AddCommand(BuildResourcesCommand(globalFormatOption, verboseOption, debugOption, quietOption, logLevelOption, nameOption, regexOption, topOption, sortOption, excludeOption));
        command.AddCommand(BuildEnumsCommand(globalFormatOption, verboseOption, debugOption, quietOption, logLevelOption, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption));

        return command;
    }

    private static Command BuildClassesCommand(
        Option<string> formatOption, Option<bool> verboseOption, Option<bool> debugOption,
        Option<bool> quietOption, Option<string?> logLevelOption,
        Option<string?> nameOption, Option<string?> regexOption,
        Option<string?> fileOption, Option<string?> dirOption,
        Option<int?> topOption, Option<string?> sortOption, Option<string[]> excludeOption)
    {
        var cmd = new Command("classes", "List all classes in the project");
        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var abstractOption = new Option<bool>("--abstract", "Show only abstract classes");
        var extendsOption = new Option<string?>("--extends", "Filter by direct base type");
        var implementsOption = new Option<string?>("--implements", "Filter by type hierarchy (finds all subclasses)");
        var innerOption = new Option<bool>("--inner", "Show only inner/nested classes");
        var topLevelOption = new Option<bool>("--top-level", "Show only top-level classes");

        cmd.AddArgument(pathArg);
        AddSharedOptions(cmd, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);
        cmd.AddOption(abstractOption);
        cmd.AddOption(extendsOption);
        cmd.AddOption(implementsOption);
        cmd.AddOption(innerOption);
        cmd.AddOption(topLevelOption);

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var (projectPath, format, logger, name, regex, file, dir, top, sort, exclude) =
                ParseShared(context, pathArg, formatOption, verboseOption, debugOption, quietOption, logLevelOption,
                    nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);

            var command = new GDListCommand(projectPath, CommandHelpers.GetFormatter(format), GDListItemKind.Class,
                logger: logger, cliExcludePatterns: exclude,
                nameGlob: name, regexPattern: regex, fileFilter: file, dirFilter: dir, top: top, sortBy: sort,
                abstractOnly: context.ParseResult.GetValueForOption(abstractOption),
                extendsType: context.ParseResult.GetValueForOption(extendsOption),
                implementsType: context.ParseResult.GetValueForOption(implementsOption),
                innerOnly: context.ParseResult.GetValueForOption(innerOption),
                topLevelOnly: context.ParseResult.GetValueForOption(topLevelOption));
            Environment.ExitCode = await command.ExecuteAsync();
        });

        return cmd;
    }

    private static Command BuildSignalsCommand(
        Option<string> formatOption, Option<bool> verboseOption, Option<bool> debugOption,
        Option<bool> quietOption, Option<string?> logLevelOption,
        Option<string?> nameOption, Option<string?> regexOption,
        Option<string?> fileOption, Option<string?> dirOption,
        Option<int?> topOption, Option<string?> sortOption, Option<string[]> excludeOption)
    {
        var cmd = new Command("signals", "List all signals in the project");
        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var sceneOption = new Option<string?>("--scene", "Filter by scene path");
        var connectedOption = new Option<bool>("--connected", "Show only connected signals");
        var unconnectedOption = new Option<bool>("--unconnected", "Show only unconnected signals");

        cmd.AddArgument(pathArg);
        AddSharedOptions(cmd, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);
        cmd.AddOption(sceneOption);
        cmd.AddOption(connectedOption);
        cmd.AddOption(unconnectedOption);

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var (projectPath, format, logger, name, regex, file, dir, top, sort, exclude) =
                ParseShared(context, pathArg, formatOption, verboseOption, debugOption, quietOption, logLevelOption,
                    nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);

            var command = new GDListCommand(projectPath, CommandHelpers.GetFormatter(format), GDListItemKind.Signal,
                logger: logger, cliExcludePatterns: exclude,
                nameGlob: name, regexPattern: regex, fileFilter: file, dirFilter: dir, top: top, sortBy: sort,
                scenePath: context.ParseResult.GetValueForOption(sceneOption),
                connectedOnly: context.ParseResult.GetValueForOption(connectedOption),
                unconnectedOnly: context.ParseResult.GetValueForOption(unconnectedOption));
            Environment.ExitCode = await command.ExecuteAsync();
        });

        return cmd;
    }

    private static Command BuildAutoloadsCommand(
        Option<string> formatOption, Option<bool> verboseOption, Option<bool> debugOption,
        Option<bool> quietOption, Option<string?> logLevelOption,
        Option<string?> nameOption, Option<string?> regexOption,
        Option<int?> topOption, Option<string?> sortOption, Option<string[]> excludeOption)
    {
        var cmd = new Command("autoloads", "List all autoloads in the project");
        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        cmd.AddArgument(pathArg);
        cmd.AddOption(nameOption);
        cmd.AddOption(regexOption);
        cmd.AddOption(topOption);
        cmd.AddOption(sortOption);
        cmd.AddOption(excludeOption);

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var (projectPath, format, logger, name, regex, _, _, top, sort, exclude) =
                ParseShared(context, pathArg, formatOption, verboseOption, debugOption, quietOption, logLevelOption,
                    nameOption, regexOption, null, null, topOption, sortOption, excludeOption);

            var command = new GDListCommand(projectPath, CommandHelpers.GetFormatter(format), GDListItemKind.Autoload,
                logger: logger, cliExcludePatterns: exclude,
                nameGlob: name, regexPattern: regex, top: top, sortBy: sort);
            Environment.ExitCode = await command.ExecuteAsync();
        });

        return cmd;
    }

    private static Command BuildEngineCallbacksCommand(
        Option<string> formatOption, Option<bool> verboseOption, Option<bool> debugOption,
        Option<bool> quietOption, Option<string?> logLevelOption,
        Option<string?> nameOption, Option<string?> regexOption,
        Option<string?> fileOption, Option<string?> dirOption,
        Option<int?> topOption, Option<string?> sortOption, Option<string[]> excludeOption)
    {
        var cmd = new Command("engine-callbacks", "List all engine callback methods (_ready, _process, etc.)");
        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        cmd.AddArgument(pathArg);
        AddSharedOptions(cmd, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var (projectPath, format, logger, name, regex, file, dir, top, sort, exclude) =
                ParseShared(context, pathArg, formatOption, verboseOption, debugOption, quietOption, logLevelOption,
                    nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);

            var command = new GDListCommand(projectPath, CommandHelpers.GetFormatter(format), GDListItemKind.EngineCallback,
                logger: logger, cliExcludePatterns: exclude,
                nameGlob: name, regexPattern: regex, fileFilter: file, dirFilter: dir, top: top, sortBy: sort);
            Environment.ExitCode = await command.ExecuteAsync();
        });

        return cmd;
    }

    private static Command BuildMethodsCommand(
        Option<string> formatOption, Option<bool> verboseOption, Option<bool> debugOption,
        Option<bool> quietOption, Option<string?> logLevelOption,
        Option<string?> nameOption, Option<string?> regexOption,
        Option<string?> fileOption, Option<string?> dirOption,
        Option<int?> topOption, Option<string?> sortOption, Option<string[]> excludeOption)
    {
        var cmd = new Command("methods", "List all methods in the project");
        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var staticOption = new Option<bool>("--static", "Show only static methods");
        var virtualOption = new Option<bool>("--virtual", "Show only virtual methods (underscore prefix)");
        var visibilityOption = new Option<string?>("--visibility", "Filter by visibility: public or private");

        cmd.AddArgument(pathArg);
        AddSharedOptions(cmd, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);
        cmd.AddOption(staticOption);
        cmd.AddOption(virtualOption);
        cmd.AddOption(visibilityOption);

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var (projectPath, format, logger, name, regex, file, dir, top, sort, exclude) =
                ParseShared(context, pathArg, formatOption, verboseOption, debugOption, quietOption, logLevelOption,
                    nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);

            var command = new GDListCommand(projectPath, CommandHelpers.GetFormatter(format), GDListItemKind.Method,
                logger: logger, cliExcludePatterns: exclude,
                nameGlob: name, regexPattern: regex, fileFilter: file, dirFilter: dir, top: top, sortBy: sort,
                staticOnly: context.ParseResult.GetValueForOption(staticOption),
                virtualOnly: context.ParseResult.GetValueForOption(virtualOption),
                visibility: context.ParseResult.GetValueForOption(visibilityOption));
            Environment.ExitCode = await command.ExecuteAsync();
        });

        return cmd;
    }

    private static Command BuildVariablesCommand(
        Option<string> formatOption, Option<bool> verboseOption, Option<bool> debugOption,
        Option<bool> quietOption, Option<string?> logLevelOption,
        Option<string?> nameOption, Option<string?> regexOption,
        Option<string?> fileOption, Option<string?> dirOption,
        Option<int?> topOption, Option<string?> sortOption, Option<string[]> excludeOption)
    {
        var cmd = new Command("variables", "List all class-level variables in the project");
        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var constOption = new Option<bool>("--const", "Show only constants");
        var staticOption = new Option<bool>("--static", "Show only static variables");
        var visibilityOption = new Option<string?>("--visibility", "Filter by visibility: public or private");

        cmd.AddArgument(pathArg);
        AddSharedOptions(cmd, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);
        cmd.AddOption(constOption);
        cmd.AddOption(staticOption);
        cmd.AddOption(visibilityOption);

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var (projectPath, format, logger, name, regex, file, dir, top, sort, exclude) =
                ParseShared(context, pathArg, formatOption, verboseOption, debugOption, quietOption, logLevelOption,
                    nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);

            var command = new GDListCommand(projectPath, CommandHelpers.GetFormatter(format), GDListItemKind.Variable,
                logger: logger, cliExcludePatterns: exclude,
                nameGlob: name, regexPattern: regex, fileFilter: file, dirFilter: dir, top: top, sortBy: sort,
                constOnly: context.ParseResult.GetValueForOption(constOption),
                staticOnly: context.ParseResult.GetValueForOption(staticOption),
                visibility: context.ParseResult.GetValueForOption(visibilityOption));
            Environment.ExitCode = await command.ExecuteAsync();
        });

        return cmd;
    }

    private static Command BuildExportsCommand(
        Option<string> formatOption, Option<bool> verboseOption, Option<bool> debugOption,
        Option<bool> quietOption, Option<string?> logLevelOption,
        Option<string?> nameOption, Option<string?> regexOption,
        Option<string?> fileOption, Option<string?> dirOption,
        Option<int?> topOption, Option<string?> sortOption, Option<string[]> excludeOption)
    {
        var cmd = new Command("exports", "List all @export variables in the project");
        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var typeOption = new Option<string?>("--type", "Filter by type (e.g. int, String, PackedScene)");

        cmd.AddArgument(pathArg);
        AddSharedOptions(cmd, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);
        cmd.AddOption(typeOption);

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var (projectPath, format, logger, name, regex, file, dir, top, sort, exclude) =
                ParseShared(context, pathArg, formatOption, verboseOption, debugOption, quietOption, logLevelOption,
                    nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);

            var command = new GDListCommand(projectPath, CommandHelpers.GetFormatter(format), GDListItemKind.Export,
                logger: logger, cliExcludePatterns: exclude,
                nameGlob: name, regexPattern: regex, fileFilter: file, dirFilter: dir, top: top, sortBy: sort,
                typeFilter: context.ParseResult.GetValueForOption(typeOption));
            Environment.ExitCode = await command.ExecuteAsync();
        });

        return cmd;
    }

    private static Command BuildNodesCommand(
        Option<string> formatOption, Option<bool> verboseOption, Option<bool> debugOption,
        Option<bool> quietOption, Option<string?> logLevelOption,
        Option<string?> nameOption, Option<string?> regexOption,
        Option<int?> topOption, Option<string?> sortOption)
    {
        var cmd = new Command("nodes", "List nodes in a scene");
        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var sceneOption = new Option<string>("--scene", "Scene path (required, e.g. res://main.tscn)") { IsRequired = true };
        var typeOption = new Option<string?>("--type", "Filter by node type");

        cmd.AddArgument(pathArg);
        cmd.AddOption(nameOption);
        cmd.AddOption(regexOption);
        cmd.AddOption(topOption);
        cmd.AddOption(sortOption);
        cmd.AddOption(sceneOption);
        cmd.AddOption(typeOption);

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var (projectPath, format, logger, name, regex, _, _, top, sort, _) =
                ParseShared(context, pathArg, formatOption, verboseOption, debugOption, quietOption, logLevelOption,
                    nameOption, regexOption, null, null, topOption, sortOption, null);

            var command = new GDListCommand(projectPath, CommandHelpers.GetFormatter(format), GDListItemKind.Node,
                logger: logger,
                nameGlob: name, regexPattern: regex, top: top, sortBy: sort,
                scenePath: context.ParseResult.GetValueForOption(sceneOption),
                typeFilter: context.ParseResult.GetValueForOption(typeOption));
            Environment.ExitCode = await command.ExecuteAsync();
        });

        return cmd;
    }

    private static Command BuildScenesCommand(
        Option<string> formatOption, Option<bool> verboseOption, Option<bool> debugOption,
        Option<bool> quietOption, Option<string?> logLevelOption,
        Option<string?> nameOption, Option<string?> regexOption,
        Option<int?> topOption, Option<string?> sortOption, Option<string[]> excludeOption)
    {
        var cmd = new Command("scenes", "List all scenes in the project");
        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        cmd.AddArgument(pathArg);
        cmd.AddOption(nameOption);
        cmd.AddOption(regexOption);
        cmd.AddOption(topOption);
        cmd.AddOption(sortOption);
        cmd.AddOption(excludeOption);

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var (projectPath, format, logger, name, regex, _, _, top, sort, exclude) =
                ParseShared(context, pathArg, formatOption, verboseOption, debugOption, quietOption, logLevelOption,
                    nameOption, regexOption, null, null, topOption, sortOption, excludeOption);

            var command = new GDListCommand(projectPath, CommandHelpers.GetFormatter(format), GDListItemKind.Scene,
                logger: logger, cliExcludePatterns: exclude,
                nameGlob: name, regexPattern: regex, top: top, sortBy: sort);
            Environment.ExitCode = await command.ExecuteAsync();
        });

        return cmd;
    }

    private static Command BuildResourcesCommand(
        Option<string> formatOption, Option<bool> verboseOption, Option<bool> debugOption,
        Option<bool> quietOption, Option<string?> logLevelOption,
        Option<string?> nameOption, Option<string?> regexOption,
        Option<int?> topOption, Option<string?> sortOption, Option<string[]> excludeOption)
    {
        var cmd = new Command("resources", "List resources in the project");
        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var unusedOption = new Option<bool>("--unused", "Show only unused resources");
        var missingOption = new Option<bool>("--missing", "Show only missing resources");
        var categoryOption = new Option<string?>("--category", "Filter by resource category");

        cmd.AddArgument(pathArg);
        cmd.AddOption(nameOption);
        cmd.AddOption(regexOption);
        cmd.AddOption(topOption);
        cmd.AddOption(sortOption);
        cmd.AddOption(excludeOption);
        cmd.AddOption(unusedOption);
        cmd.AddOption(missingOption);
        cmd.AddOption(categoryOption);

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var (projectPath, format, logger, name, regex, _, _, top, sort, exclude) =
                ParseShared(context, pathArg, formatOption, verboseOption, debugOption, quietOption, logLevelOption,
                    nameOption, regexOption, null, null, topOption, sortOption, excludeOption);

            var command = new GDListCommand(projectPath, CommandHelpers.GetFormatter(format), GDListItemKind.Resource,
                logger: logger, cliExcludePatterns: exclude,
                nameGlob: name, regexPattern: regex, top: top, sortBy: sort,
                unusedOnly: context.ParseResult.GetValueForOption(unusedOption),
                missingOnly: context.ParseResult.GetValueForOption(missingOption),
                category: context.ParseResult.GetValueForOption(categoryOption));
            Environment.ExitCode = await command.ExecuteAsync();
        });

        return cmd;
    }

    private static Command BuildEnumsCommand(
        Option<string> formatOption, Option<bool> verboseOption, Option<bool> debugOption,
        Option<bool> quietOption, Option<string?> logLevelOption,
        Option<string?> nameOption, Option<string?> regexOption,
        Option<string?> fileOption, Option<string?> dirOption,
        Option<int?> topOption, Option<string?> sortOption, Option<string[]> excludeOption)
    {
        var cmd = new Command("enums", "List all enums in the project");
        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        cmd.AddArgument(pathArg);
        AddSharedOptions(cmd, nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var (projectPath, format, logger, name, regex, file, dir, top, sort, exclude) =
                ParseShared(context, pathArg, formatOption, verboseOption, debugOption, quietOption, logLevelOption,
                    nameOption, regexOption, fileOption, dirOption, topOption, sortOption, excludeOption);

            var command = new GDListCommand(projectPath, CommandHelpers.GetFormatter(format), GDListItemKind.Enum,
                logger: logger, cliExcludePatterns: exclude,
                nameGlob: name, regexPattern: regex, fileFilter: file, dirFilter: dir, top: top, sortBy: sort);
            Environment.ExitCode = await command.ExecuteAsync();
        });

        return cmd;
    }

    private static void AddSharedOptions(Command cmd,
        Option<string?> nameOption, Option<string?> regexOption,
        Option<string?> fileOption, Option<string?> dirOption,
        Option<int?> topOption, Option<string?> sortOption, Option<string[]> excludeOption)
    {
        cmd.AddOption(nameOption);
        cmd.AddOption(regexOption);
        cmd.AddOption(fileOption);
        cmd.AddOption(dirOption);
        cmd.AddOption(topOption);
        cmd.AddOption(sortOption);
        cmd.AddOption(excludeOption);
    }

    private static (string projectPath, string format, GDCliLogger logger,
        string? name, string? regex, string? file, string? dir,
        int? top, GDListSortBy sort, string[]? exclude)
        ParseShared(
            InvocationContext context,
            Argument<string> pathArg,
            Option<string> formatOption,
            Option<bool> verboseOption,
            Option<bool> debugOption,
            Option<bool> quietOption,
            Option<string?> logLevelOption,
            Option<string?> nameOption,
            Option<string?> regexOption,
            Option<string?>? fileOption,
            Option<string?>? dirOption,
            Option<int?> topOption,
            Option<string?> sortOption,
            Option<string[]>? excludeOption)
    {
        var projectPath = context.ParseResult.GetValueForArgument(pathArg) ?? ".";
        var format = context.ParseResult.GetValueForOption(formatOption) ?? "text";
        var verbose = context.ParseResult.GetValueForOption(verboseOption);
        var debug = context.ParseResult.GetValueForOption(debugOption);
        var quiet = context.ParseResult.GetValueForOption(quietOption);
        var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
        var logger = GDCliLogger.FromFlags(quiet, verbose, debug, logLevel);

        var name = context.ParseResult.GetValueForOption(nameOption);
        var regex = context.ParseResult.GetValueForOption(regexOption);
        var file = fileOption != null ? context.ParseResult.GetValueForOption(fileOption) : null;
        var dir = dirOption != null ? context.ParseResult.GetValueForOption(dirOption) : null;
        var top = context.ParseResult.GetValueForOption(topOption);
        var sortStr = context.ParseResult.GetValueForOption(sortOption);
        var sort = sortStr?.ToLowerInvariant() == "file" ? GDListSortBy.File : GDListSortBy.Name;
        var exclude = excludeOption != null ? context.ParseResult.GetValueForOption(excludeOption) : null;

        return (projectPath, format, logger, name, regex, file, dir, top, sort, exclude);
    }
}
