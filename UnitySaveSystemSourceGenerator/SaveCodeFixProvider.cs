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

using static Celezt.SaveSystem.Generation.SaveDiagnosticsDescriptors;

namespace Celezt.SaveSystem.Generation
{
	[ExportCodeFixProvider(LanguageNames.CSharp)]
	internal class SaveCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds { get; }
			= ImmutableArray.Create
				(
					ClassMustBePartial.Id,
					MustBeInsideAClass.Id
				);

		public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			foreach (Diagnostic diagnostic in context.Diagnostics)
			{
				void Register(string title, Func<CodeFixContext, Diagnostic, CancellationToken, Task<Document>> createChangedDocument) =>
					context.RegisterCodeFix(CodeAction.Create(title, async token => await createChangedDocument(context, diagnostic, token), title), diagnostic);

				if (diagnostic.Id == ClassMustBePartial.Id)
					Register(ClassMustBePartial.Title.ToString(), AddPartialKeywordAsync);
				else if (diagnostic.Id == MustBeInsideAClass.Id)
					Register(MustBeInsideAClass.Title.ToString(), ChangeToClass);
			}

			return Task.CompletedTask;
		}

		private async Task<Document> AddPartialKeywordAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
		{
			SyntaxNode? root = await context.Document.GetSyntaxRootAsync(cancellationToken);

			if (root is null)
				return context.Document;

			var classDeclaration = FindDeclaration<ClassDeclarationSyntax>(diagnostic, root);

			var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
			var newDeclaration = classDeclaration.AddModifiers(partialToken);
			var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);

			return context.Document.WithSyntaxRoot(newRoot);
		}

		private async Task<Document> ChangeToClass(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
		{
			SyntaxNode? root = await context.Document.GetSyntaxRootAsync(cancellationToken);

			if (root is null)
				return context.Document;

			var typeDeclaration = FindDeclaration<TypeDeclarationSyntax>(diagnostic, root);	// E.g. struct, interface, record.

			var newClassDeclaration = SyntaxFactory.ClassDeclaration(typeDeclaration.AttributeLists, typeDeclaration.Modifiers, typeDeclaration.Identifier,
										typeDeclaration.TypeParameterList, typeDeclaration.BaseList, typeDeclaration.ConstraintClauses, typeDeclaration.Members);
			var newRoot = root.ReplaceNode(typeDeclaration, newClassDeclaration);

			return context.Document.WithSyntaxRoot(newRoot);			
		}

		private static T FindDeclaration<T>(Diagnostic diagnostic, SyntaxNode root) where T : TypeDeclarationSyntax =>
			root.FindToken(diagnostic.Location.SourceSpan.Start)
						.Parent?.AncestorsAndSelf()
						.OfType<T>()
						.First()!;
	}
}
