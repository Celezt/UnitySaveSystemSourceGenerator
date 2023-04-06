﻿using Microsoft.CodeAnalysis;
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
				foreach (var (classDeclaration, namespaceDeclaration, membersDeclarations) in receiver.Saves.Content.Select(x => (x.Key, x.Value.Namespace, x.Value.Members)))
				{
					SemanticModel semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
					INamedTypeSymbol? classNamedTypeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
					bool isDerivedFromMonoBehaviour = classNamedTypeSymbol?.IsDerivedFrom("UnityEngine.MonoBehaviour") ?? false;
					bool isDerivedFromIIdentifiable = classNamedTypeSymbol?.AllInterfaces.Any(x => x.ToString() == "Celezt.SaveSystem.IIdentifiable") ?? false;

					if (!isDerivedFromMonoBehaviour && !isDerivedFromIIdentifiable)	// IIdentifiable or MonoBehaviour is required to generate code.
						continue;

					string entryKeyName = isDerivedFromIIdentifiable ? "Guid" : "this"; // Prioritize using existing Guid.

					ClassDeclarationSyntax generatedClass = classDeclaration
						.RemoveNode(classDeclaration.BaseList, SyntaxRemoveOptions.KeepNoTrivia)	// Remove base type list.
						.WithMembers(SingletonList<MemberDeclarationSyntax>(
							CreateRegisterSaveObjectMethod(
								CreateSaveContent(semanticModel, Identifier(entryKeyName), membersDeclarations))));

					if (namespaceDeclaration != null)	// If the class is wrapped inside of a namespace.
					{
						NamespaceDeclarationSyntax generatedNamespace = NamespaceDeclaration(
							namespaceDeclaration.AttributeLists, namespaceDeclaration.Modifiers,
							namespaceDeclaration.Name, namespaceDeclaration.Externs, namespaceDeclaration.Usings, 
							SingletonList<MemberDeclarationSyntax>(generatedClass));
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

		private BlockSyntax CreateSaveContent(SemanticModel semanticModel, SyntaxToken entryKeyToken, List<MemberDeclarationSyntax> memberDeclarations)
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

			ExpressionSyntax SetSubEntry(ExpressionSyntax expression, SyntaxNode syntaxNode)
			{
				List<SyntaxNodeOrToken> Arguments()
				{
					IFieldSymbol? fieldSymbol = syntaxNode is VariableDeclaratorSyntax ? (IFieldSymbol?)semanticModel.GetDeclaredSymbol(syntaxNode) : null;
					IPropertySymbol? propertySymbol = syntaxNode is PropertyDeclarationSyntax ? (IPropertySymbol?)semanticModel.GetDeclaredSymbol(syntaxNode) : null;

					if (fieldSymbol == null && propertySymbol == null)
						throw new NullReferenceException("SyntaxNode most be of type 'VariableDeclaratorSyntax' or 'PropertyDeclarationSyntax'.");

					ITypeSymbol typeSymbol = fieldSymbol?.Type ?? propertySymbol!.Type;
					bool isReadOnly = fieldSymbol?.IsReadOnly ?? propertySymbol!.IsReadOnly;
					bool isConst = fieldSymbol?.IsConst ?? false;
					string identifier = fieldSymbol?.Name ?? propertySymbol!.Name;
					SyntaxToken identifierToken = Identifier(identifier);

					List<SyntaxNodeOrToken> syntaxNodeOrTokens = new List<SyntaxNodeOrToken>()
					{
						Argument(								// .SetSubEntry("{}",
							LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(identifier.ToSnakeCase()))),
						Token(SyntaxKind.CommaToken),
						Argument(								// () => {fieldToken},
							ParenthesizedLambdaExpression()
								.WithExpressionBody(
									IdentifierName(identifierToken)))
					};

					if (!(isReadOnly || isConst))
					{
						syntaxNodeOrTokens.Add(Token(SyntaxKind.CommaToken));
						syntaxNodeOrTokens.Add(Argument(                                // value => {valueToken} = ({valueTypeToken})value);
							SimpleLambdaExpression(
									Parameter(
										Identifier("value")))
								.WithExpressionBody(
									AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
										IdentifierName(identifierToken),
										CastExpression(
											IdentifierName(typeSymbol!
												.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),    // global::Namespace.Type
											IdentifierName("value"))))));
					}

					return syntaxNodeOrTokens;
				}

				return InvocationExpression(
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, IdentifierName("SetSubEntry"))
						.WithOperatorToken(Token(SyntaxKind.DotToken)))
					.WithArgumentList(ArgumentList(
						SeparatedList<ArgumentSyntax>(Arguments())));
			}

			ExpressionSyntax expressionSyntax = GetEntryKey();	// Deepest expression in the tree.
			foreach (var valueDeclaration in memberDeclarations)  // e.g. string variable1, variable2, variable3 = ... or string Variable { get; set } = ...
			{
				if (valueDeclaration is FieldDeclarationSyntax fieldDeclaration)    // If value is of type field.
					foreach (var variableSyntax in fieldDeclaration.Declaration.Variables) // e.g. variable1
						expressionSyntax = SetSubEntry(expressionSyntax, variableSyntax);
				else if (valueDeclaration is PropertyDeclarationSyntax propertyDeclaration)
					expressionSyntax = SetSubEntry(expressionSyntax, propertyDeclaration);
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
		public Dictionary<ClassDeclarationSyntax, (NamespaceDeclarationSyntax? Namespace, List<MemberDeclarationSyntax> Members)> Content { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (!IsSaveAttribute(syntaxNode, out var attributeSyntax))
				return;

			var memberDeclaration = (MemberDeclarationSyntax?)attributeSyntax.GetParent<FieldDeclarationSyntax>() ?? attributeSyntax.GetParent<PropertyDeclarationSyntax>();
			var classDeclaration = attributeSyntax.GetParent<ClassDeclarationSyntax>();
			var namespaceDeclaration = attributeSyntax.GetParent<NamespaceDeclarationSyntax>();
			
			if (memberDeclaration is null)
				return;

			if (classDeclaration is null)
				return;

			if (!classDeclaration.IsPartial())
				return;

			if (Content.TryGetValue(classDeclaration, out var data))
				data.Members.Add(memberDeclaration);
			else
				Content[classDeclaration] = (namespaceDeclaration, new() { memberDeclaration });
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

