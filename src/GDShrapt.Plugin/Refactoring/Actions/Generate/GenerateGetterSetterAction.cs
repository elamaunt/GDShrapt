using GDShrapt.Plugin.Refactoring.UI;
using GDShrapt.Reader;
using Godot;
using System.Text;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Refactoring.Actions.Generate;

/// <summary>
/// Generates getter and setter for a class variable.
/// Supports both GDScript 4.x property syntax and traditional method syntax.
/// </summary>
internal class GenerateGetterSetterAction : IRefactoringAction
{
    public string Id => "generate_getter_setter";
    public string DisplayName => "Generate Getter/Setter";
    public RefactoringCategory Category => RefactoringCategory.Generate;
    public string Shortcut => "Ctrl+Alt+G";
    public int Priority => 10;

    public bool IsAvailable(RefactoringContext context)
    {
        if (context?.ContainingClass == null)
            return false;

        // Available when cursor is on a class-level variable declaration
        if (!context.IsOnClassVariable)
            return false;

        var varDecl = context.GetVariableDeclaration();
        if (varDecl == null)
            return false;

        // Don't offer for constants
        if (varDecl.ConstKeyword != null)
            return false;

        // Don't offer if already has getters/setters
        if (varDecl.FirstAccessorDeclarationNode != null || varDecl.SecondAccessorDeclarationNode != null)
            return false;

        return true;
    }

    public async Task ExecuteAsync(RefactoringContext context)
    {
        Logger.Info("GenerateGetterSetterAction: Starting execution");

        var editor = context.Editor;
        var varDecl = context.GetVariableDeclaration();

        if (varDecl == null)
        {
            Logger.Info("GenerateGetterSetterAction: No variable declaration found");
            return;
        }

        var varName = varDecl.Identifier?.ToString();
        var varType = varDecl.Type?.ToString();
        var hasInitializer = varDecl.Initializer != null;
        var initializerText = varDecl.Initializer?.ToString();

        Logger.Info($"GenerateGetterSetterAction: Variable '{varName}' type '{varType}'");

        // Show options dialog
        var dialog = new GetterSetterOptionsDialog();
        context.DialogParent?.AddChild(dialog);

        var options = await dialog.ShowForResult(varName);
        dialog.QueueFree();

        if (options == null)
        {
            Logger.Info("GenerateGetterSetterAction: Cancelled by user");
            return;
        }

        // Generate the code
        string generatedCode;
        if (options.UsePropertySyntax)
        {
            generatedCode = GeneratePropertySyntax(varName, varType, initializerText, options);
        }
        else
        {
            generatedCode = GenerateMethodSyntax(varName, varType, options);
        }

        // Get position to replace the variable declaration
        var startLine = varDecl.StartLine;
        var startColumn = varDecl.StartColumn;
        var endLine = varDecl.EndLine;
        var endColumn = varDecl.EndColumn;

        Logger.Info($"GenerateGetterSetterAction: Replacing at ({startLine}:{startColumn}) - ({endLine}:{endColumn})");

        // Replace the variable declaration
        editor.Select(startLine, startColumn, endLine, endColumn);
        editor.Cut();
        editor.InsertTextAtCursor(generatedCode);

        editor.ReloadScriptFromText();
        Logger.Info("GenerateGetterSetterAction: Completed successfully");
    }

    private string GeneratePropertySyntax(string varName, string varType, string initializer, GetterSetterOptions options)
    {
        var sb = new StringBuilder();
        var backingField = options.UseBackingField ? $"_{varName}" : null;

        // Generate backing field if needed
        if (options.UseBackingField)
        {
            sb.Append($"var {backingField}");
            if (!string.IsNullOrEmpty(varType))
                sb.Append($": {varType}");
            if (!string.IsNullOrEmpty(initializer))
                sb.Append($" = {initializer}");
            sb.AppendLine();
            sb.AppendLine();
        }

        // Generate property with get/set
        sb.Append($"var {varName}");
        if (!string.IsNullOrEmpty(varType))
            sb.Append($": {varType}");
        if (!options.UseBackingField && !string.IsNullOrEmpty(initializer))
            sb.Append($" = {initializer}");
        sb.AppendLine(":");

        // Getter
        if (options.GenerateGetter)
        {
            if (options.UseBackingField)
                sb.AppendLine($"\tget: return {backingField}");
            else
                sb.AppendLine($"\tget: return {varName}");
        }

        // Setter
        if (options.GenerateSetter)
        {
            if (options.UseBackingField)
                sb.AppendLine($"\tset(value): {backingField} = value");
            else
                sb.AppendLine($"\tset(value): {varName} = value");
        }

        return sb.ToString().TrimEnd();
    }

    private string GenerateMethodSyntax(string varName, string varType, GetterSetterOptions options)
    {
        var sb = new StringBuilder();
        var backingField = $"_{varName}";

        // Generate backing field (always needed for method syntax)
        sb.Append($"var {backingField}");
        if (!string.IsNullOrEmpty(varType))
            sb.Append($": {varType}");
        sb.AppendLine();

        // Getter method
        if (options.GenerateGetter)
        {
            sb.AppendLine();
            sb.Append($"func get_{varName}()");
            if (!string.IsNullOrEmpty(varType))
                sb.Append($" -> {varType}");
            sb.AppendLine(":");
            sb.AppendLine($"\treturn {backingField}");
        }

        // Setter method
        if (options.GenerateSetter)
        {
            sb.AppendLine();
            sb.Append($"func set_{varName}(value");
            if (!string.IsNullOrEmpty(varType))
                sb.Append($": {varType}");
            sb.AppendLine("):");
            sb.AppendLine($"\t{backingField} = value");
        }

        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// Options for getter/setter generation.
/// </summary>
internal class GetterSetterOptions
{
    public bool GenerateGetter { get; set; } = true;
    public bool GenerateSetter { get; set; } = true;
    public bool UsePropertySyntax { get; set; } = true;
    public bool UseBackingField { get; set; } = true;
}

/// <summary>
/// Dialog for selecting getter/setter options.
/// </summary>
internal partial class GetterSetterOptionsDialog : ConfirmationDialog
{
    private CheckBox _getterCheck;
    private CheckBox _setterCheck;
    private CheckBox _propertyCheck;
    private CheckBox _backingFieldCheck;
    private TaskCompletionSource<GetterSetterOptions> _completion;

    public GetterSetterOptionsDialog()
    {
        Title = "Generate Getter/Setter";

        GetCancelButton().Pressed += OnCancelled;
        GetOkButton().Pressed += OnOk;
        Canceled += OnCancelled;
        Confirmed += OnOk;

        var vbox = new VBoxContainer();
        AddChild(vbox);

        _getterCheck = new CheckBox { Text = "Generate Getter", ButtonPressed = true };
        vbox.AddChild(_getterCheck);

        _setterCheck = new CheckBox { Text = "Generate Setter", ButtonPressed = true };
        vbox.AddChild(_setterCheck);

        vbox.AddChild(new HSeparator());

        _propertyCheck = new CheckBox { Text = "Use Property Syntax (GDScript 4.x)", ButtonPressed = true };
        _propertyCheck.Toggled += OnPropertyToggled;
        vbox.AddChild(_propertyCheck);

        _backingFieldCheck = new CheckBox { Text = "Use Backing Field (_var)", ButtonPressed = true };
        vbox.AddChild(_backingFieldCheck);

        Size = new Vector2I(320, 200);
    }

    private void OnPropertyToggled(bool toggled)
    {
        // Backing field option is only relevant for property syntax
        _backingFieldCheck.Disabled = !toggled;
        if (!toggled)
            _backingFieldCheck.ButtonPressed = true; // Method syntax always uses backing field
    }

    private void OnOk()
    {
        var options = new GetterSetterOptions
        {
            GenerateGetter = _getterCheck.ButtonPressed,
            GenerateSetter = _setterCheck.ButtonPressed,
            UsePropertySyntax = _propertyCheck.ButtonPressed,
            UseBackingField = _backingFieldCheck.ButtonPressed
        };

        Logger.Info($"GetterSetterOptionsDialog: Getter={options.GenerateGetter}, Setter={options.GenerateSetter}, Property={options.UsePropertySyntax}, BackingField={options.UseBackingField}");
        _completion?.TrySetResult(options);
        Hide();
    }

    private void OnCancelled()
    {
        Logger.Info("GetterSetterOptionsDialog: Cancelled");
        _completion?.TrySetResult(null);
    }

    public Task<GetterSetterOptions> ShowForResult(string varName)
    {
        _completion = new TaskCompletionSource<GetterSetterOptions>();
        Title = $"Generate Getter/Setter for '{varName}'";

        Popup();
        return _completion.Task;
    }
}
