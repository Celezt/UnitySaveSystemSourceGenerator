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

namespace Celezt.SaveSystem.Generation
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class SaveAnalyzer : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
			= ImmutableArray.Create(SaveDiagnosticsDescriptors.ClassMustBePartial);

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
			
			context.RegisterSymbolAction(Analyzer, SymbolKind.Field);
		}

		private void Analyzer(SymbolAnalysisContext context)
		{
			var symbol = (IFieldSymbol)context.Symbol;

			if (!symbol.GetAttributes()
				.Any(x => x.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Celezt.SaveSystem.SaveAttribute"))
				return;

			foreach (var declaringSyntaxReference in symbol.DeclaringSyntaxReferences)
			{
				var classDeclaration = declaringSyntaxReference.GetSyntax().GetParent<ClassDeclarationSyntax>();

				if (classDeclaration == null || classDeclaration.IsPartial())
					continue;

				var error = Diagnostic.Create(SaveDiagnosticsDescriptors.ClassMustBePartial,
								symbol.Locations.FirstOrDefault(), classDeclaration.Identifier.ValueText, symbol.Name);

				context.ReportDiagnostic(error);
			}
		}
	}
}
