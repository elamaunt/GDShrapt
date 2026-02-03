namespace GDShrapt.Reader
{
    /// <summary>
    /// Helper class for identifying GDScript virtual methods and lifecycle callbacks
    /// </summary>
    public static class GDSpecialMethodHelper
    {
        // Lifecycle methods
        public const string Init = "_init";
        public const string Ready = "_ready";
        public const string EnterTree = "_enter_tree";
        public const string ExitTree = "_exit_tree";
        public const string Notification = "_notification";
        public const string GetConfigurationWarnings = "_get_configuration_warnings";

        // Process methods
        public const string Process = "_process";
        public const string PhysicsProcess = "_physics_process";

        // Input methods
        public const string Input = "_input";
        public const string UnhandledInput = "_unhandled_input";
        public const string UnhandledKeyInput = "_unhandled_key_input";
        public const string ShortcutInput = "_shortcut_input";

        // 2D drawing
        public const string Draw = "_draw";

        // Static constructor
        public const string StaticInit = "_static_init";

        // Property methods (for @export with get/set)
        public const string Get = "_get";
        public const string Set = "_set";
        public const string GetPropertyList = "_get_property_list";
        public const string PropertyCanRevert = "_property_can_revert";
        public const string PropertyGetRevert = "_property_get_revert";
        public const string ValidateProperty = "_validate_property";

        // Editor plugin methods
        public const string HasMainScreen = "_has_main_screen";
        public const string MakeVisible = "_make_visible";
        public const string GetPluginName = "_get_plugin_name";
        public const string GetPluginIcon = "_get_plugin_icon";
        public const string Edit = "_edit";
        public const string Handles = "_handles";
        public const string ForwardCanvasGuiInput = "_forward_canvas_gui_input";
        public const string Forward3DGuiInput = "_forward_3d_gui_input";

        // Resource methods
        public const string ToStringMethod = "_to_string";

        /// <summary>
        /// Checks if the method is a lifecycle callback
        /// </summary>
        public static bool IsLifecycleMethod(this GDMethodDeclaration method)
        {
            var name = method?.Identifier?.Sequence;
            if (name == null)
                return false;

            return name == Init ||
                   name == Ready ||
                   name == EnterTree ||
                   name == ExitTree ||
                   name == Notification ||
                   name == GetConfigurationWarnings ||
                   name == StaticInit;
        }

        /// <summary>
        /// Checks if the method is a process callback
        /// </summary>
        public static bool IsProcessMethod(this GDMethodDeclaration method)
        {
            var name = method?.Identifier?.Sequence;
            if (name == null)
                return false;

            return name == Process || name == PhysicsProcess;
        }

        /// <summary>
        /// Checks if the method is an input handler
        /// </summary>
        public static bool IsInputMethod(this GDMethodDeclaration method)
        {
            var name = method?.Identifier?.Sequence;
            if (name == null)
                return false;

            return name == Input ||
                   name == UnhandledInput ||
                   name == UnhandledKeyInput ||
                   name == ShortcutInput;
        }

        /// <summary>
        /// Checks if the method is a virtual method (starts with underscore)
        /// </summary>
        public static bool IsVirtualMethod(this GDMethodDeclaration method)
        {
            var name = method?.Identifier?.Sequence;
            return name != null && name.StartsWith("_");
        }

        /// <summary>
        /// Checks if the method is a property accessor
        /// </summary>
        public static bool IsPropertyAccessor(this GDMethodDeclaration method)
        {
            var name = method?.Identifier?.Sequence;
            if (name == null)
                return false;

            return name == Get ||
                   name == Set ||
                   name == GetPropertyList ||
                   name == PropertyCanRevert ||
                   name == PropertyGetRevert ||
                   name == ValidateProperty;
        }

        /// <summary>
        /// Checks if the method is _ready
        /// </summary>
        public static bool IsReady(this GDMethodDeclaration method)
        {
            return method?.Identifier?.Sequence == Ready;
        }

        /// <summary>
        /// Checks if the method is _init
        /// </summary>
        public static bool IsInit(this GDMethodDeclaration method)
        {
            return method?.Identifier?.Sequence == Init;
        }

        /// <summary>
        /// Checks if the method is _process
        /// </summary>
        public static bool IsProcess(this GDMethodDeclaration method)
        {
            return method?.Identifier?.Sequence == Process;
        }

        /// <summary>
        /// Checks if the method is _physics_process
        /// </summary>
        public static bool IsPhysicsProcess(this GDMethodDeclaration method)
        {
            return method?.Identifier?.Sequence == PhysicsProcess;
        }

        /// <summary>
        /// Checks if the method is _input
        /// </summary>
        public static bool IsInput(this GDMethodDeclaration method)
        {
            return method?.Identifier?.Sequence == Input;
        }

        /// <summary>
        /// Checks if the method is _enter_tree
        /// </summary>
        public static bool IsEnterTree(this GDMethodDeclaration method)
        {
            return method?.Identifier?.Sequence == EnterTree;
        }

        /// <summary>
        /// Checks if the method is _exit_tree
        /// </summary>
        public static bool IsExitTree(this GDMethodDeclaration method)
        {
            return method?.Identifier?.Sequence == ExitTree;
        }

        /// <summary>
        /// Checks if the method is _draw
        /// </summary>
        public static bool IsDraw(this GDMethodDeclaration method)
        {
            return method?.Identifier?.Sequence == Draw;
        }

        /// <summary>
        /// Checks if the method is _notification
        /// </summary>
        public static bool IsNotification(this GDMethodDeclaration method)
        {
            return method?.Identifier?.Sequence == Notification;
        }

        /// <summary>
        /// Checks if the method name is a known GDScript virtual method
        /// </summary>
        public static bool IsKnownVirtualMethod(string methodName)
        {
            if (methodName == null)
                return false;

            return methodName == Init ||
                   methodName == Ready ||
                   methodName == EnterTree ||
                   methodName == ExitTree ||
                   methodName == Notification ||
                   methodName == GetConfigurationWarnings ||
                   methodName == Process ||
                   methodName == PhysicsProcess ||
                   methodName == Input ||
                   methodName == UnhandledInput ||
                   methodName == UnhandledKeyInput ||
                   methodName == ShortcutInput ||
                   methodName == Draw ||
                   methodName == StaticInit ||
                   methodName == Get ||
                   methodName == Set ||
                   methodName == GetPropertyList ||
                   methodName == PropertyCanRevert ||
                   methodName == PropertyGetRevert ||
                   methodName == ValidateProperty ||
                   methodName == ToStringMethod;
        }

        /// <summary>
        /// Checks if the method is a lifecycle method that is guaranteed to run after _ready().
        /// Includes: _process, _physics_process, _input, _unhandled_input, _unhandled_key_input,
        /// _shortcut_input, _draw, and _ready itself.
        /// </summary>
        public static bool IsLifecycleMethodAfterReady(this GDMethodDeclaration method)
        {
            return method.IsProcessMethod() ||
                   method.IsInputMethod() ||
                   method.IsDraw() ||
                   method.IsReady();
        }

        /// <summary>
        /// Checks if the method name is a lifecycle method that runs after _ready().
        /// </summary>
        public static bool IsLifecycleMethodAfterReady(string methodName)
        {
            if (methodName == null)
                return false;

            return methodName == Ready ||
                   methodName == Process ||
                   methodName == PhysicsProcess ||
                   methodName == Input ||
                   methodName == UnhandledInput ||
                   methodName == UnhandledKeyInput ||
                   methodName == ShortcutInput ||
                   methodName == Draw;
        }
    }
}
