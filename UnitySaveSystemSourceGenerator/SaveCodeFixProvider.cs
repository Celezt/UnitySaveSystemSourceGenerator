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

namespace Celezt.SaveSystem.Generation
{
	[ExportCodeFixProvider(LanguageNames.CSharp)]
	internal class SaveCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds { get; }
			= ImmutableArray.Create(SaveDiagnosticsDescriptors.ClassMustBePartial.Id);

		public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			foreach (var diagnostic in context.Diagnostics)
			{
				if (diagnostic.Id != SaveDiagnosticsDescriptors.ClassMustBePartial.Id)
					continue;
				
				string title = SaveDiagnosticsDescriptors.ClassMustBePartial.Title.ToString();
				var action = CodeAction.Create(title,
										token => AddPartialKeywordAsync(context, diagnostic, token),
										title);

				context.RegisterCodeFix(action, diagnostic);
			}

			return Task.CompletedTask;
		}

		private async Task<Document> AddPartialKeywordAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
		{
			SyntaxNode? root = await context.Document.GetSyntaxRootAsync(cancellationToken);

			if (root is null)
				return context.Document;

			var classDeclaration = FindClassDeclaration(diagnostic, root);

			var partial = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
			var newDeclaration = classDeclaration.AddModifiers(partial);
			var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);
			var newDocument = context.Document.WithSyntaxRoot(newRoot);

			return newDocument;
		}

		private ClassDeclarationSyntax FindClassDeclaration(Diagnostic diagnostic, SyntaxNode root) =>
			root.FindToken(diagnostic.Location.SourceSpan.Start)
						.Parent?.AncestorsAndSelf()
						.OfType<ClassDeclarationSyntax>()
						.First()!;
	}
}
