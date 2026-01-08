using GDShrapt.Reader;
using Godot;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal abstract class Command
{
    public GDShraptPlugin Plugin { get; }
    public Godot.ScriptEditor Editor => EditorInterface.Singleton.GetScriptEditor();
    public GDProjectMap Map => Plugin.ProjectMap;

    public Command(GDShraptPlugin plugin)
    {
        Plugin = plugin;
    }

    public abstract Task Execute(IScriptEditor editor);
}
