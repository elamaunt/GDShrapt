using System;

namespace GDShrapt.Reader
{
    public static partial class GDBuildingExtensionMethods
    {
        public static T AddToolAtribute<T>(this T receiver)
           where T : ITokenReceiver<GDToolAttribute>
        {
            receiver.HandleReceivedToken(GD.Atribute.Tool());
            return receiver;
        }

        public static T AddClassNameAtribute<T>(this T receiver, string name)
            where T : ITokenReceiver<GDClassNameAttribute>
        {
            receiver.HandleReceivedToken(GD.Atribute.ClassName(name));
            return receiver;
        }

        public static T AddClassNameAtribute<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDClassNameAttribute>
        {
            receiver.HandleReceivedToken(GD.Atribute.ClassName(unsafeTokens));
            return receiver;
        }

        public static T AddExtendsAtribute<T>(this T receiver, string baseTypeName)
            where T : ITokenReceiver<GDExtendsAttribute>
        {
            receiver.HandleReceivedToken(GD.Atribute.Extends(baseTypeName));
            return receiver;
        }

        public static T AddExtendsAtribute<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDExtendsAttribute>
        {
            receiver.HandleReceivedToken(GD.Atribute.Extends(unsafeTokens));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, Func<GDVariableDeclaration, GDVariableDeclaration> setup)
            where T : ITokenReceiver<GDVariableDeclaration>
        {
            receiver.HandleReceivedToken(setup(new GDVariableDeclaration()));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDVariableDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(unsafeTokens));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, string name)
            where T : ITokenReceiver<GDVariableDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(name));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, string name, string type)
            where T : ITokenReceiver<GDVariableDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(name, type));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, string name, GDExpression initializer)
            where T : ITokenReceiver<GDVariableDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(name, initializer));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, string identifier, string type, GDExpression initializer)
            where T : ITokenReceiver<GDVariableDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(identifier, type, initializer));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, string identifier, string type, GDExpression initializer, GDIdentifier setMethod, GDIdentifier getMethod)
            where T : ITokenReceiver<GDVariableDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(identifier, type, initializer, setMethod, getMethod));
            return receiver;
        }

        public static T AddConst<T>(this T receiver, string identifier, string type, GDExpression initializer)
            where T : ITokenReceiver<GDVariableDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Const(identifier, type, initializer));
            return receiver;
        }

        public static T AddConst<T>(this T receiver, string identifier, GDExpression initializer)
            where T : ITokenReceiver<GDVariableDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Const(identifier, initializer));
            return receiver;
        }

        public static T AddMethod<T>(this T receiver, Func<GDMethodDeclaration, GDMethodDeclaration> setup)
            where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(setup(new GDMethodDeclaration()));
            return receiver;
        }

        public static T AddMethod<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Method(unsafeTokens));
            return receiver;
        }

        public static T AddMethod<T>(this T receiver, string name, params GDStatement[] statements)
            where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Method(name, statements));
            return receiver;
        }

        public static T AddMethod<T>(this T receiver, string name, string type, GDExpression expression, params GDStatement[] statements)
           where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Method(name, GD.ParseTypeNode(type), expression, statements));
            return receiver;
        }

        public static T AddMethod<T>(this T receiver, string name, string type, params GDStatement[] statements)
           where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Method(name, GD.ParseTypeNode(type), statements));
            return receiver;
        }

        public static T AddMethod<T>(this T receiver, string name, string type, GDExpression[] baseCallParameters, params GDStatement[] statements)
           where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Method(name, GD.ParseTypeNode(type), baseCallParameters, statements));
            return receiver;
        }

        public static T AddMethod<T>(this T receiver, string name, GDExpression[] baseCallParameters, params GDStatement[] statements)
           where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Method(name, baseCallParameters, statements));
            return receiver;
        }

        public static T AddAbstract<T>(this T receiver)
            where T : ITokenReceiver<GDCustomAttribute>
        {
            receiver.HandleReceivedToken(GD.Atribute.Abstract());
            return receiver;
        }

        public static T AddAbstractMethod<T>(this T receiver, string name)
            where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.AbstractMethod(name));
            return receiver;
        }

        public static T AddAbstractMethod<T>(this T receiver, string name, GDParametersList parameters)
            where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.AbstractMethod(name, parameters));
            return receiver;
        }

        public static T AddAbstractMethod<T>(this T receiver, string name, GDTypeNode returnType)
            where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.AbstractMethod(name, returnType));
            return receiver;
        }

        public static T AddAbstractMethod<T>(this T receiver, string name, GDParametersList parameters, GDTypeNode returnType)
            where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.AbstractMethod(name, parameters, returnType));
            return receiver;
        }

        public static T AddAbstractMethod<T>(this T receiver, string name, string returnType)
            where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.AbstractMethod(name, GD.ParseTypeNode(returnType)));
            return receiver;
        }

        public static T AddAbstractMethod<T>(this T receiver, string name, GDParametersList parameters, string returnType)
            where T : ITokenReceiver<GDMethodDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.AbstractMethod(name, parameters, GD.ParseTypeNode(returnType)));
            return receiver;
        }
    }
}
