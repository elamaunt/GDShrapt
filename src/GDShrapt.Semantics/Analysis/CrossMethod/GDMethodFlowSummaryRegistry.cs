namespace GDShrapt.Semantics
{
    /// <summary>
    /// Registry for caching method flow summaries.
    /// </summary>
    public class GDMethodFlowSummaryRegistry
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, GDMethodFlowSummary> _summaries = new();
        private readonly Dictionary<string, List<string>> _summariesByFile = new();

        /// <summary>
        /// Registers a method flow summary.
        /// </summary>
        public void Register(GDMethodFlowSummary summary, string? filePath = null)
        {
            lock (_lock)
            {
                _summaries[summary.MethodKey] = summary;

                if (!string.IsNullOrEmpty(filePath))
                {
                    if (!_summariesByFile.TryGetValue(filePath, out var keys))
                    {
                        keys = new List<string>();
                        _summariesByFile[filePath] = keys;
                    }
                    if (!keys.Contains(summary.MethodKey))
                        keys.Add(summary.MethodKey);
                }
            }
        }

        /// <summary>
        /// Gets a method flow summary by key.
        /// </summary>
        public GDMethodFlowSummary? GetSummary(string methodKey)
        {
            lock (_lock)
            {
                return _summaries.TryGetValue(methodKey, out var summary) ? summary : null;
            }
        }

        /// <summary>
        /// Gets a method flow summary by class and method name.
        /// </summary>
        public GDMethodFlowSummary? GetSummary(string className, string methodName)
        {
            return GetSummary($"{className}.{methodName}");
        }

        /// <summary>
        /// Checks if a summary exists for the given method key.
        /// </summary>
        public bool HasSummary(string methodKey)
        {
            lock (_lock)
            {
                return _summaries.ContainsKey(methodKey);
            }
        }

        /// <summary>
        /// Gets all registered summaries.
        /// </summary>
        public IEnumerable<GDMethodFlowSummary> GetAllSummaries()
        {
            lock (_lock)
            {
                return _summaries.Values.ToList();
            }
        }

        /// <summary>
        /// Invalidates all summaries for a file.
        /// </summary>
        public void InvalidateFile(string filePath)
        {
            lock (_lock)
            {
                if (_summariesByFile.TryGetValue(filePath, out var keys))
                {
                    foreach (var key in keys)
                        _summaries.Remove(key);
                    _summariesByFile.Remove(filePath);
                }
            }
        }

        /// <summary>
        /// Clears all summaries.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _summaries.Clear();
                _summariesByFile.Clear();
            }
        }

        /// <summary>
        /// Gets the number of registered summaries.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _summaries.Count;
                }
            }
        }
    }
}
