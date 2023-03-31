using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Celezt.SaveSystem.Generation
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class SaveClassMustBePartialAnalyzer : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		= ImmutableArray.Create(SaveDiagnosticsDescriptors.ClassMustBePartial);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSymbolAction(AnalyzerNamedType, SymbolKind.NamedType);
		}

		private void AnalyzerNamedType(SymbolAnalysisContext context)
		{
			if (!SaveSourceGenerator.IsSave(context.Symbol))
				return;

			var symbol = (INamedTypeSymbol)context.Symbol;

			foreach (var declaringSyntaxReference in symbol.DeclaringSyntaxReferences)
			{
				if (declaringSyntaxReference.GetSyntax() is not 
					ClassDeclarationSyntax classDeclaration || classDeclaration.IsPartial())
					continue;

				var error = Diagnostic.Create(SaveDiagnosticsDescriptors.ClassMustBePartial, 
								classDeclaration.Identifier.GetLocation(), symbol.Name);

				context.ReportDiagnostic(error);
			}
		}
	}
}
