namespace GDShrapt.Plugin;

/// <summary>
/// Container that detects layout mode changes based on available space.
/// Used for adaptive UI that works in narrow side panels, wide bottom panels, and full-screen modes.
/// </summary>
internal partial class GDResponsiveContainer : Container
{
    /// <summary>
    /// Layout modes for different panel sizes.
    /// </summary>
    public new enum LayoutMode
    {
        /// <summary>
        /// Minimal layout for very small heights (&lt; 300px).
        /// Shows only signature and confidence, no tabs.
        /// </summary>
        Minimal,

        /// <summary>
        /// Compact layout for narrow widths (&lt; 350px) or limited height (&lt; 500px).
        /// Single column, collapsed sections.
        /// </summary>
        Compact,

        /// <summary>
        /// Normal layout for medium widths (350-600px).
        /// All tabs visible, vertical arrangement.
        /// </summary>
        Normal,

        /// <summary>
        /// Wide layout for large widths (&gt; 600px).
        /// Side-by-side arrangement of Overview and Inflows/Outflows.
        /// </summary>
        Wide
    }

    private LayoutMode _currentMode = LayoutMode.Normal;

    // Breakpoints (configurable) - adjusted for better scaling behavior
    private float _compactWidthThreshold = 400f;   // Increased from 350 to catch more narrow windows
    private float _wideWidthThreshold = 700f;      // Increased from 600 for wider threshold
    private float _minimalHeightThreshold = 300f;  // Keep same - very small heights
    private float _compactHeightThreshold = 400f;  // Reduced from 500 to 400 for clearer boundaries

    /// <summary>
    /// Fired when the layout mode changes.
    /// </summary>
    public event Action<LayoutMode> LayoutModeChanged;

    /// <summary>
    /// Gets the current layout mode.
    /// </summary>
    public LayoutMode CurrentMode => _currentMode;

    /// <summary>
    /// Gets or sets the compact width threshold.
    /// </summary>
    public float CompactWidthThreshold
    {
        get => _compactWidthThreshold;
        set
        {
            _compactWidthThreshold = value;
            UpdateLayoutMode();
        }
    }

    /// <summary>
    /// Gets or sets the wide width threshold.
    /// </summary>
    public float WideWidthThreshold
    {
        get => _wideWidthThreshold;
        set
        {
            _wideWidthThreshold = value;
            UpdateLayoutMode();
        }
    }

    /// <summary>
    /// Gets or sets the minimal height threshold.
    /// </summary>
    public float MinimalHeightThreshold
    {
        get => _minimalHeightThreshold;
        set
        {
            _minimalHeightThreshold = value;
            UpdateLayoutMode();
        }
    }

    /// <summary>
    /// Gets or sets the compact height threshold.
    /// </summary>
    public float CompactHeightThreshold
    {
        get => _compactHeightThreshold;
        set
        {
            _compactHeightThreshold = value;
            UpdateLayoutMode();
        }
    }

    public GDResponsiveContainer()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            UpdateLayoutMode();
            // Also update children sizes when resized
            UpdateChildrenSizes();
        }
        else if (what == NotificationSortChildren)
        {
            // When children are added or layout is requested, update their sizes
            UpdateChildrenSizes();
        }
    }

    private void UpdateChildrenSizes()
    {
        // Make all children fill this container's size
        var rect = new Rect2(Vector2.Zero, Size);
        foreach (var child in GetChildren())
        {
            if (child is Control control)
            {
                FitChildInRect(control, rect);
            }
        }
    }

    private void UpdateLayoutMode()
    {
        var newMode = DetermineLayoutMode(Size);
        if (newMode != _currentMode)
        {
            _currentMode = newMode;
            LayoutModeChanged?.Invoke(_currentMode);
        }
    }

    private LayoutMode DetermineLayoutMode(Vector2 size)
    {
        // Priority 1: Critical height constraint - very small heights
        if (size.Y < _minimalHeightThreshold)
            return LayoutMode.Minimal;

        // Priority 2: Narrow width OR limited height - both need compact mode
        // This ensures windows like 350x400 correctly go to Compact
        if (size.X < _compactWidthThreshold || size.Y < _compactHeightThreshold)
            return LayoutMode.Compact;

        // Priority 3: Wide layout for large widths
        if (size.X > _wideWidthThreshold)
            return LayoutMode.Wide;

        // Default: Normal layout
        return LayoutMode.Normal;
    }

    /// <summary>
    /// Forces a layout mode update. Call this after changing thresholds.
    /// </summary>
    public void RefreshLayoutMode()
    {
        var newMode = DetermineLayoutMode(Size);
        _currentMode = newMode;
        LayoutModeChanged?.Invoke(_currentMode);
    }

    /// <summary>
    /// Checks if the current mode is compact (Minimal or Compact).
    /// </summary>
    public bool IsCompactLayout => _currentMode == LayoutMode.Minimal || _currentMode == LayoutMode.Compact;

    /// <summary>
    /// Checks if the current mode is minimal.
    /// </summary>
    public bool IsMinimalLayout => _currentMode == LayoutMode.Minimal;

    /// <summary>
    /// Checks if the current mode is wide.
    /// </summary>
    public bool IsWideLayout => _currentMode == LayoutMode.Wide;
}
