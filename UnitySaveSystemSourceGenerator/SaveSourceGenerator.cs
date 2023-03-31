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
				foreach (var (classDeclaration, fieldDeclaration) in receiver.Saves.Content.Select(x => (x.Key, x.Value)))
				{
					if (!classDeclaration.IsPartial())	// ignore if class is not partial. 
						continue;

					SemanticModel semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
					INamedTypeSymbol? classNamedTypeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
					bool isDerivedFromMonoBehaviour = classNamedTypeSymbol?.IsDerivedFrom("UnityEngine.MonoBehaviour") ?? false;
					bool isDerivedFromIIdentifiable = classNamedTypeSymbol?.IsDerivedFrom("Celezt.SaveSystem.IIdentifiable") ?? false;
					string entryKeyName = isDerivedFromMonoBehaviour ? "this" : "Guid";

					ClassDeclarationSyntax output = classDeclaration
						.RemoveNode(classDeclaration.BaseList, SyntaxRemoveOptions.KeepNoTrivia)	// Remove base type list.
						.WithMembers(SingletonList<MemberDeclarationSyntax>(
							CreateRegisterSaveObjectMethod(
								CreateSaveContent(semanticModel, Identifier(entryKeyName), fieldDeclaration))));

					// If no entryKey exist and is not derived from MonoBehaviour, require the user to create one by implementing IIdentifiable.
					if (!isDerivedFromIIdentifiable && !isDerivedFromMonoBehaviour)
						output = output.WithBaseList(
							BaseList(
								SingletonSeparatedList<BaseTypeSyntax>(
									SimpleBaseType(IdentifierName("global::Celezt.SaveSystem.IIdentifiable")))));

					output = output.NormalizeWhitespace();

					context.AddSource($"{output.Identifier.Text}.g.cs", output.GetText(Encoding.UTF8));
				}
			}
			catch (Exception e)
			{
				context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
					"SS0001",
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

		public static bool IsSave(ISymbol symbol) =>
			symbol.GetAttributes().Any(x => x.AttributeClass?
				.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Celezt.SaveSystem.SaveAttribute");

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
		public Dictionary<ClassDeclarationSyntax, List<FieldDeclarationSyntax>> Content { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is not AttributeSyntax { Name: IdentifierNameSyntax{Identifier.Text: "Save" or "SaveAttribute"} } attribute)
				return;

			var fieldDeclaration = attribute.GetParent<FieldDeclarationSyntax>();
			var classDeclaration = attribute.GetParent<ClassDeclarationSyntax>();

			if (fieldDeclaration is null)
				return;

			if (classDeclaration is null)
				return;

			if (!classDeclaration.IsPartial())
				return;

			if (Content.TryGetValue(classDeclaration, out var fields))
				fields.Add(fieldDeclaration);
			else
				Content[classDeclaration] = new() { fieldDeclaration };
		}
	}
}

