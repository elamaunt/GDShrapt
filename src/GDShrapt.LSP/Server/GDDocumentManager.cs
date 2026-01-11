using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GDShrapt.Semantics;

namespace GDShrapt.LSP;

/// <summary>
/// Manages open documents in the language server.
/// </summary>
public class GDDocumentManager
{
    private readonly ConcurrentDictionary<string, GDOpenDocument> _documents = new();
    private readonly GDScriptProject _project;

    public GDDocumentManager(GDScriptProject project)
    {
        _project = project;
    }

    /// <summary>
    /// Opens a document.
    /// </summary>
    public void OpenDocument(string uri, string content, int version)
    {
        var filePath = UriToPath(uri);
        var doc = new GDOpenDocument(uri, filePath, content, version);
        _documents[uri] = doc;

        // Get the script and reload with new content
        var script = _project.GetScript(filePath);
        script?.Reload(content);
    }

    /// <summary>
    /// Updates a document's content.
    /// </summary>
    public void UpdateDocument(string uri, string content, int version)
    {
        if (_documents.TryGetValue(uri, out var doc))
        {
            doc.Content = content;
            doc.Version = version;

            // Reload the script with new content
            var script = _project.GetScript(doc.FilePath);
            script?.Reload(content);
        }
    }

    /// <summary>
    /// Closes a document.
    /// </summary>
    public void CloseDocument(string uri)
    {
        if (_documents.TryRemove(uri, out var doc))
        {
            // Reload from disk
            var script = _project.GetScript(doc.FilePath);
            script?.Reload();
        }
    }

    /// <summary>
    /// Gets an open document.
    /// </summary>
    public GDOpenDocument? GetDocument(string uri)
    {
        _documents.TryGetValue(uri, out var doc);
        return doc;
    }

    /// <summary>
    /// Gets all open documents.
    /// </summary>
    public IEnumerable<GDOpenDocument> GetAllDocuments()
    {
        return _documents.Values;
    }

    /// <summary>
    /// Converts a file URI to a local path.
    /// </summary>
    public static string UriToPath(string uri)
    {
        if (uri.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
        {
            var path = uri.Substring(8);
            // Handle Windows paths (file:///C:/...)
            if (path.Length > 2 && path[1] == ':')
            {
                return path.Replace('/', '\\');
            }
            // Unix paths
            return "/" + path;
        }
        return uri;
    }

    /// <summary>
    /// Converts a local path to a file URI.
    /// </summary>
    public static string PathToUri(string path)
    {
        path = path.Replace('\\', '/');
        if (!path.StartsWith("/"))
        {
            // Windows path
            return "file:///" + path;
        }
        // Unix path
        return "file://" + path;
    }
}

/// <summary>
/// Represents an open document.
/// </summary>
public class GDOpenDocument
{
    public string Uri { get; }
    public string FilePath { get; }
    public string Content { get; set; }
    public int Version { get; set; }

    public GDOpenDocument(string uri, string filePath, string content, int version)
    {
        Uri = uri;
        FilePath = filePath;
        Content = content;
        Version = version;
    }
}
