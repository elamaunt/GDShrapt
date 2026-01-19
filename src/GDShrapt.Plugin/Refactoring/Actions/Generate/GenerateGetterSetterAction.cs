using GDShrapt.Semantics;
using Godot;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Generates getter and setter for a class variable.
/// Supports both GDScript 4.x property syntax and traditional method syntax.
/// Delegates to GDGenerateGetterSetterService for the actual logic.
/// </summary>
internal class GenerateGetterSetterAction : GDRefactoringActionBase
{
    private readonly GDGenerateGetterSetterService _service = new();

    public override string Id => "generate_getter_setter";
    public override string DisplayName => "Generate Getter/Setter";
    public override GDRefactoringCategory Category => GDRefactoringCategory.Generate;
    public override string Shortcut => "Ctrl+Alt+G";
    public override int Priority => 10;

    public override bool IsAvailable(GDPluginRefactoringContext context)
    {
        if (context?.ContainingClass == null)
            return false;

        if (!context.IsOnClassVariable)
            return false;

        var semanticsContext = context.BuildSemanticsContext();
        return semanticsContext != null && _service.CanExecute(semanticsContext);
    }

    protected override string ValidateContext(GDPluginRefactoringContext context)
    {
        var baseError = base.ValidateContext(context);
        if (baseError != null) return baseError;

        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
            return "Failed to build refactoring context";

        if (!_service.CanExecute(semanticsContext))
            return "No suitable variable declaration found";

        return null;
    }

    protected override async Task ExecuteInternalAsync(GDPluginRefactoringContext context)
    {
        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
            throw new GDRefactoringException("Failed to build refactoring context");

        // Show options dialog
        var optionsDialog = new GetterSetterOptionsDialog();
        context.DialogParent?.AddChild(optionsDialog);

        var varDecl = context.GetVariableDeclaration();
        var varName = varDecl?.Identifier?.ToString() ?? "variable";

        var pluginOptions = await optionsDialog.ShowForResult(varName);
        optionsDialog.QueueFree();

        if (pluginOptions == null)
        {
            Logger.Info("GenerateGetterSetterAction: Cancelled by user");
            return;
        }

        // Convert plugin options to semantics options
        var options = new GDGetterSetterOptions
        {
            GenerateGetter = pluginOptions.GenerateGetter,
            GenerateSetter = pluginOptions.GenerateSetter,
            UsePropertySyntax = pluginOptions.UsePropertySyntax,
            UseBackingField = pluginOptions.UseBackingField
        };

        // Plan the refactoring
        var plan = _service.Plan(semanticsContext, options);
        if (!plan.Success)
            throw new GDRefactoringException(plan.ErrorMessage ?? "Failed to plan getter/setter generation");

        // Show preview dialog
        var previewDialog = new RefactoringPreviewDialog();
        context.DialogParent?.AddChild(previewDialog);

        try
        {
            var syntaxType = options.UsePropertySyntax ? "property syntax" : "method syntax";
            var title = $"Generate Getter/Setter ({syntaxType})";

            // In Base Plugin: Apply is disabled (Pro required)
            var canApply = false;
            var proMessage = "GDShrapt Pro required to apply this refactoring";

            var result = await previewDialog.ShowForResult(
                title,
                plan.OriginalCode,
                plan.ResultCode,
                canApply,
                "Apply",
                proMessage);

            if (result.ShouldApply && canApply)
            {
                // Execute the refactoring
                var executeResult = _service.Execute(semanticsContext, options);
                if (!executeResult.Success)
                    throw new GDRefactoringException(executeResult.ErrorMessage ?? "Failed to execute getter/setter generation");

                // Apply edits to the editor
                ApplyEdits(context, executeResult);
            }
        }
        finally
        {
            previewDialog.QueueFree();
        }
    }

    private void ApplyEdits(GDPluginRefactoringContext context, GDRefactoringResult result)
    {
        var editor = context.Editor;

        foreach (var edit in result.Edits)
        {
            var oldTextLines = edit.OldText.Split('\n');
            var endLine = edit.Line + oldTextLines.Length - 1;
            var endColumn = oldTextLines.Length > 1
                ? oldTextLines[^1].Length
                : edit.Column + edit.OldText.Length;

            editor.Select(edit.Line, edit.Column, endLine, endColumn);
            editor.Cut();
            editor.InsertTextAtCursor(edit.NewText);
        }

        editor.ReloadScriptFromText();
    }
}

/// <summary>
/// Options for getter/setter generation (Plugin-side).
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
