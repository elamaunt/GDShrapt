namespace GDShrapt.Reader
{
    public static partial class GD
    {
        public static class List
        {
            public static GDStatementsList Statements(params GDStatement[] statements)
            {
                if (statements == null || statements.Length == 0)
                    return null;

                var list = new GDStatementsList();

                for (int i = 0; i < statements.Length; i++)
                {
                    list.Form.Add(new GDNewLine());
                    list.Form.Add(Syntax.Intendation());
                    list.Add(statements[i]);
                }

                return list;
            }

            public static GDExpressionsList Expressions(params GDExpression[] expressions)
            {
                if (expressions == null || expressions.Length == 0)
                    return null;

                var list = new GDExpressionsList();

                for (int i = 0; i < expressions.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.Add(new GDComma());
                        list.Form.Add(Syntax.Space());
                    }

                    list.Add(expressions[i]);
                }

                return list;
            }

            public static GDDictionaryKeyValueDeclarationList KeyValues(params GDDictionaryKeyValueDeclaration[] keyValues)
            {
                if (keyValues == null || keyValues.Length == 0)
                    return null;

                var list = new GDDictionaryKeyValueDeclarationList();

                for (int i = 0; i < keyValues.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.Add(new GDComma());
                        list.Form.Add(Syntax.Space());
                    }

                    list.Add(keyValues[i]);
                }

                return list;
            }

            public static GDParametersList Parameters(params GDParameterDeclaration[] parameters)
            {
                if (parameters == null || parameters.Length == 0)
                    return null;

                var list = new GDParametersList();

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.Add(new GDComma());
                        list.Form.Add(Syntax.Space());
                    }

                    list.Add(parameters[i]);
                }

                return list;
            }

            public static GDElifBranchesList ElifBranches(params GDElifBranch[] branches)
            {
                if (branches == null || branches.Length == 0)
                    return null;

                var list = new GDElifBranchesList();

                for (int i = 0; i < branches.Length; i++)
                {
                    list.Form.Add(new GDNewLine());
                    list.Form.Add(Syntax.Intendation());
                    list.Add(branches[i]);
                }

                return list;
            }

            public static GDClassMembersList Members(params GDClassMember[] members)
            {
                if (members == null || members.Length == 0)
                    return null;

                var list = new GDClassMembersList();

                for (int i = 0; i < members.Length; i++)
                {
                    list.Form.Add(new GDNewLine());
                    list.Form.Add(Syntax.Intendation());
                    list.Add(members[i]);
                }

                return list;
            }

            public static GDClassAtributesList Atributes(params GDClassAtribute[] atributes)
            {
                if (atributes == null || atributes.Length == 0)
                    return null;

                var list = new GDClassAtributesList();

                for (int i = 0; i < atributes.Length; i++)
                {
                    list.Form.Add(new GDNewLine());
                    list.Form.Add(Syntax.Intendation());
                    list.Add(atributes[i]);
                }

                return list;
            }

            public static GDEnumValuesList EnumValues(params GDEnumValueDeclaration[] values)
            {
                if (values == null || values.Length == 0)
                    return null;

                var list = new GDEnumValuesList();

                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.Add(new GDComma());
                        list.Form.Add(Syntax.Space());
                    }

                    list.Add(values[i]);
                }

                return list;
            }

            public static GDPathList Path(params GDIdentifier[] identifiers)
            {
                 if (identifiers == null || identifiers.Length == 0)
                    return null;

                var list = new GDPathList();

                for (int i = 0; i < identifiers.Length; i++)
                {
                    if (i > 0)
                        list.Form.Add(new GDRightSlash());

                    list.Add(identifiers[i]);
                }

                return list;
            }

            public static GDExportParametersList ExportParameters(params GDDataToken[] tokens)
            {
                if (tokens == null || tokens.Length == 0)
                    return null;

                var list = new GDExportParametersList();

                for (int i = 0; i < tokens.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.Add(new GDComma());
                        list.Form.Add(Syntax.Space());
                    }

                    list.Add(tokens[i]);
                }

                return list;
            }
        }
    }
}
