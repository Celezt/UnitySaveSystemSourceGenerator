using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

using static Celezt.SaveSystem.Generation.SaveDiagnosticsDescriptors;

namespace Celezt.SaveSystem.Generation
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	internal class SaveAnalyzer : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
			= ImmutableArray.Create
				(
					ClassMustBePartial, 
					MustBeInsideAClass
				);

		public override void Initialize(AnalysisContext context)
		{
//#if (DEBUG)
//			if (!Debugger.IsAttached)
//			{
//				Debugger.Launch();
//			}
//#endif
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			
			context.RegisterSymbolAction(Analyzer, SymbolKind.Field, SymbolKind.Property);
		}

		private void Analyzer(SymbolAnalysisContext context)
		{
			if (!context.Symbol.GetAttributes()
				.Any(x => x.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Celezt.SaveSystem.SaveAttribute"))
				return;

			foreach (var declaringSyntaxReference in context.Symbol.DeclaringSyntaxReferences)
			{
				var syntaxNode = declaringSyntaxReference.GetSyntax();	// Can be property or variable. If variable, get parent field.
				var memberDeclaration = syntaxNode is MemberDeclarationSyntax ? (MemberDeclarationSyntax)syntaxNode : syntaxNode.GetParent<MemberDeclarationSyntax>()!;
				var classDeclaration = memberDeclaration.GetParent<ClassDeclarationSyntax>();
				var attributeSyntax = memberDeclaration.AttributeLists.SelectFirstOrDefault(x => x.Attributes.FirstOrDefault(x => SaveAggregate.IsSaveAttribute(x, out _)))!;

				if (classDeclaration == null)	// If '[Save]' is not inside a class.
				{
					var typeDeclaration = memberDeclaration.GetParent<TypeDeclarationSyntax>()!;

					string typeKind = typeDeclaration.Kind() switch
					{
						SyntaxKind.StructDeclaration => "struct",
						SyntaxKind.InterfaceDeclaration => "interface",
						SyntaxKind.RecordDeclaration => "record",
						_ => "",
					};

					context.ReportDiagnostic(Diagnostic.Create(MustBeInsideAClass,
						attributeSyntax.GetLocation(), typeKind, context.Symbol.Name));
				}
				else if (!classDeclaration.IsPartial())	// If the class does not contain the modifier 'partial'.
				{
					context.ReportDiagnostic(Diagnostic.Create(ClassMustBePartial,
						attributeSyntax.GetLocation(), classDeclaration.Identifier.ValueText, context.Symbol.Name));
				}
			}
		}
	}
}
