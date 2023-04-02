using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Celezt.SaveSystem.Generation
{
	[Generator]
	public class SaveSourceGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			try
			{
				var receiver = (MainSyntaxReceiver)context.SyntaxReceiver!;
				foreach (var (classDeclaration, namespaceDeclaration, fieldDeclaration) in 
					receiver.Saves.Content.Select(x => (x.Key, x.Value.Namespace, x.Value.Fields)))
				{
					if (!classDeclaration.IsPartial())	// ignore if class is not partial. 
						continue;

					SemanticModel semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
					INamedTypeSymbol? classNamedTypeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
					bool isDerivedFromMonoBehaviour = classNamedTypeSymbol?.IsDerivedFrom("UnityEngine.MonoBehaviour") ?? false;
					bool isDerivedFromIIdentifiable = classNamedTypeSymbol?.IsDerivedFrom("Celezt.SaveSystem.IIdentifiable") ?? false;
					string entryKeyName = isDerivedFromMonoBehaviour ? "this" : "Guid";

					ClassDeclarationSyntax generatedClass = classDeclaration
						.RemoveNode(classDeclaration.BaseList, SyntaxRemoveOptions.KeepNoTrivia)	// Remove base type list.
						.WithMembers(SingletonList<MemberDeclarationSyntax>(
							CreateRegisterSaveObjectMethod(
								CreateSaveContent(semanticModel, Identifier(entryKeyName), fieldDeclaration))));

					// If no entryKey exist and is not derived from MonoBehaviour, require the user to create one by implementing IIdentifiable.
					if (!isDerivedFromIIdentifiable && !isDerivedFromMonoBehaviour)
						generatedClass = generatedClass.WithBaseList(
							BaseList(
								SingletonSeparatedList<BaseTypeSyntax>(
									SimpleBaseType(IdentifierName("global::Celezt.SaveSystem.IIdentifiable")))));

					if (namespaceDeclaration != null)	// If the class is wrapped inside of a namespace.
					{
						NamespaceDeclarationSyntax generatedNamespace = namespaceDeclaration
							.WithMembers(SingletonList<MemberDeclarationSyntax>(generatedClass));

						context.AddSource($"{generatedClass.Identifier.Text}.g.cs", generatedNamespace.NormalizeWhitespace().GetText(Encoding.UTF8));
					}
					else
						context.AddSource($"{generatedClass.Identifier.Text}.g.cs", generatedClass.NormalizeWhitespace().GetText(Encoding.UTF8));		
				}
			}
			catch (Exception e)
			{
				context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
					"CSS000",
					"An exception was thrown by the Save generator",
					"An exception was thrown by the Save generator: '{0}'",
					"Save",
					DiagnosticSeverity.Error,
					isEnabledByDefault: true
					
				), Location.None, e));
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
//#if (DEBUG)
//			if (!Debugger.IsAttached)
//			{
//				Debugger.Launch();
//			}
//#endif

			context.RegisterForSyntaxNotifications(() => new MainSyntaxReceiver());
		}

		private MethodDeclarationSyntax CreateRegisterSaveObjectMethod(BlockSyntax blockSyntax) =>
			MethodDeclaration(
					PredefinedType(Token(SyntaxKind.VoidKeyword)), 
					Identifier("RegisterSaveObject"))
				.WithModifiers(
					TokenList(Token(SyntaxKind.ProtectedKeyword)))
				.WithBody(blockSyntax);

		private BlockSyntax CreateSaveContent(SemanticModel semanticModel, SyntaxToken entryKeyToken, List<FieldDeclarationSyntax> fieldDeclaration)
		{
			ExpressionSyntax GetEntryKey() =>
				InvocationExpression(								// SaveSystem.GetEntryKey({entryKeyToken})
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName("global::Celezt.SaveSystem.SaveSystem"),
						IdentifierName("GetEntryKey")))
				.WithArgumentList(
					ArgumentList(
						SingletonSeparatedList(
							Argument(
								IdentifierName(entryKeyToken)))));

			ExpressionSyntax SetSubEntry(ExpressionSyntax expression, SyntaxToken fieldToken, TypeSyntax typeSyntax) =>
				InvocationExpression(
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, IdentifierName("SetSubEntry"))
						.WithOperatorToken(Token(SyntaxKind.DotToken)))
					.WithArgumentList(ArgumentList(
						SeparatedList<ArgumentSyntax>(
							new SyntaxNodeOrToken[]{
								Argument(								// .SetSubEntry("{}",
									LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(fieldToken.Text))),
								Token(SyntaxKind.CommaToken),
								Argument(								// () => {fieldToken},
									ParenthesizedLambdaExpression()
										.WithExpressionBody(
											IdentifierName(fieldToken))),
								Token(SyntaxKind.CommaToken),
								Argument(								// value => {fieldToken} = ({fieldTypeToken})value);
									SimpleLambdaExpression(				
											Parameter(
												Identifier("value")))
										.WithExpressionBody(
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName(fieldToken),
												CastExpression(
													IdentifierName(semanticModel.GetTypeInfo(typeSyntax).Type!
														.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),	// global::Namespace.Type
													IdentifierName("value")))))})));

			ExpressionSyntax expressionSyntax = GetEntryKey();	// Deepest expression in the tree.
			foreach (var fieldSyntax in fieldDeclaration)	// e.g. string variable1, variable2, variable3 = ...
				foreach (var variableSyntax in fieldSyntax.Declaration.Variables) // e.g. variable1
					expressionSyntax = SetSubEntry(expressionSyntax, variableSyntax.Identifier, fieldSyntax.Declaration.Type);

			return Block(ExpressionStatement(expressionSyntax));
		}
	}

	public class MainSyntaxReceiver : ISyntaxReceiver
	{
		public SaveAggregate Saves { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			Saves.OnVisitSyntaxNode(syntaxNode);
		}
	}

	public class SaveAggregate : ISyntaxReceiver
	{
		public Dictionary<ClassDeclarationSyntax, (NamespaceDeclarationSyntax? Namespace, List<FieldDeclarationSyntax> Fields)> Content { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is not AttributeSyntax { Name: IdentifierNameSyntax{Identifier.Text: "Save" or "SaveAttribute"} } attribute)
				return;

			var fieldDeclaration = attribute.GetParent<FieldDeclarationSyntax>();
			var classDeclaration = attribute.GetParent<ClassDeclarationSyntax>();
			var namespaceDeclaration = attribute.GetParent<NamespaceDeclarationSyntax>();

			if (fieldDeclaration is null)
				return;

			if (classDeclaration is null)
				return;

			if (!classDeclaration.IsPartial())
				return;

			if (Content.TryGetValue(classDeclaration, out var data))
				data.Fields.Add(fieldDeclaration);
			else
				Content[classDeclaration] = (namespaceDeclaration, new() { fieldDeclaration });
		}
	}
}

