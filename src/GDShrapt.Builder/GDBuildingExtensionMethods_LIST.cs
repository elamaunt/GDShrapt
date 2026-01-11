using System;
using GDShrapt.Reader;

namespace GDShrapt.Builder
{
    public static partial class GDBuildingExtensionMethods
    {
        public static T AddStatements<T>(this T receiver, params GDStatement[] statements)
            where T : ITokenReceiver<GDStatementsList>
        {
            receiver.HandleReceivedToken(GD.List.Statements(statements));
            return receiver;
        }

        public static T AddStatements<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDStatementsList>
        {
            receiver.HandleReceivedToken(GD.List.Statements(unsafeTokens));
            return receiver;
        }

        public static T AddStatements<T>(this T receiver, GDStatementsList statementsList)
            where T : ITokenReceiver<GDStatementsList>
        {
            receiver.HandleReceivedToken(statementsList);
            return receiver;
        }

        public static T AddStatements<T>(this T receiver, Func<GDStatementsList, GDStatementsList> setup)
            where T : ITokenReceiver<GDStatementsList>
        {
            receiver.HandleReceivedToken(setup(new GDStatementsList()));
            return receiver;
        }

        public static T AddMembers<T>(this T receiver, params GDClassMember[] members)
            where T : ITokenReceiver<GDClassMembersList>
        {
            receiver.HandleReceivedToken(GD.List.Members(members));
            return receiver;
        }

        public static T AddMembers<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDClassMembersList>
        {
            receiver.HandleReceivedToken(GD.List.Members(unsafeTokens));
            return receiver;
        }

        public static T AddMembers<T>(this T receiver, GDClassMembersList list)
            where T : ITokenReceiver<GDClassMembersList>
        {
            receiver.HandleReceivedToken(list);
            return receiver;
        }

        public static T AddMembers<T>(this T receiver, Func<GDClassMembersList, GDClassMembersList> setup)
            where T : ITokenReceiver<GDClassMembersList>
        {
            receiver.HandleReceivedToken(setup(new GDClassMembersList()));
            return receiver;
        }

        public static T AddExpressions<T>(this T receiver, params GDExpression[] expressions)
           where T : ITokenReceiver<GDExpressionsList>
        {
            receiver.HandleReceivedToken(GD.List.Expressions(expressions));
            return receiver;
        }

        public static T AddExpressions<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDExpressionsList>
        {
            receiver.HandleReceivedToken(GD.List.Expressions(unsafeTokens));
            return receiver;
        }

        public static T AddExpressions<T>(this T receiver, GDExpressionsList list)
            where T : ITokenReceiver<GDExpressionsList>
        {
            receiver.HandleReceivedToken(list);
            return receiver;
        }

        public static T AddExpressions<T>(this T receiver, Func<GDExpressionsList, GDExpressionsList> setup)
            where T : ITokenReceiver<GDExpressionsList>
        {
            receiver.HandleReceivedToken(setup(new GDExpressionsList()));
            return receiver;
        }

        public static T AddKeyValues<T>(this T receiver, params GDDictionaryKeyValueDeclaration[] declarations)
           where T : ITokenReceiver<GDDictionaryKeyValueDeclarationList>
        {
            receiver.HandleReceivedToken(GD.List.KeyValues(declarations));
            return receiver;
        }

        public static T AddKeyValues<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDDictionaryKeyValueDeclarationList>
        {
            receiver.HandleReceivedToken(GD.List.KeyValues(unsafeTokens));
            return receiver;
        }

        public static T AddKeyValues<T>(this T receiver, GDDictionaryKeyValueDeclarationList list)
            where T : ITokenReceiver<GDDictionaryKeyValueDeclarationList>
        {
            receiver.HandleReceivedToken(list);
            return receiver;
        }

        public static T AddKeyValues<T>(this T receiver, Func<GDDictionaryKeyValueDeclarationList, GDDictionaryKeyValueDeclarationList> setup)
            where T : ITokenReceiver<GDDictionaryKeyValueDeclarationList>
        {
            receiver.HandleReceivedToken(setup(new GDDictionaryKeyValueDeclarationList()));
            return receiver;
        }

        public static T AddParameters<T>(this T receiver, params GDParameterDeclaration[] parameters)
           where T : ITokenReceiver<GDParametersList>
        {
            receiver.HandleReceivedToken(GD.List.Parameters(parameters));
            return receiver;
        }

        public static T AddParameters<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDParametersList>
        {
            receiver.HandleReceivedToken(GD.List.Parameters(unsafeTokens));
            return receiver;
        }

        public static T AddParameters<T>(this T receiver, GDParametersList list)
            where T : ITokenReceiver<GDParametersList>
        {
            receiver.HandleReceivedToken(list);
            return receiver;
        }

        public static T AddParameters<T>(this T receiver, Func<GDParametersList, GDParametersList> setup)
            where T : ITokenReceiver<GDParametersList>
        {
            receiver.HandleReceivedToken(setup(new GDParametersList()));
            return receiver;
        }

        public static T AddElifBranches<T>(this T receiver, params GDElifBranch[] branches)
          where T : ITokenReceiver<GDElifBranchesList>
        {
            receiver.HandleReceivedToken(GD.List.ElifBranches(branches));
            return receiver;
        }

        public static T AddElifBranches<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDElifBranchesList>
        {
            receiver.HandleReceivedToken(GD.List.ElifBranches(unsafeTokens));
            return receiver;
        }

        public static T AddElifBranches<T>(this T receiver, GDElifBranchesList list)
            where T : ITokenReceiver<GDElifBranchesList>
        {
            receiver.HandleReceivedToken(list);
            return receiver;
        }

        public static T AddElifBranches<T>(this T receiver, Func<GDElifBranchesList, GDElifBranchesList> setup)
            where T : ITokenReceiver<GDElifBranchesList>
        {
            receiver.HandleReceivedToken(setup(new GDElifBranchesList()));
            return receiver;
        }

        public static T AddEnumValues<T>(this T receiver, params GDEnumValueDeclaration[] values)
          where T : ITokenReceiver<GDEnumValuesList>
        {
            receiver.HandleReceivedToken(GD.List.EnumValues(values));
            return receiver;
        }

        public static T AddEnumValues<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDEnumValuesList>
        {
            receiver.HandleReceivedToken(GD.List.EnumValues(unsafeTokens));
            return receiver;
        }

        public static T AddEnumValues<T>(this T receiver, GDEnumValuesList list)
            where T : ITokenReceiver<GDEnumValuesList>
        {
            receiver.HandleReceivedToken(list);
            return receiver;
        }

        public static T AddEnumValues<T>(this T receiver, Func<GDEnumValuesList, GDEnumValuesList> setup)
            where T : ITokenReceiver<GDEnumValuesList>
        {
            receiver.HandleReceivedToken(setup(new GDEnumValuesList()));
            return receiver;
        }

        public static T AddPath<T>(this T receiver, params GDIdentifier[] names)
          where T : ITokenReceiver<GDPathList>
        {
            receiver.HandleReceivedToken(GD.List.Path(names));
            return receiver;
        }

        public static T AddPath<T>(this T receiver, params GDSyntaxToken[] unsafeTokens)
            where T : ITokenReceiver<GDPathList>
        {
            receiver.HandleReceivedToken(GD.List.Path(unsafeTokens));
            return receiver;
        }

        public static T AddPath<T>(this T receiver, GDPathList list)
            where T : ITokenReceiver<GDPathList>
        {
            receiver.HandleReceivedToken(list);
            return receiver;
        }

        public static T AddPath<T>(this T receiver, Func<GDPathList, GDPathList> setup)
            where T : ITokenReceiver<GDPathList>
        {
            receiver.HandleReceivedToken(setup(new GDPathList()));
            return receiver;
        }
    }
}
