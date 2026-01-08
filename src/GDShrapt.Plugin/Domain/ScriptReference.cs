namespace GDShrapt.Plugin;

internal class ScriptReference
{
    private string _fullPath;

    public ScriptReference(string fullPath)
    {
        _fullPath = fullPath.Replace('\\', '/').TrimEnd('/');
    }

    public string FullPath => _fullPath;

    /// <summary>
    /// Gets the resource path (res://...) for this script.
    /// </summary>
    public string? ResourcePath
    {
        get
        {
            if (string.IsNullOrEmpty(_fullPath))
                return null;

            // Try to convert full path to res:// path
            var projectPath = Godot.ProjectSettings.GlobalizePath("res://").Replace('\\', '/').TrimEnd('/');
            var normalizedPath = _fullPath.Replace('\\', '/');

            if (normalizedPath.StartsWith(projectPath, System.StringComparison.OrdinalIgnoreCase))
            {
                return "res://" + normalizedPath.Substring(projectPath.Length).TrimStart('/');
            }

            return null;
        }
    }

    public override int GetHashCode()
    {
        return _fullPath?.GetHashCode() ?? base.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj is ScriptReference reference)
            return string.Equals(reference._fullPath, _fullPath, System.StringComparison.OrdinalIgnoreCase);

        return base.Equals(obj);
    }
}
