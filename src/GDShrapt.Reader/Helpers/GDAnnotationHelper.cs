namespace GDShrapt.Reader
{
    /// <summary>
    /// Helper class for identifying and working with GDScript annotations (@export, @onready, etc.)
    /// </summary>
    public static class GDAnnotationHelper
    {
        // Export annotations
        public const string Export = "export";
        public const string ExportRange = "export_range";
        public const string ExportExp = "export_exp_easing";
        public const string ExportFlags = "export_flags";
        public const string ExportFlags2DPhysics = "export_flags_2d_physics";
        public const string ExportFlags2DRender = "export_flags_2d_render";
        public const string ExportFlags2DNavigation = "export_flags_2d_navigation";
        public const string ExportFlags3DPhysics = "export_flags_3d_physics";
        public const string ExportFlags3DRender = "export_flags_3d_render";
        public const string ExportFlags3DNavigation = "export_flags_3d_navigation";
        public const string ExportEnum = "export_enum";
        public const string ExportFile = "export_file";
        public const string ExportDir = "export_dir";
        public const string ExportGlobalFile = "export_global_file";
        public const string ExportGlobalDir = "export_global_dir";
        public const string ExportMultiline = "export_multiline";
        public const string ExportPlaceholder = "export_placeholder";
        public const string ExportColorNoAlpha = "export_color_no_alpha";
        public const string ExportNodePath = "export_node_path";
        public const string ExportCategory = "export_category";
        public const string ExportGroup = "export_group";
        public const string ExportSubgroup = "export_subgroup";
        public const string ExportStorage = "export_storage";
        public const string ExportCustom = "export_custom";
        public const string ExportToolButton = "export_tool_button";

        // Other common annotations
        public const string Onready = "onready";
        public const string Tool = "tool";
        public const string Icon = "icon";
        public const string WarningIgnore = "warning_ignore";
        public const string Rpc = "rpc";
        public const string Static_Unload = "static_unload";
        public const string Abstract = "abstract";

        /// <summary>
        /// Checks if the attribute is any type of export annotation
        /// </summary>
        public static bool IsExportAnnotation(this GDAttribute attribute)
        {
            if (attribute?.Name?.Sequence == null)
                return false;

            var name = attribute.Name.Sequence;
            return name == Export ||
                   name.StartsWith("export_");
        }

        /// <summary>
        /// Checks if the attribute is @export
        /// </summary>
        public static bool IsExport(this GDAttribute attribute)
        {
            return attribute?.Name?.Sequence == Export;
        }

        /// <summary>
        /// Checks if the attribute is @export_range
        /// </summary>
        public static bool IsExportRange(this GDAttribute attribute)
        {
            return attribute?.Name?.Sequence == ExportRange;
        }

        /// <summary>
        /// Checks if the attribute is @export_enum
        /// </summary>
        public static bool IsExportEnum(this GDAttribute attribute)
        {
            return attribute?.Name?.Sequence == ExportEnum;
        }

        /// <summary>
        /// Checks if the attribute is @export_flags or any of its variants
        /// </summary>
        public static bool IsExportFlags(this GDAttribute attribute)
        {
            if (attribute?.Name?.Sequence == null)
                return false;

            var name = attribute.Name.Sequence;
            return name == ExportFlags ||
                   name == ExportFlags2DPhysics ||
                   name == ExportFlags2DRender ||
                   name == ExportFlags2DNavigation ||
                   name == ExportFlags3DPhysics ||
                   name == ExportFlags3DRender ||
                   name == ExportFlags3DNavigation;
        }

        /// <summary>
        /// Checks if the attribute is @export_file or @export_global_file
        /// </summary>
        public static bool IsExportFile(this GDAttribute attribute)
        {
            if (attribute?.Name?.Sequence == null)
                return false;

            var name = attribute.Name.Sequence;
            return name == ExportFile || name == ExportGlobalFile;
        }

        /// <summary>
        /// Checks if the attribute is @export_dir or @export_global_dir
        /// </summary>
        public static bool IsExportDir(this GDAttribute attribute)
        {
            if (attribute?.Name?.Sequence == null)
                return false;

            var name = attribute.Name.Sequence;
            return name == ExportDir || name == ExportGlobalDir;
        }

        /// <summary>
        /// Checks if the attribute is @export_group or @export_subgroup
        /// </summary>
        public static bool IsExportGroup(this GDAttribute attribute)
        {
            if (attribute?.Name?.Sequence == null)
                return false;

            var name = attribute.Name.Sequence;
            return name == ExportGroup || name == ExportSubgroup;
        }

        /// <summary>
        /// Checks if the attribute is @export_category
        /// </summary>
        public static bool IsExportCategory(this GDAttribute attribute)
        {
            return attribute?.Name?.Sequence == ExportCategory;
        }

        /// <summary>
        /// Checks if the attribute is @onready
        /// </summary>
        public static bool IsOnready(this GDAttribute attribute)
        {
            return attribute?.Name?.Sequence == Onready;
        }

        /// <summary>
        /// Checks if the attribute is @tool
        /// </summary>
        public static bool IsTool(this GDAttribute attribute)
        {
            return attribute?.Name?.Sequence == Tool;
        }

        /// <summary>
        /// Checks if the attribute is @icon
        /// </summary>
        public static bool IsIcon(this GDAttribute attribute)
        {
            return attribute?.Name?.Sequence == Icon;
        }

        /// <summary>
        /// Checks if the attribute is @warning_ignore
        /// </summary>
        public static bool IsWarningIgnore(this GDAttribute attribute)
        {
            return attribute?.Name?.Sequence == WarningIgnore;
        }

        /// <summary>
        /// Checks if the attribute is @rpc
        /// </summary>
        public static bool IsRpc(this GDAttribute attribute)
        {
            return attribute?.Name?.Sequence == Rpc;
        }

        /// <summary>
        /// Checks if the attribute is @static_unload
        /// </summary>
        public static bool IsStaticUnload(this GDAttribute attribute)
        {
            return attribute?.Name?.Sequence == Static_Unload;
        }

        /// <summary>
        /// Checks if the attribute is @abstract (Godot 4.5+)
        /// </summary>
        public static bool IsAbstract(this GDAttribute attribute)
        {
            return attribute?.Name?.Sequence == Abstract;
        }

        /// <summary>
        /// Gets the annotation name from an attribute
        /// </summary>
        public static string GetAnnotationName(this GDAttribute attribute)
        {
            return attribute?.Name?.Sequence;
        }

        /// <summary>
        /// Checks if the attribute has any parameters
        /// </summary>
        public static bool HasParameters(this GDAttribute attribute)
        {
            return attribute?.Parameters != null && attribute.Parameters.Count > 0;
        }
    }
}
