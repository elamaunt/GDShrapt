using System;
using GDShrapt.Reader;

namespace GDShrapt.Builder
{
    public static partial class GDBuildingExtensionMethods
    {
        public static T AddToolAttribute<T>(this T receiver)
           where T : ITokenReceiver<GDToolAttribute>
        {
            receiver.HandleReceivedToken(GD.Attribute.Tool());
            return receiver;
        }

        public static T AddClassNameAttribute<T>(this T receiver, string name)
            where T : ITokenReceiver<GDClassNameAttribute>
        {
            receiver.HandleReceivedToken(GD.Attribute.ClassName(name));
            return receiver;
        }

        public static T AddClassNameAttribute<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDClassNameAttribute>
        {
            receiver.HandleReceivedToken(GD.Attribute.ClassName(unsafeTokens));
            return receiver;
        }

        public static T AddExtendsAttribute<T>(this T receiver, string baseTypeName)
            where T : ITokenReceiver<GDExtendsAttribute>
        {
            receiver.HandleReceivedToken(GD.Attribute.Extends(baseTypeName));
            return receiver;
        }

        public static T AddExtendsAttribute<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDExtendsAttribute>
        {
            receiver.HandleReceivedToken(GD.Attribute.Extends(unsafeTokens));
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

        public static T AddVariable<T>(this T receiver, string name, GDTypeNode type)
            where T : ITokenReceiver<GDVariableDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(name, type));
            return receiver;
        }

        public static T AddVariable<T>(this T receiver, string name, GDTypeNode type, GDExpression initializer)
            where T : ITokenReceiver<GDVariableDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Variable(name, type, initializer));
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

        public static T AddConst<T>(this T receiver, string identifier, GDTypeNode type, GDExpression initializer)
            where T : ITokenReceiver<GDVariableDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Const(identifier, type, initializer));
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
            receiver.HandleReceivedToken(GD.Attribute.Abstract());
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

        // Signal declaration extension methods
        public static T AddSignal<T>(this T receiver, string name)
            where T : ITokenReceiver<GDSignalDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Signal(name));
            return receiver;
        }

        public static T AddSignal<T>(this T receiver, string name, GDParametersList parameters)
            where T : ITokenReceiver<GDSignalDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Signal(GD.Syntax.Identifier(name), parameters));
            return receiver;
        }

        public static T AddSignal<T>(this T receiver, string name, params GDParameterDeclaration[] parameters)
            where T : ITokenReceiver<GDSignalDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.Signal(name, parameters));
            return receiver;
        }

        public static T AddSignal<T>(this T receiver, GDSignalDeclaration signal)
            where T : ITokenReceiver<GDSignalDeclaration>
        {
            receiver.HandleReceivedToken(signal);
            return receiver;
        }

        // Enum declaration extension method
        public static T AddEnum<T>(this T receiver, GDEnumDeclaration enumDecl)
            where T : ITokenReceiver<GDEnumDeclaration>
        {
            receiver.HandleReceivedToken(enumDecl);
            return receiver;
        }

        // Inner class extension methods
        public static T AddInnerClass<T>(this T receiver, string name)
            where T : ITokenReceiver<GDInnerClassDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.InnerClass(name));
            return receiver;
        }

        public static T AddInnerClass<T>(this T receiver, string name, GDClassMembersList members)
            where T : ITokenReceiver<GDInnerClassDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.InnerClass(name, members));
            return receiver;
        }

        public static T AddInnerClass<T>(this T receiver, string name, params GDClassMember[] members)
            where T : ITokenReceiver<GDInnerClassDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.InnerClass(name, members));
            return receiver;
        }

        public static T AddInnerClass<T>(this T receiver, Func<GDInnerClassDeclaration, GDInnerClassDeclaration> setup)
            where T : ITokenReceiver<GDInnerClassDeclaration>
        {
            receiver.HandleReceivedToken(setup(GD.Declaration.InnerClass()));
            return receiver;
        }

        // Property type annotation extension methods
        public static T AddTypeAnnotation<T>(this T receiver, GDTypeNode type)
            where T : ITokenOrSkipReceiver<GDTypeNode>
        {
            receiver.HandleReceivedToken(type);
            return receiver;
        }

        public static T AddTypeAnnotation<T>(this T receiver, string typeName)
            where T : ITokenOrSkipReceiver<GDTypeNode>
        {
            receiver.HandleReceivedToken(GD.Type.Single(typeName));
            return receiver;
        }

        // Property accessor extension methods
        public static T AddAccessor<T>(this T receiver, GDAccessorDeclaration accessor)
            where T : IIntendedTokenOrSkipReceiver<GDAccessorDeclaration>
        {
            receiver.HandleReceivedToken(accessor);
            return receiver;
        }

        public static T AddGetAccessor<T>(this T receiver, GDExpression expression)
            where T : IIntendedTokenOrSkipReceiver<GDAccessorDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.GetAccessorBody(expression));
            return receiver;
        }

        public static T AddGetAccessor<T>(this T receiver, params GDStatement[] statements)
            where T : IIntendedTokenOrSkipReceiver<GDAccessorDeclaration>
        {
            receiver.HandleReceivedToken(GD.Declaration.GetAccessorBody(statements));
            return receiver;
        }

        public static T AddSetAccessor<T>(this T receiver, string paramName, params GDStatement[] statements)
            where T : IIntendedTokenOrSkipReceiver<GDAccessorDeclaration>
        {
            // Skip comma if needed (for second accessor in property)
            if (receiver is ITokenSkipReceiver<GDComma> commaSkipper)
            {
                commaSkipper.HandleReceivedTokenSkip();
            }
            receiver.HandleReceivedToken(GD.Declaration.SetAccessorBody(paramName, statements));
            return receiver;
        }

        public static T AddSetAccessor<T>(this T receiver, string paramName, GDExpression expression)
            where T : IIntendedTokenOrSkipReceiver<GDAccessorDeclaration>
        {
            // Skip comma if needed (for second accessor in property)
            if (receiver is ITokenSkipReceiver<GDComma> commaSkipper)
            {
                commaSkipper.HandleReceivedTokenSkip();
            }
            receiver.HandleReceivedToken(GD.Declaration.SetAccessorBody(paramName, expression));
            return receiver;
        }
    }
}
