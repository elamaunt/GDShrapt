using GDShrapt.Reader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Converter
{
    public class CSharpGeneratingVisitor : INodeVisitor
    {
        private readonly ConversionSettings _settings;

        private NamespaceDeclarationSyntax _generatedNamespace;

        private int _classCounter = 0;

        private Stack<StackNode> _completionStack = new Stack<StackNode>();


        public CSharpGeneratingVisitor(ConversionSettings settings)
        {
            _settings = settings;
        }

        public string BuildCSharpNormalisedCode()
        {
            while (_completionStack.Count > 0)
                LeftNode();

            if (_generatedNamespace == null)
                return null;

            // Normalize and get code as string.
            var code = _generatedNamespace
                .NormalizeWhitespace()
                .ToFullString();

            return code;
        }

        public void Visit(GDClassDeclaration d)
        {
            // Generate new namespace
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(string.IsNullOrWhiteSpace(_settings.Namespace) ? "Generated" : _settings.Namespace ?? "Generated")).NormalizeWhitespace();

            @namespace = @namespace.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));
            @namespace = @namespace.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")));
            @namespace = @namespace.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Godot")));

            Push(@namespace, nameSpace => _generatedNamespace = (NamespaceDeclarationSyntax)nameSpace);

            var classDeclaration = GenerateClassShell(d.Name?.Sequence, d.ExtendsClass?.Sequence, d.IsTool);

            Push(classDeclaration, (NamespaceDeclarationSyntax nameSpace, ClassDeclarationSyntax @class ) => nameSpace.AddMembers(@class));
        }

        public void Visit(GDInnerClassDeclaration d)
        {
            var classDeclaration = GenerateClassShell(d.Name?.Sequence, d.ExtendsClass?.Sequence, d.IsTool);
            Push(classDeclaration, (ClassDeclarationSyntax @parentClass, ClassDeclarationSyntax @class) => @parentClass.AddMembers(@class));
        }

        private ClassDeclarationSyntax GenerateClassShell(string name, string extendsClassName, bool isTool)
        {
            // Get class name
            var className = name ?? (_settings.FileName != null ? Path.GetFileNameWithoutExtension(_settings.FileName) : null) ?? $"GeneratedClass{_classCounter++}";

            if (_settings.ConvertGDScriptNamingStyleToSharp)
                className = ConversionHelper.ConvertDeclarationNameToSharpNamingStyle(className);

            // Generate class declaration
            var classDeclaration = SyntaxFactory.ClassDeclaration(className);

            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            // Check tool atribute
            if (isTool)
            {
                classDeclaration = classDeclaration.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(
                    new[] { SyntaxFactory.Attribute(SyntaxFactory.ParseName("Tool"))
                })));
            }

            // Check base class
            if (!string.IsNullOrWhiteSpace(extendsClassName))
            {
                if (_settings.ConvertGDScriptNamingStyleToSharp)
                    extendsClassName = ConversionHelper.ConvertDeclarationNameToSharpNamingStyle(extendsClassName);

                classDeclaration = classDeclaration.AddBaseListTypes(
                 SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(extendsClassName)));
            }

            return classDeclaration;
        }

        public void Visit(GDParameterDeclaration d)
        {

        }

        public void Visit(GDVariableDeclaration d)
        {
            var field =
                SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.ParseTypeName(ConversionHelper.ExtractType(_settings, d)),
                    SyntaxFactory.SeparatedList(new[] { SyntaxFactory.VariableDeclarator(ConversionHelper.GetName(_settings, d.Identifier)) })
                ))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            Push(field, (ClassDeclarationSyntax @class, FieldDeclarationSyntax field) => @class.AddMembers(field));

            // Create an auto-property
            /*var property =
                SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(ConversionHelper.ExtractType(_settings, d)),
                    ConversionHelper.GetName(_settings, d.Identifier)
                )
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                );
            */

            //_classDeclaration = _classDeclaration.AddMembers(property);
            // _classDeclaration = _classDeclaration.AddMembers(field);
        }

        public void Visit(GDMethodDeclaration d)
        {

        }

        //////////////////////////////////////////////////////////////////////////////////////////////

        public void Visit(GDToolAtribute a)
        {

        }

        public void Visit(GDClassNameAtribute a)
        {

        }

        public void Visit(GDExportAtribute a)
        {

        }

        public void Visit(GDExtendsAtribute a)
        {

        }

        public void Visit(GDExpressionStatement s)
        {

        }

        //////////////////////////////////////////////////////////////////////////////////////////////

        public void Visit(GDIfStatement s)
        {

        }

        public void Visit(GDReturnStatement s)
        {

        }

        public void Visit(GDForStatement s)
        {

        }

        public void Visit(GDMatchStatement s)
        {

        }

        public void Visit(GDPassStatement s)
        {

        }

        public void Visit(GDVariableDeclarationStatement s)
        {

        }

        public void Visit(GDWhileStatement s)
        {

        }

        public void Visit(GDYieldStatement s)
        {

        }

        public void Visit(GDArrayInitializerExpression e)
        {

        }

        public void Visit(GDBracketExpression e)
        {

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
        public void Visit(GDCallExression e)
        {

        }

        public void Visit(GDDualOperatorExression e)
        {

        }

        public void Visit(GDIdentifierExpression e)
        {

        }

        public void Visit(GDIndexerExression e)
        {

        }

        public void Visit(GDMemberOperatorExpression e)
        {

        }

        public void Visit(GDNumberExpression e)
        {

        }

        public void Visit(GDParametersExpression e)
        {

        }

        public void Visit(GDSingleOperatorExpression e)
        {

        }

        public void Visit(GDStringExpression e)
        {

        }

        public void Visit(GDReturnExpression e)
        {

        }

        public void Visit(GDPassExpression e)
        {

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        class StackNode
        {
            public SyntaxNode Node;
            public Action<SyntaxNode> Completion;
        }

        void Update<T>(Func<T, T> update)
            where T : SyntaxNode
        {
            var node = _completionStack.Where(x => x.Node is T).First();
            node.Node = update((T)node.Node);
        }

        void Push(SyntaxNode node, Action<SyntaxNode> completion) => _completionStack.Push(new StackNode()
        {
            Node = node,
            Completion = completion
        });

        void Push<B, T>(B node, Func<T, B, T> update) where T : SyntaxNode where B : SyntaxNode => _completionStack.Push(new StackNode()
        {
            Node = node,
            Completion = n => Update<T>(x => update(x, (B)n))
        });
            
        public void LeftNode()
        {
            var node = _completionStack.Pop();
            node.Completion(node.Node);
        }
    }
}