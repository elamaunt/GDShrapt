using Godot;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal partial class NewMethodNameDialog : ConfirmationDialog
{
    private LineEdit _nameEdit;
    private TaskCompletionSource<string?>? _showCompletion;

    public NewMethodNameDialog()
    {
        Title = "Enter new method name";

        GetCancelButton().Pressed += OnCancelled;
        GetOkButton().Pressed += OnOk;
        Canceled += OnCancelled;
        Confirmed += OnOk;

        _nameEdit = new LineEdit()
        {
            Text = "new_method"
        };

        AddChild(_nameEdit);

        _nameEdit.TextSubmitted += OnNewText;

        Size = new Vector2I(300, 100);
    }

    public void OnNewText(string text)
    {
        Logger.Info($"OnNewText {text}");
        OnOk();
        Hide();
    }

    public void OnOk()
    {
        var name = NewMethodName;
        Logger.Info($"OnOk NewMethodNameDialog");
        Logger.Info($"Result {name}");

        _showCompletion?.TrySetResult(name);
    }

    public void OnCancelled()
    {
        Logger.Info($"OnCancelled NewMethodNameDialog");
        _showCompletion?.TrySetResult(null);
    }

    public string? NewMethodName => _nameEdit.Text?.Trim();

    public Task<string?> ShowForResult()
    {
        _showCompletion = new TaskCompletionSource<string?>();

        Popup();

        _nameEdit.Text = "new_method";
        _nameEdit.SelectAll();
        _nameEdit.GrabFocus();
        _nameEdit.CaretColumn = "new_method".Length;

        return _showCompletion.Task;
    }
}
