using System;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public static class GDBuildingExtensionMethods
    {
        public static T AddExpression<T>(this T receiver, GDExpression token)
           where T : ITokenReceiver<GDExpression>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T AddStatement<T>(this T receiver, GDStatement token)
            where T : ITokenReceiver<GDStatement>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T AddSpace<T>(this T receiver, int count = 1)
            where T : ITokenReceiver
        {
            receiver.HandleReceivedToken(GD.Syntax.Space());
            return receiver;
        }

        public static T AddNewLine<T>(this T receiver)
            where T : INewLineReceiver
        {
            receiver.HandleReceivedToken(GD.Syntax.NewLine());
            return receiver;
        }

        public static T AddComment<T>(this T receiver, string comment)
            where T : ITokenReceiver
        {
            receiver.HandleReceivedToken(GD.Syntax.Comment(comment));
            return receiver;
        }

        public static T AddIntendation<T>(this T receiver, int count = 0)
            where T : IIntendedTokenReceiver
        {
            receiver.HandleReceivedToken(GD.Syntax.Intendation(count));
            return receiver;
        }

        public static T Add<T, B>(this T receiver, B token)
            where T : ITokenReceiver<B>
            where B : GDSyntaxToken
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T Add<T>(this T receiver, GDSingleOperator token)
            where T : ITokenReceiver<GDSingleOperator>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T Add<T>(this T receiver, GDSingleOperatorType type)
           where T : ITokenReceiver<GDSingleOperator>
        {
            receiver.HandleReceivedToken(GD.Syntax.SingleOperator(type));
            return receiver;
        }


        public static T Add<T>(this T receiver, GDDualOperator token)
            where T : ITokenReceiver<GDDualOperator>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T Add<T>(this T receiver, GDDualOperatorType type)
            where T : ITokenReceiver<GDDualOperator>
        {
            receiver.HandleReceivedToken(GD.Syntax.DualOperator(type));
            return receiver;
        }
        

        public static T Add<T>(this T receiver, long value)
            where T : ITokenReceiver<GDNumber>
        {
            receiver.HandleReceivedToken(GD.Syntax.Number(value));
            return receiver;
        }

        public static T Add<T>(this T receiver, int value)
           where T : ITokenReceiver<GDNumber>
        {
            receiver.HandleReceivedToken(GD.Syntax.Number(value));
            return receiver;
        }

        public static T Add<T>(this T receiver, double value)
            where T : ITokenReceiver<GDNumber>
        {
            receiver.HandleReceivedToken(GD.Syntax.Number(value));
            return receiver;
        }

        public static T Add<T>(this T receiver, string value, bool multiline = false, GDStringBoundingChar boundingChar = GDStringBoundingChar.DoubleQuotas)
            where T : ITokenReceiver<GDString>
        {
            receiver.HandleReceivedToken(GD.Syntax.String(value, multiline, boundingChar));
            return receiver;
        }

        public static T Add<T>(this T receiver, GDClassAtribute atribute)
            where T : ITokenReceiver<GDClassAtribute>
        {
            receiver.HandleReceivedToken(atribute);
            return receiver;
        }

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
    }
}
