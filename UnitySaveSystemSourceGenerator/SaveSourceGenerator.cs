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
				var receiver = (MainSyntaxReceiver)context.SyntaxReceiver;
				foreach (var batch in receiver.Saves.Content)
				{
					ClassDeclarationSyntax output = batch.Key
						.WithMembers(new(
							CreateRegisterSaveObjectMethod(
								CreateSaveContent(Identifier("key"), batch.Value))))
						.NormalizeWhitespace();

					context.AddSource($"{output.Identifier.Text}.g.cs", output.GetText(Encoding.UTF8));
				}
			}
			catch (Exception e)
			{
				context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
					"SI0000",
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
//#if DEBUG
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

		private BlockSyntax CreateSaveContent(SyntaxToken entryKeyToken, List<FieldDeclarationSyntax> fieldSyntaxes)
		{
			ExpressionSyntax GetEntryKey() =>
				InvocationExpression(								// SaveSystem.GetEntryKey({entryKeyToken})
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName("SaveSystem"),
						IdentifierName("GetEntryKey")))
				.WithArgumentList(
					ArgumentList(
						SingletonSeparatedList(
							Argument(
								IdentifierName(entryKeyToken))))
						.WithCloseParenToken(Token(
								TriviaList(),
								SyntaxKind.CloseParenToken,
								TriviaList(
									LineFeed))));

			ExpressionSyntax SetSubEntry(ExpressionSyntax expression, SyntaxToken fieldToken, TypeSyntax typeSyntax)
			{
				return InvocationExpression(
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, IdentifierName("SetSubEntry"))
				).WithArgumentList(ArgumentList(
					SeparatedList<ArgumentSyntax>(
						new SyntaxNodeOrToken[]{
							Argument(								// .SetSubEntry("{}",
									LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(fieldToken.Text))),
								Token(SyntaxKind.CommaToken),
							Argument(								//		() => {fieldToken},
									ParenthesizedLambdaExpression()
										.WithExpressionBody(
											IdentifierName(fieldToken))),
								Token(SyntaxKind.CommaToken),
							Argument(								//		value => {fieldToken} = ({fieldTypeToken})value);
								SimpleLambdaExpression(				
									Parameter(Identifier("value")))
										.WithExpressionBody(
											AssignmentExpression(
												SyntaxKind.SimpleAssignmentExpression,
												IdentifierName(fieldToken),
												CastExpression(typeSyntax, IdentifierName("value")))))})));
			}

			ExpressionSyntax expressionSyntax = GetEntryKey();	// Deepest expression in the tree.
			foreach (var fieldSyntax in fieldSyntaxes)	// e.g. string variable1, variable2, variable3 = ...
				foreach (var variableSyntax in fieldSyntax.Declaration.Variables) // e.g. variable1
					expressionSyntax = SetSubEntry(expressionSyntax, variableSyntax.Identifier, fieldSyntax.Declaration.Type);

			return Block(SingletonList<StatementSyntax>(ExpressionStatement(expressionSyntax)));
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
			if (syntaxNode is not AttributeSyntax { Name: IdentifierNameSyntax{Identifier.Text: "Save"} } attribute)
				return;

			var fieldDeclaration = attribute.GetParent<FieldDeclarationSyntax>();
			var classDeclaration = attribute.GetParent<ClassDeclarationSyntax>();

			if (Content.TryGetValue(classDeclaration, out var fields))
				fields.Add(fieldDeclaration);
			else
				Content[classDeclaration] = new() { fieldDeclaration };
		}
	}
}

