namespace GDShrapt.Reader
{
    public static partial class GDBuildingExtensionMethods
    {
        public static T AddBreakKeyword<T>(this T receiver) where T : ITokenReceiver<GDBreakKeyword> => receiver.Add(new GDBreakKeyword());
        public static T AddBreakPointKeyword<T>(this T receiver) where T : ITokenReceiver<GDBreakPointKeyword> => receiver.Add(new GDBreakPointKeyword());
        public static T AddClassKeyword<T>(this T receiver) where T : ITokenReceiver<GDClassKeyword> => receiver.Add(new GDClassKeyword());
        public static T AddClassNameKeyword<T>(this T receiver) where T : ITokenReceiver<GDClassNameKeyword> => receiver.Add(new GDClassNameKeyword());
        public static T AddConstKeyword<T>(this T receiver) where T : ITokenReceiver<GDConstKeyword> => receiver.Add(new GDConstKeyword());
        public static T AddContinueKeyword<T>(this T receiver) where T : ITokenReceiver<GDContinueKeyword> => receiver.Add(new GDContinueKeyword());
        public static T AddElifKeyword<T>(this T receiver) where T : ITokenReceiver<GDElifKeyword> => receiver.Add(new GDElifKeyword());
        public static T AddElseKeyword<T>(this T receiver) where T : ITokenReceiver<GDElseKeyword> => receiver.Add(new GDElseKeyword());
        public static T AddEnumKeyword<T>(this T receiver) where T : ITokenReceiver<GDEnumKeyword> => receiver.Add(new GDEnumKeyword());
        public static T AddExportKeyword<T>(this T receiver) where T : ITokenReceiver<GDExportKeyword> => receiver.Add(new GDExportKeyword());
        public static T AddExtendsKeyword<T>(this T receiver) where T : ITokenReceiver<GDExtendsKeyword> => receiver.Add(new GDExtendsKeyword());
        public static T AddFalseKeyword<T>(this T receiver) where T : ITokenReceiver<GDFalseKeyword> => receiver.Add(new GDFalseKeyword());
        public static T AddForKeyword<T>(this T receiver) where T : ITokenReceiver<GDForKeyword> => receiver.Add(new GDForKeyword());
        public static T AddFuncKeyword<T>(this T receiver) where T : ITokenReceiver<GDFuncKeyword> => receiver.Add(new GDFuncKeyword());
        public static T AddIfKeyword<T>(this T receiver) where T : ITokenReceiver<GDIfKeyword> => receiver.Add(new GDIfKeyword());
        public static T AddInKeyword<T>(this T receiver) where T : ITokenReceiver<GDInKeyword> => receiver.Add(new GDInKeyword());
        public static T AddMatchKeyword<T>(this T receiver) where T : ITokenReceiver<GDMatchKeyword> => receiver.Add(new GDMatchKeyword());
        public static T AddOnreadyKeyword<T>(this T receiver) where T : ITokenReceiver<GDOnreadyKeyword> => receiver.Add(new GDOnreadyKeyword());
        public static T AddPassKeyword<T>(this T receiver) where T : ITokenReceiver<GDPassKeyword> => receiver.Add(new GDPassKeyword());
        public static T AddReturnKeyword<T>(this T receiver) where T : ITokenReceiver<GDReturnKeyword> => receiver.Add(new GDReturnKeyword());
        public static T AddSetGetKeyword<T>(this T receiver) where T : ITokenReceiver<GDSetGetKeyword> => receiver.Add(new GDSetGetKeyword());
        public static T AddSignalKeyword<T>(this T receiver) where T : ITokenReceiver<GDSignalKeyword> => receiver.Add(new GDSignalKeyword());
        public static T AddStaticKeyword<T>(this T receiver) where T : ITokenReceiver<GDStaticKeyword> => receiver.Add(new GDStaticKeyword());
        public static T AddToolKeyword<T>(this T receiver) where T : ITokenReceiver<GDToolKeyword> => receiver.Add(new GDToolKeyword());
        public static T AddTrueKeyword<T>(this T receiver) where T : ITokenReceiver<GDTrueKeyword> => receiver.Add(new GDTrueKeyword());
        public static T AddVarKeyword<T>(this T receiver) where T : ITokenReceiver<GDVarKeyword> => receiver.Add(new GDVarKeyword());
        public static T AddWhileKeyword<T>(this T receiver) where T : ITokenReceiver<GDWhileKeyword> => receiver.Add(new GDWhileKeyword());
        public static T AddYieldKeyword<T>(this T receiver) where T : ITokenReceiver<GDYieldKeyword> => receiver.Add(new GDYieldKeyword());

        // Additional keyword extensions
        public static T AddArrayKeyword<T>(this T receiver) where T : ITokenReceiver<GDArrayKeyword> => receiver.Add(new GDArrayKeyword());
        public static T AddAwaitKeyword<T>(this T receiver) where T : ITokenReceiver<GDAwaitKeyword> => receiver.Add(new GDAwaitKeyword());
        public static T AddDictionaryKeyword<T>(this T receiver) where T : ITokenReceiver<GDDictionaryKeyword> => receiver.Add(new GDDictionaryKeyword());
        public static T AddGetKeyword<T>(this T receiver) where T : ITokenReceiver<GDGetKeyword> => receiver.Add(new GDGetKeyword());
        public static T AddSetKeyword<T>(this T receiver) where T : ITokenReceiver<GDSetKeyword> => receiver.Add(new GDSetKeyword());
        public static T AddReturnTypeKeyword<T>(this T receiver) where T : ITokenReceiver<GDReturnTypeKeyword> => receiver.Add(new GDReturnTypeKeyword());
        public static T AddWhenKeyword<T>(this T receiver) where T : ITokenReceiver<GDWhenKeyword> => receiver.Add(new GDWhenKeyword());
    }
}
