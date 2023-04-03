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
	internal class SaveSourceGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			try
			{
				var receiver = (MainSyntaxReceiver)context.SyntaxReceiver!;
				foreach (var (classDeclaration, namespaceDeclaration, valueDeclarations) in 
					receiver.Saves.Content.Select(x => (x.Key, x.Value.Namespace, x.Value.Values)))
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
								CreateSaveContent(semanticModel, Identifier(entryKeyName), valueDeclarations))));

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

		private BlockSyntax CreateSaveContent(SemanticModel semanticModel, SyntaxToken entryKeyToken, List<MemberDeclarationSyntax> valueDeclarations)
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
									LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(fieldToken.Text.ToSnakeCase()))),
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
			foreach (var valueDeclaration in valueDeclarations)  // e.g. string variable1, variable2, variable3 = ... or string Variable { get; set } = ...
			{
				if (valueDeclaration is FieldDeclarationSyntax fieldDeclaration)    // If value is of type field.
					foreach (var variableSyntax in fieldDeclaration.Declaration.Variables) // e.g. variable1
						expressionSyntax = SetSubEntry(expressionSyntax, variableSyntax.Identifier, fieldDeclaration.Declaration.Type);
				else if (valueDeclaration is PropertyDeclarationSyntax propertyDeclaration)
					expressionSyntax = SetSubEntry(expressionSyntax, propertyDeclaration.Identifier, propertyDeclaration.Type);
				else
					throw new NotSupportedException($"{valueDeclaration.GetType()} is not supported as a value declaration");
			}

			return Block(ExpressionStatement(expressionSyntax));
		}
	}

	internal class MainSyntaxReceiver : ISyntaxReceiver
	{
		public SaveAggregate Saves { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			Saves.OnVisitSyntaxNode(syntaxNode);
		}
	}

	internal class SaveAggregate : ISyntaxReceiver
	{
		public Dictionary<ClassDeclarationSyntax, (NamespaceDeclarationSyntax? Namespace, List<MemberDeclarationSyntax> Values)> Content { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (!IsSaveAttribute(syntaxNode, out var attributeSyntax))
				return;

			var valueDeclaration = (MemberDeclarationSyntax?)attributeSyntax.GetParent<FieldDeclarationSyntax>() ?? attributeSyntax.GetParent<PropertyDeclarationSyntax>();
			var classDeclaration = attributeSyntax.GetParent<ClassDeclarationSyntax>();
			var namespaceDeclaration = attributeSyntax.GetParent<NamespaceDeclarationSyntax>();
			
			if (valueDeclaration is null)
				return;

			if (classDeclaration is null)
				return;

			if (!classDeclaration.IsPartial())
				return;

			if (Content.TryGetValue(classDeclaration, out var data))
				data.Values.Add(valueDeclaration);
			else
				Content[classDeclaration] = (namespaceDeclaration, new() { valueDeclaration });
		}

		public static bool IsSaveAttribute(SyntaxNode syntaxNode, out AttributeSyntax attributeSyntax)
		{
			attributeSyntax = null!;

			if (syntaxNode is AttributeSyntax{
				Name: IdentifierNameSyntax { Identifier.Text: "Save" or "SaveAttribute" }
					or QualifiedNameSyntax { Right.Identifier.Text: "Save" or "SaveAttribute" }} attribute)
			{
				attributeSyntax = attribute;
				return true;
			}

			return false;
		}
	}
}

