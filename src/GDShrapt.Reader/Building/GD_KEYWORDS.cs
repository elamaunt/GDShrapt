namespace GDShrapt.Reader
{
    public static partial class GD
    {
        public static class Keyword
        {
            public static GDBreakKeyword Break => new GDBreakKeyword();
            public static GDBreakPointKeyword BreakPoint => new GDBreakPointKeyword();
            public static GDClassKeyword Class => new GDClassKeyword();
            public static GDClassNameKeyword ClassName => new GDClassNameKeyword();
            public static GDConstKeyword Const => new GDConstKeyword();
            public static GDContinueKeyword Continue => new GDContinueKeyword();
            public static GDElifKeyword Elif  => new GDElifKeyword();
            public static GDElseKeyword Else => new GDElseKeyword();
            public static GDEnumKeyword Enum  => new GDEnumKeyword();
            public static GDExportKeyword Export  => new GDExportKeyword();
            public static GDExtendsKeyword Extends => new GDExtendsKeyword();
            public static GDFalseKeyword False => new GDFalseKeyword();
            public static GDForKeyword For => new GDForKeyword();
            public static GDFuncKeyword Func => new GDFuncKeyword();
            public static GDIfKeyword If => new GDIfKeyword();
            public static GDInKeyword In => new GDInKeyword();
            public static GDMatchKeyword Match => new GDMatchKeyword();
            public static GDOnreadyKeyword Onready => new GDOnreadyKeyword();
            public static GDPassKeyword Pass => new GDPassKeyword();
            public static GDReturnKeyword Return => new GDReturnKeyword();
            public static GDSetGetKeyword SetGet => new GDSetGetKeyword();
            public static GDSignalKeyword Signal => new GDSignalKeyword();
            public static GDStaticKeyword Static => new GDStaticKeyword();
            public static GDToolKeyword Tool => new GDToolKeyword();
            public static GDTrueKeyword True => new GDTrueKeyword();
            public static GDVarKeyword Var => new GDVarKeyword();
            public static GDWhileKeyword While => new GDWhileKeyword();
            public static GDYieldKeyword Yield => new GDYieldKeyword();
        }
    }
}
