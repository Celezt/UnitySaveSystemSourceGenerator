using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SSD = Celezt.SaveSystem.Generation.SaveDiagnosticsDescriptors;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Celezt.SaveSystem.Generation
{
	[ExportCodeFixProvider(LanguageNames.CSharp)]
	internal class SaveCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds { get; }
			= ImmutableArray.Create
				(
					SSD.ClassMustBePartial.Id,
					SSD.MustBeInsideAClass.Id,
					SSD.MustImplementIIdentifiable.Id,
					SSD.MustCallRegisterSaveObjectInvocation.Id
				);

		public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			foreach (Diagnostic diagnostic in context.Diagnostics)
			{
				void Register(string title, Func<CodeFixContext, Diagnostic, CancellationToken, Task<Document>> createChangedDocument) =>
					context.RegisterCodeFix(CodeAction.Create(title, async token => await createChangedDocument(context, diagnostic, token), title), diagnostic);

				if (diagnostic.Id == SSD.ClassMustBePartial.Id)
					Register(SSD.ClassMustBePartial.Title.ToString(), AddPartialKeywordAsync);
				else if (diagnostic.Id == SSD.MustBeInsideAClass.Id)
					Register(SSD.MustBeInsideAClass.Title.ToString(), ChangeToClass);
				else if (diagnostic.Id == SSD.MustImplementIIdentifiable.Id)
					Register(SSD.MustImplementIIdentifiable.Title.ToString(), ImplementIIdentifiable);
				else if (diagnostic.Id == SSD.MustCallRegisterSaveObjectInvocation.Id)
					Register(SSD.MustCallRegisterSaveObjectInvocation.Title.ToString(), AddRegisterObjectSave);
			}

			return Task.CompletedTask;
		}

		private async Task<Document> AddPartialKeywordAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
		{
			SyntaxNode? root = await context.Document.GetSyntaxRootAsync(cancellationToken);

			if (root is null)
				return context.Document;

			var classDeclaration = FindDeclaration<ClassDeclarationSyntax>(diagnostic, root);

			var newDeclaration = classDeclaration.AddModifiers(Token(SyntaxKind.PartialKeyword));
			var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);

			return context.Document.WithSyntaxRoot(newRoot);
		}

		private async Task<Document> ChangeToClass(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
		{
			SyntaxNode? root = await context.Document.GetSyntaxRootAsync(cancellationToken);

			if (root is null)
				return context.Document;

			var typeDeclaration = FindDeclaration<TypeDeclarationSyntax>(diagnostic, root);	// E.g. struct, interface, record.

			var newClassDeclaration = ClassDeclaration(typeDeclaration.AttributeLists, typeDeclaration.Modifiers, typeDeclaration.Identifier,
										typeDeclaration.TypeParameterList, typeDeclaration.BaseList, typeDeclaration.ConstraintClauses, typeDeclaration.Members);
			var newRoot = root.ReplaceNode(typeDeclaration, newClassDeclaration);

			return context.Document.WithSyntaxRoot(newRoot);			
		}

		private async Task<Document> ImplementIIdentifiable(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
		{
			SyntaxNode? root = await context.Document.GetSyntaxRootAsync(cancellationToken);

			if (root is null)
				return context.Document;

			var classDeclaration = FindDeclaration<ClassDeclarationSyntax>(diagnostic, root);

			var newDeclaration = classDeclaration.AddBaseListTypes(SimpleBaseType(IdentifierName("IIdentifiable")));
			var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);

			return context.Document.WithSyntaxRoot(newRoot);
		}

		private async Task<Document> AddRegisterObjectSave(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
		{
			SyntaxNode? root = await context.Document.GetSyntaxRootAsync(cancellationToken);

			if (root is null)
				return context.Document;

			var awakeMethod = root.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault(x => x.Identifier.ToString() == "Awake");	// If Awake exist.
			var registerSaveObject = ExpressionStatement(InvocationExpression(IdentifierName("RegisterSaveObject")));
			var classDeclaration = FindDeclaration<ClassDeclarationSyntax>(diagnostic, root);

			ClassDeclarationSyntax newDeclaration;
			if (awakeMethod != null)
			{
				MethodDeclarationSyntax newAwakeMethod;
				if (awakeMethod.Body == null)
					newAwakeMethod = awakeMethod.WithBody(
									Block(SingletonList<StatementSyntax>(registerSaveObject)));
				else
					newAwakeMethod = awakeMethod.WithBody(
									Block(awakeMethod.Body.Statements.Insert(0, registerSaveObject)));

				newDeclaration = classDeclaration.ReplaceNode(awakeMethod, newAwakeMethod);
			}
			else
			{
				awakeMethod = MethodDeclaration(
									PredefinedType(Token(SyntaxKind.VoidKeyword)),
									Identifier("Awake"))
								.WithModifiers(
									TokenList(
										Token(SyntaxKind.PrivateKeyword)))
								.WithBody(
									Block(SingletonList<StatementSyntax>(registerSaveObject)));

				// Add method before any existing methods. If no method exist, add it last.
				int indexOfFirstMethod = classDeclaration.Members.IndexOf(x => x is MethodDeclarationSyntax);
				newDeclaration = indexOfFirstMethod > 0 ?
					classDeclaration.WithMembers(classDeclaration.Members.Insert(indexOfFirstMethod, awakeMethod)) : classDeclaration.AddMembers(awakeMethod);
			}

			var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);

			return context.Document.WithSyntaxRoot(newRoot);
		}

		private static T FindDeclaration<T>(Diagnostic diagnostic, SyntaxNode root) where T : TypeDeclarationSyntax =>
			root.FindToken(diagnostic.Location.SourceSpan.Start)
						.Parent?.AncestorsAndSelf()
						.OfType<T>()
						.First()!;
	}
}
