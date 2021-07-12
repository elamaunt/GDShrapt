using System;

namespace GDShrapt.Reader
{
    public static partial class GDBuildingExtensionMethods
    {
        public static T AddToolAtribute<T>(this T receiver)
           where T : ITokenReceiver<GDToolAtribute>
        {
            receiver.HandleReceivedToken(GD.Atribute.Tool());
            return receiver;
        }

        public static T AddClassNameAtribute<T>(this T receiver, string name)
            where T : ITokenReceiver<GDClassNameAtribute>
        {
            receiver.HandleReceivedToken(GD.Atribute.ClassName(name));
            return receiver;
        }

        public static T AddExtendsAtribute<T>(this T receiver, string baseTypeName)
            where T : ITokenReceiver<GDExtendsAtribute>
        {
            receiver.HandleReceivedToken(GD.Atribute.Extends(baseTypeName));
            return receiver;
        }

        public static T AddExtendsWithPathAtribute<T>(this T receiver, string path)
            where T : ITokenReceiver<GDExtendsAtribute>
        {
            receiver.HandleReceivedToken(GD.Atribute.ExtendsPath(path));
            return receiver;
        }

        public static T AddAtributes<T>(this T receiver, Func<GDClassAtributesList, GDClassAtributesList> setup)
            where T : ITokenReceiver<GDClassAtributesList>
        {
            receiver.HandleReceivedToken(setup(new GDClassAtributesList()));
            return receiver;
        }

        public static T AddMembers<T>(this T receiver, Func<GDClassMembersList, GDClassMembersList> setup)
            where T : ITokenReceiver<GDClassMembersList>
        {
            receiver.HandleReceivedToken(setup(new GDClassMembersList()));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, string name)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(name));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, string name, string type)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(name, type));
            return receiver;
        }
        public static T AddVariable<T>(this T receiver, string name, string type, GDExpression initializer)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(name, type, initializer));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, string name, GDExpression initializer)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(name, initializer));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, string identifier, string type, GDExportDeclaration export, GDExpression initializer)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(identifier, type, export, initializer));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, string identifier, string type, GDExpression initializer, GDIdentifier setMethod, GDIdentifier getMethod)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(identifier, type, initializer, setMethod, getMethod));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, string identifier, string type, GDExportDeclaration export, GDExpression initializer, GDIdentifier setMethod, GDIdentifier getMethod)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(identifier, type, export, initializer, setMethod, getMethod));
            return receiver;
        }
        public static T AddConst<T>(this T receiver, string identifier, string type, GDExpression initializer)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.Const(identifier, type, initializer));
            return receiver;
        }

        public static T AddConst<T>(this T receiver, string identifier, GDExpression initializer)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.Const(identifier, initializer));
            return receiver;
        }

        public static T AddOnreadyVariable<T>(this T receiver, string name, string type, GDExpression initializer)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.OnreadyVariable(name, type, initializer));
            return receiver;
        }

        public static T AddOnreadyVariable<T>(this T receiver, string name, GDExpression initializer)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.OnreadyVariable(name, initializer));
            return receiver;
        }

        public static T AddOnreadyVariable<T>(this T receiver, string identifier, string type, GDExportDeclaration export, GDExpression initializer)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.OnreadyVariable(identifier, type, export, initializer));
            return receiver;
        }

        public static T AddOnreadyVariable<T>(this T receiver, string identifier, string type, GDExpression initializer, GDIdentifier setMethod, GDIdentifier getMethod)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.OnreadyVariable(identifier, type, initializer, setMethod, getMethod));
            return receiver;
        }

        public static T AddOnreadyVariable<T>(this T receiver, string identifier, string type, GDExportDeclaration export, GDExpression initializer, GDIdentifier setMethod, GDIdentifier getMethod)
            where T : ITokenReceiver<GDClassMember>
        {
            receiver.HandleReceivedToken(GD.Declaration.OnreadyVariable(identifier, type, export, initializer, setMethod, getMethod));
            return receiver;
        }
    }
}
