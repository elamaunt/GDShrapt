using System;
using GDShrapt.Reader;

namespace GDShrapt.Builder
{
    public static partial class GD
    {
        public static class List
        {
            public static GDStatementsList Statements() => new GDStatementsList();
            public static GDStatementsList Statements(Func<GDStatementsList, GDStatementsList> setup) => setup(new GDStatementsList());
            public static GDStatementsList Statements(params GDSyntaxToken[] unsafeTokens) => new GDStatementsList() { FormTokensSetter = unsafeTokens };
            public static GDStatementsList Statements(params GDStatement[] statements)
            {
                if (statements == null || statements.Length == 0)
                    return new GDStatementsList();

                var list = new GDStatementsList();

                for (int i = 0; i < statements.Length; i++)
                {
                    list.Form.AddToEnd(new GDNewLine());
                    list.Form.AddToEnd(Syntax.Intendation());
                    list.Add(statements[i]);
                }

                return list;
            }

            public static GDExpressionsList Expressions() => new GDExpressionsList();
            public static GDExpressionsList Expressions(Func<GDExpressionsList, GDExpressionsList> setup) => setup(new GDExpressionsList());
            public static GDExpressionsList Expressions(params GDSyntaxToken[] unsafeTokens) => new GDExpressionsList() { FormTokensSetter = unsafeTokens };
            public static GDExpressionsList Expressions(params GDExpression[] expressions)
            {
                if (expressions == null || expressions.Length == 0)
                    return new GDExpressionsList();

                var list = new GDExpressionsList();

                for (int i = 0; i < expressions.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.AddToEnd(new GDComma());
                        list.Form.AddToEnd(Syntax.Space());
                    }

                    list.Add(expressions[i]);
                }

                return list;
            }

            public static GDDictionaryKeyValueDeclarationList KeyValues() => new GDDictionaryKeyValueDeclarationList();
            public static GDDictionaryKeyValueDeclarationList KeyValues(Func<GDDictionaryKeyValueDeclarationList, GDDictionaryKeyValueDeclarationList> setup) => setup(new GDDictionaryKeyValueDeclarationList());
            public static GDDictionaryKeyValueDeclarationList KeyValues(params GDSyntaxToken[] unsafeTokens) => new GDDictionaryKeyValueDeclarationList() { FormTokensSetter = unsafeTokens };
            public static GDDictionaryKeyValueDeclarationList KeyValues(params GDDictionaryKeyValueDeclaration[] keyValues)
            {
                if (keyValues == null || keyValues.Length == 0)
                    return new GDDictionaryKeyValueDeclarationList();

                var list = new GDDictionaryKeyValueDeclarationList();

                for (int i = 0; i < keyValues.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.AddToEnd(new GDComma());
                        list.Form.AddToEnd(Syntax.Space());
                    }

                    list.Add(keyValues[i]);
                }

                return list;
            }

            public static GDParametersList Parameters() => new GDParametersList();
            public static GDParametersList Parameters(Func<GDParametersList, GDParametersList> setup) => setup(new GDParametersList());
            public static GDParametersList Parameters(params GDSyntaxToken[] unsafeTokens) => new GDParametersList() { FormTokensSetter = unsafeTokens };
            public static GDParametersList Parameters(params GDParameterDeclaration[] parameters)
            {
                if (parameters == null || parameters.Length == 0)
                    return new GDParametersList();

                var list = new GDParametersList();

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.AddToEnd(new GDComma());
                        list.Form.AddToEnd(Syntax.Space());
                    }

                    list.Add(parameters[i]);
                }

                return list;
            }

            public static GDElifBranchesList ElifBranches() => new GDElifBranchesList();
            public static GDElifBranchesList ElifBranches(Func<GDElifBranchesList, GDElifBranchesList> setup) => setup(new GDElifBranchesList());
            public static GDElifBranchesList ElifBranches(params GDSyntaxToken[] unsafeTokens) => new GDElifBranchesList() { FormTokensSetter = unsafeTokens };
            public static GDElifBranchesList ElifBranches(params GDElifBranch[] branches)
            {
                if (branches == null || branches.Length == 0)
                    return new GDElifBranchesList();

                var list = new GDElifBranchesList();

                for (int i = 0; i < branches.Length; i++)
                {
                    list.Form.AddToEnd(new GDNewLine());
                    list.Form.AddToEnd(Syntax.Intendation());
                    list.Add(branches[i]);
                }

                return list;
            }

            public static GDClassMembersList Members() => new GDClassMembersList();
            public static GDClassMembersList Members(Func<GDClassMembersList, GDClassMembersList> setup) => setup(new GDClassMembersList());
            public static GDClassMembersList Members(params GDSyntaxToken[] unsafeTokens) => new GDClassMembersList() { FormTokensSetter = unsafeTokens };
            public static GDClassMembersList Members(params GDClassMember[] members)
            {
                if (members == null || members.Length == 0)
                    return new GDClassMembersList();

                var list = new GDClassMembersList();

                bool previousMemberIsAttribute = false;
                bool currentMemberIsAttribute = false;
                bool isNotAttributeMet = false;

                for (int i = 0; i < members.Length; i++)
                {
                    var member = members[i];

                    currentMemberIsAttribute = member is GDClassAttribute;
                    isNotAttributeMet = isNotAttributeMet || !(member is GDClassAttribute);

                    if (!previousMemberIsAttribute)
                    {
                        if (i > 0)
                        {
                            list.Form.AddToEnd(new GDNewLine());
                            list.Form.AddToEnd(new GDNewLine());
                        }
                    }
                    else
                    {
                        list.Form.AddToEnd(new GDNewLine());
                    }

                    list.Form.AddToEnd(Syntax.Intendation());
                    previousMemberIsAttribute = currentMemberIsAttribute;
                    list.Add(member);
                }

                return list;
            }


            public static GDEnumValuesList EnumValues() => new GDEnumValuesList();
            public static GDEnumValuesList EnumValues(Func<GDEnumValuesList, GDEnumValuesList> setup) => setup(new GDEnumValuesList());
            public static GDEnumValuesList EnumValues(params GDSyntaxToken[] unsafeTokens) => new GDEnumValuesList() { FormTokensSetter = unsafeTokens };
            public static GDEnumValuesList EnumValues(params GDEnumValueDeclaration[] values)
            {
                if (values == null || values.Length == 0)
                    return new GDEnumValuesList();

                var list = new GDEnumValuesList();

                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.AddToEnd(new GDComma());
                        list.Form.AddToEnd(Syntax.Space());
                    }

                    list.Add(values[i]);
                }

                return list;
            }


            public static GDPathList Path() => new GDPathList();
            public static GDPathList Path(Func<GDPathList, GDPathList> setup) => setup(new GDPathList());
            public static GDPathList Path(params GDSyntaxToken[] unsafeTokens) => new GDPathList() { FormTokensSetter = unsafeTokens };
            public static GDPathList Path(params GDLayersList[] identifiers)
            {
                if (identifiers == null || identifiers.Length == 0)
                    return new GDPathList();

                var list = new GDPathList();

                for (int i = 0; i < identifiers.Length; i++)
                {
                    if (i > 0)
                        list.Form.AddToEnd(new GDRightSlash());

                    list.Add(identifiers[i]);
                }

                return list;
            }

            public static GDLayersList LayersList() => new GDLayersList(true);
            public static GDLayersList LayersList(Func<GDLayersList, GDLayersList> setup) => setup(new GDLayersList(true));
            public static GDLayersList LayersList(params GDSyntaxToken[] unsafeTokens) => new GDLayersList(true) { FormTokensSetter = unsafeTokens };
            public static GDLayersList LayersList(params GDPathSpecifier[] pathSpecifiers)
            {
                if (pathSpecifiers == null || pathSpecifiers.Length == 0)
                    return new GDLayersList(true);

                var list = new GDLayersList(true);

                for (int i = 0; i < pathSpecifiers.Length; i++)
                {
                    if (i > 0)
                        list.Form.AddToEnd(new GDColon());

                    list.Add(pathSpecifiers[i]);
                }

                return list;
            }

            public static GDMatchCasesList MatchCases() => new GDMatchCasesList();
            public static GDMatchCasesList MatchCases(Func<GDMatchCasesList, GDMatchCasesList> setup) => setup(new GDMatchCasesList());
            public static GDMatchCasesList MatchCases(params GDSyntaxToken[] unsafeTokens) => new GDMatchCasesList() { FormTokensSetter = unsafeTokens };
            public static GDMatchCasesList MatchCases(params GDMatchCaseDeclaration[] tokens)
            {
                if (tokens == null || tokens.Length == 0)
                    return new GDMatchCasesList();

                var list = new GDMatchCasesList();

                for (int i = 0; i < tokens.Length; i++)
                {
                    list.Form.AddToEnd(new GDNewLine());
                    list.Form.AddToEnd(Syntax.Intendation());
                    list.Add(tokens[i]);
                }

                return list;
            }
        }
    }
}
