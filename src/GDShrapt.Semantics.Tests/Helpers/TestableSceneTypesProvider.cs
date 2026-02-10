using System.Reflection;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Test helper that extends GDSceneTypesProvider to allow programmatic firing of SceneChanged.
/// </summary>
internal class TestableSceneTypesProvider : GDSceneTypesProvider
{
    public TestableSceneTypesProvider(string projectPath, IGDFileSystem? fileSystem = null)
        : base(projectPath, fileSystem, GDNullLogger.Instance)
    {
    }

    /// <summary>
    /// Fires the SceneChanged event with the specified arguments.
    /// Uses reflection because the event can only be invoked from the declaring class.
    /// </summary>
    public void RaiseSceneChanged(GDSceneChangedEventArgs args)
    {
        var field = typeof(GDSceneTypesProvider)
            .GetField("SceneChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        if (field != null)
        {
            var handler = field.GetValue(this) as EventHandler<GDSceneChangedEventArgs>;
            handler?.Invoke(this, args);
        }
    }
}
