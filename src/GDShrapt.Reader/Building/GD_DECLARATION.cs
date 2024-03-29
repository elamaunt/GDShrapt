﻿using System;

namespace GDShrapt.Reader
{
    /// <summary>
    /// GDScript code generation helper
    /// </summary>
    public static partial class GD
    {
        public static class Declaration
        {
            public static GDClassDeclaration Class() => new GDClassDeclaration();
            public static GDClassDeclaration Class(Func<GDClassDeclaration, GDClassDeclaration> setup) => setup(new GDClassDeclaration());
            public static GDClassDeclaration Class(params GDSyntaxToken[] unsafeTokens) => new GDClassDeclaration() { FormTokensSetter = unsafeTokens };
            public static GDClassDeclaration Class(GDClassMembersList members) => new GDClassDeclaration()
            {
                Members = members
            };

            public static GDInnerClassDeclaration InnerClass() => new GDInnerClassDeclaration();
            public static GDInnerClassDeclaration InnerClass(Func<GDInnerClassDeclaration, GDInnerClassDeclaration> setup) => setup(new GDInnerClassDeclaration());
            public static GDInnerClassDeclaration InnerClass(params GDSyntaxToken[] unsafeTokens) => new GDInnerClassDeclaration() { FormTokensSetter = unsafeTokens };
            public static GDInnerClassDeclaration InnerClass(string name) => new GDInnerClassDeclaration()
            {
                ClassKeyword = new GDClassKeyword(),
                [1] = Syntax.Space(),
                Identifier = Syntax.Identifier(name)
            };

            public static GDInnerClassDeclaration InnerClass(string name, GDClassMembersList members) => new GDInnerClassDeclaration()
            {
                ClassKeyword = new GDClassKeyword(),
                [1] = Syntax.Space(),
                Identifier = Syntax.Identifier(name),
                Colon = new GDColon(),
                [3] = new GDNewLine(),
                Members = members
            };

            public static GDInnerClassDeclaration InnerClass(GDIdentifier identifier, GDClassMembersList members) => new GDInnerClassDeclaration()
            {
                ClassKeyword = new GDClassKeyword(),
                [1] = Syntax.Space(),
                Identifier = identifier,
                Colon = new GDColon(),
                [3] = new GDNewLine(),
                Members = members
            };

            public static GDClassDeclaration Class(params GDClassMember[] members) => new GDClassDeclaration()
            {
                Members = List.Members(members)
            };

            public static GDInnerClassDeclaration InnerClass(string name, params GDClassMember[] members) => new GDInnerClassDeclaration()
            {
                ClassKeyword = new GDClassKeyword(),
                [1] = Syntax.Space(),
                Identifier = Syntax.Identifier(name),
                Colon = new GDColon(),
                [3] = new GDNewLine(),
                Members = List.Members(members)
            };

            public static GDInnerClassDeclaration InnerClass(GDIdentifier identifier, params GDClassMember[] members) => new GDInnerClassDeclaration()
            {
                ClassKeyword = new GDClassKeyword(),
                [1] = Syntax.Space(),
                Identifier = identifier,
                Colon = new GDColon(),
                [3] = new GDNewLine(),
                Members = List.Members(members)
            };

            public static GDMethodDeclaration Method() => new GDMethodDeclaration();
            public static GDMethodDeclaration Method(Func<GDMethodDeclaration, GDMethodDeclaration> setup) => setup(new GDMethodDeclaration());
            public static GDMethodDeclaration Method(params GDSyntaxToken[] unsafeTokens) => new GDMethodDeclaration() { FormTokensSetter = unsafeTokens };

            public static GDMethodDeclaration Method(GDIdentifier identifier, GDParametersList parameters, GDExpression expression, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                Colon = new GDColon(),
                OpenBracket = new GDOpenBracket(),
                Parameters = parameters,
                CloseBracket = new GDCloseBracket(),
                Expression = expression,
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration Method(GDIdentifier identifier, GDParametersList parameters, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                Colon = new GDColon(),
                OpenBracket = new GDOpenBracket(),
                Parameters = parameters,
                CloseBracket = new GDCloseBracket(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration Method(GDIdentifier identifier, params GDStatement[] statements) => new GDMethodDeclaration( )
            { 
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration Method(GDIdentifier identifier, GDTypeNode returnType, GDExpression expression, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                [10] = Syntax.Space(),
                ReturnTypeKeyword = new GDReturnTypeKeyword(),
                [11] = Syntax.Space(),
                ReturnType = returnType,
                Colon = new GDColon(),
                Expression = expression,
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration Method(GDIdentifier identifier, GDTypeNode returnType, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                [10] = Syntax.Space(),
                ReturnTypeKeyword = new GDReturnTypeKeyword(),
                [11] = Syntax.Space(),
                ReturnType = returnType,
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration Method(GDIdentifier identifier, GDTypeNode returnType, GDExpression[] baseCallParameters, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                BaseCallPoint = new GDPoint(),
                BaseCallOpenBracket = new GDOpenBracket(),
                BaseCallParameters = List.Expressions(baseCallParameters),
                BaseCallCloseBracket =new GDCloseBracket(),
                [10] = Syntax.Space(),
                ReturnTypeKeyword = new GDReturnTypeKeyword(),
                [11] = Syntax.Space(),
                ReturnType = returnType,
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration Method(GDIdentifier identifier, GDExpression[] baseCallParameters, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                BaseCallPoint = new GDPoint(),
                BaseCallOpenBracket = new GDOpenBracket(),
                BaseCallParameters = List.Expressions(baseCallParameters),
                BaseCallCloseBracket = new GDCloseBracket(),
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration StaticMethod(GDIdentifier identifier, GDExpression expression, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                StaticKeyword = new GDStaticKeyword(),
                [1] = Syntax.Space(),
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                Colon = new GDColon(),
                Expression = expression,
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration StaticMethod(GDIdentifier identifier, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                StaticKeyword = new GDStaticKeyword(),
                [1] = Syntax.Space(),
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration StaticMethod(GDIdentifier identifier, GDParametersList parameters, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                StaticKeyword = new GDStaticKeyword(),
                [1] = Syntax.Space(),
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                Colon = new GDColon(),
                OpenBracket = new GDOpenBracket(),
                Parameters = parameters,
                CloseBracket = new GDCloseBracket(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration StaticMethod(GDIdentifier identifier, GDTypeNode returnType, GDExpression expression, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                StaticKeyword = new GDStaticKeyword(),
                [1] = Syntax.Space(),
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                [10] = Syntax.Space(),
                ReturnTypeKeyword = new GDReturnTypeKeyword(),
                [11] = Syntax.Space(),
                ReturnType = returnType,
                Colon = new GDColon(),
                Expression = expression,
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration StaticMethod(GDIdentifier identifier, GDTypeNode returnType, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                StaticKeyword = new GDStaticKeyword(),
                [1] = Syntax.Space(),
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                [10] = Syntax.Space(),
                ReturnTypeKeyword = new GDReturnTypeKeyword(),
                [11] = Syntax.Space(),
                ReturnType = returnType,
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration StaticMethod(GDIdentifier identifier, GDTypeNode returnType, GDExpression[] baseCallParameters, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                StaticKeyword = new GDStaticKeyword(),
                [1] = Syntax.Space(),
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                BaseCallPoint = new GDPoint(),
                BaseCallOpenBracket = new GDOpenBracket(),
                BaseCallParameters = List.Expressions(baseCallParameters),
                BaseCallCloseBracket = new GDCloseBracket(),
                [10] = Syntax.Space(),
                ReturnTypeKeyword = new GDReturnTypeKeyword(),
                [11] = Syntax.Space(),
                ReturnType = returnType,
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDParameterDeclaration Parameter() => new GDParameterDeclaration();
            public static GDParameterDeclaration Parameter(Func<GDParameterDeclaration, GDParameterDeclaration> setup) => setup(new GDParameterDeclaration());
            public static GDParameterDeclaration Parameter(params GDSyntaxToken[] unsafeTokens) => new GDParameterDeclaration() { FormTokensSetter = unsafeTokens };
            public static GDParameterDeclaration Parameter(string identifier) => new GDParameterDeclaration()
            {
                Identifier = Syntax.Identifier(identifier)
            };

            public static GDParameterDeclaration Parameter(string identifier, GDTypeNode type, GDExpression defaultValue) => new GDParameterDeclaration()
            {
                Identifier = Syntax.Identifier(identifier),
                [1] = Syntax.Space(),
                Colon = new GDColon(),
                [2] = Syntax.Space(),
                Type = type,
                [3] = Syntax.Space(),
                Assign = new GDAssign(),
                [4] = Syntax.Space(),
                DefaultValue = defaultValue
            };

            public static GDParameterDeclaration Parameter(string identifier, GDExpression defaultValue) => new GDParameterDeclaration()
            {
                Identifier = Syntax.Identifier(identifier),
                [3] = Syntax.Space(),
                Assign = new GDAssign(),
                [4] = Syntax.Space(),
                DefaultValue = defaultValue
            };

            public static GDParameterDeclaration Parameter(GDIdentifier identifier) => new GDParameterDeclaration()
            { 
                Identifier = identifier
            };

            public static GDParameterDeclaration Parameter(GDIdentifier identifier, GDTypeNode type, GDExpression defaultValue) => new GDParameterDeclaration()
            {
                Identifier = identifier,
                [1] = Syntax.Space(),
                Colon = new GDColon(),
                [2] = Syntax.Space(),
                Type = type,
                [3] = Syntax.Space(),
                Assign = new GDAssign(),
                [4] = Syntax.Space(),
                DefaultValue = defaultValue
            };

            public static GDParameterDeclaration Parameter(GDIdentifier identifier, GDExpression defaultValue) => new GDParameterDeclaration()
            {
                Identifier = identifier,
                [3] = Syntax.Space(),
                Assign = new GDAssign(),
                [4] = Syntax.Space(),
                DefaultValue = defaultValue
            };

            public static GDMatchCaseDeclaration MatchCase() => new GDMatchCaseDeclaration();
            public static GDMatchCaseDeclaration MatchCase(Func<GDMatchCaseDeclaration, GDMatchCaseDeclaration> setup) => setup(new GDMatchCaseDeclaration());

            public static GDMatchCaseDeclaration MatchCase(params GDSyntaxToken[] unsafeTokens) => new GDMatchCaseDeclaration() { FormTokensSetter = unsafeTokens };
            public static GDMatchCaseDeclaration MatchCase(GDExpressionsList conditions, GDStatementsList statements) => new GDMatchCaseDeclaration()
            { 
                Conditions = conditions,
                Colon = new GDColon(),
                Statements = statements
            };

            public static GDMatchCaseDeclaration MatchCase(GDExpressionsList conditions, params GDStatement[] statements) => new GDMatchCaseDeclaration()
            {
                Conditions = conditions,
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDSignalDeclaration Signal() => new GDSignalDeclaration();
            public static GDSignalDeclaration Signal(Func<GDSignalDeclaration, GDSignalDeclaration> setup) => setup(new GDSignalDeclaration());
            public static GDSignalDeclaration Signal(params GDSyntaxToken[] unsafeTokens) => new GDSignalDeclaration() { FormTokensSetter = unsafeTokens };
            public static GDSignalDeclaration Signal(GDIdentifier identifier, GDParametersList parameters) => new GDSignalDeclaration()
            {
                SignalKeyword = new GDSignalKeyword(),
                [1] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                Parameters = parameters,
                CloseBracket = new GDCloseBracket()
            };

            public static GDSignalDeclaration Signal(string identifier, params GDParameterDeclaration[] parameters) => new GDSignalDeclaration()
            {
                SignalKeyword = new GDSignalKeyword(),
                [1] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                OpenBracket = new GDOpenBracket(),
                Parameters = List.Parameters(parameters),
                CloseBracket = new GDCloseBracket()
            };

            public static GDSignalDeclaration Signal(GDIdentifier identifier, params GDParameterDeclaration[] parameters) => new GDSignalDeclaration()
            {
                SignalKeyword = new GDSignalKeyword(),
                [1] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                Parameters = List.Parameters(parameters),
                CloseBracket = new GDCloseBracket()
            };

            public static GDVariableDeclaration Variable() => new GDVariableDeclaration();
            public static GDVariableDeclaration Variable(Func<GDVariableDeclaration, GDVariableDeclaration> setup) => setup(new GDVariableDeclaration());
            public static GDVariableDeclaration Variable(params GDSyntaxToken[] unsafeTokens) => new GDVariableDeclaration() { FormTokensSetter = unsafeTokens };
            public static GDVariableDeclaration Variable(GDIdentifier identifier) => new GDVariableDeclaration()
            { 
                VarKeyword = new GDVarKeyword(),
                [3] = Syntax.Space(),
                Identifier = identifier
            };

            public static GDVariableDeclaration Variable(string identifier) => new GDVariableDeclaration()
            {
                VarKeyword = new GDVarKeyword(),
                [3] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier)
            };

            public static GDVariableDeclaration Variable(string identifier, string type) => new GDVariableDeclaration()
            {
                VarKeyword = new GDVarKeyword(),
                [3] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                [4] = Syntax.Space(),
                Colon = new GDColon(),
                [5] = Syntax.Space(),
                Type = GDResolvingHelper.ParseTypeNode(type)
            };

            public static GDVariableDeclaration Variable(string identifier, GDExpression initializer) => new GDVariableDeclaration()
            {
                VarKeyword = new GDVarKeyword(),
                [3] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                [6] = Syntax.Space(),
                Assign = new GDAssign(),
                [7] = Syntax.Space(),
                Initializer = initializer
            };

            public static GDVariableDeclaration Variable(string identifier, string type, GDExpression initializer) => new GDVariableDeclaration()
            {
                VarKeyword = new GDVarKeyword(),
                [3] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                [4] = Syntax.Space(),
                Colon = new GDColon(),
                [5] = Syntax.Space(),
                Type = GDResolvingHelper.ParseTypeNode(type),
                [6] = Syntax.Space(),
                Assign = new GDAssign(),
                [7] = Syntax.Space(),
                Initializer = initializer
            };

            public static GDVariableDeclaration Variable(string identifier, string type, GDExpression initializer, GDIdentifier setMethod, GDIdentifier getMethod) => new GDVariableDeclaration()
            {
                VarKeyword = new GDVarKeyword(),
                [3] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                [4] = Syntax.Space(),
                Colon = new GDColon(),
                [5] = Syntax.Space(),
                Type = GDResolvingHelper.ParseTypeNode(type),
                [6] = Syntax.Space(),
                Assign = new GDAssign(),
                [7] = Syntax.Space(),
                Initializer = initializer,
                [8] = Syntax.Space(),

                FirstAccessorDeclarationNode = new GDSetAccessorMethodDeclaration() 
                { 
                    SetKeyword = new GDSetKeyword(),
                    [1] = Syntax.Space(),
                    Assign = new GDAssign(),
                    [2] = Syntax.Space(),
                    Identifier = setMethod
                },

                [9] = Syntax.Space(),
                Comma = new GDComma(),
                [10] = Syntax.Space(),

                SecondAccessorDeclarationNode = new GDGetAccessorMethodDeclaration()
                { 
                    GetKeyword = new GDGetKeyword(),
                    [1] = Syntax.Space(),
                    Assign = new GDAssign(),
                    [2] = Syntax.Space(),
                    Identifier = getMethod
                }
            };

            public static GDVariableDeclaration Variable(string identifier, string type, GDIdentifier setMethod, GDIdentifier getMethod) => new GDVariableDeclaration()
            {
                VarKeyword = new GDVarKeyword(),
                [3] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                [4] = Syntax.Space(),
                Colon = new GDColon(),
                [5] = Syntax.Space(),
                Type = GDResolvingHelper.ParseTypeNode(type),
                [6] = Syntax.Space(),

                FirstAccessorDeclarationNode = new GDSetAccessorMethodDeclaration()
                {
                    SetKeyword = new GDSetKeyword(),
                    [1] = Syntax.Space(),
                    Assign = new GDAssign(),
                    [2] = Syntax.Space(),
                    Identifier = setMethod
                },

                [9] = Syntax.Space(),
                Comma = new GDComma(),
                [10] = Syntax.Space(),

                SecondAccessorDeclarationNode = new GDGetAccessorMethodDeclaration()
                {
                    GetKeyword = new GDGetKeyword(),
                    [1] = Syntax.Space(),
                    Assign = new GDAssign(),
                    [2] = Syntax.Space(),
                    Identifier = getMethod
                }
            };

            public static GDVariableDeclaration Const(string identifier, string type, GDExpression initializer) => new GDVariableDeclaration()
            {
                ConstKeyword = new GDConstKeyword(),
                [3] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                [4] = Syntax.Space(),
                Colon = new GDColon(),
                [5] = Syntax.Space(),
                Type = GDResolvingHelper.ParseTypeNode(type),
                [6] = Syntax.Space(),
                Assign = new GDAssign(),
                [7] = Syntax.Space(),
                Initializer = initializer
            };

            public static GDVariableDeclaration Const(string identifier, GDExpression initializer) => new GDVariableDeclaration()
            {
                ConstKeyword = new GDConstKeyword(),
                [3] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                [6] = Syntax.Space(),
                Assign = new GDAssign(),
                [7] = Syntax.Space(),
                Initializer = initializer
            };

            public static GDVariableDeclaration Const(GDIdentifier identifier, GDTypeNode type, GDExpression initializer) => new GDVariableDeclaration()
            {
                ConstKeyword = new GDConstKeyword(),
                [3] = Syntax.Space(),
                Identifier = identifier,
                [4] = Syntax.Space(),
                Colon = new GDColon(),
                [5] = Syntax.Space(),
                Type = type,
                [6] = Syntax.Space(),
                Assign = new GDAssign(),
                [7] = Syntax.Space(),
                Initializer = initializer
            };

            public static GDVariableDeclaration Const(GDIdentifier identifier, GDExpression initializer) => new GDVariableDeclaration()
            {
                ConstKeyword = new GDConstKeyword(),
                [3] = Syntax.Space(),
                Identifier = identifier,
                [6] = Syntax.Space(),
                Assign = new GDAssign(),
                [7] = Syntax.Space(),
                Initializer = initializer
            };
        }
    }
}
