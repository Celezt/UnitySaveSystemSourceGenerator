using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Text;

using SSD = Celezt.SaveSystem.Generation.SaveDiagnosticsDescriptors;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Celezt.SaveSystem.Generation
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	internal class SaveAnalyzer : DiagnosticAnalyzer
	{
		private HashSet<string> _hasCheckedRegisterSaveObject = null!;

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
			= ImmutableArray.Create
				(
					SSD.ClassMustBePartial,
					SSD.MustBeInsideAClass,
					SSD.MustImplementIIdentifiable,
					SSD.GetMethodMustReturnAndNoParameters,
					SSD.SetMethodMustBeVoidAndHaveParameters,
					SSD.MustCallRegisterSaveObjectInvocation
				);

		public override void Initialize(AnalysisContext context)
		{
			_hasCheckedRegisterSaveObject = new();

//#if (DEBUG)
//			if (!Debugger.IsAttached)
//			{
//				Debugger.Launch();
//			}
//#endif
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			
			context.RegisterSyntaxNodeAction(SyntaxNodeAnalyzer, SyntaxKind.AttributeList);
		}

		private void SyntaxNodeAnalyzer(SyntaxNodeAnalysisContext context)
		{
			var symbol = context.ContainingSymbol;

			if (symbol == null)
				return;

			if (!symbol.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Celezt.SaveSystem.SaveAttribute"))
				return;

			foreach (var declaringSyntaxReference in symbol.DeclaringSyntaxReferences)
			{
				var syntaxNode = declaringSyntaxReference.GetSyntax();  // Can be property or variable. If variable, get parent field.
				var memberDeclaration = syntaxNode is MemberDeclarationSyntax ? (MemberDeclarationSyntax)syntaxNode : syntaxNode.GetParent<MemberDeclarationSyntax>()!;
				var attributeSyntax = memberDeclaration.AttributeLists.SelectFirstOrDefault(x => x.Attributes.FirstOrDefault(x => SaveAggregate.IsSaveAttribute(x, out _)))!;
				var classDeclaration = memberDeclaration.GetParent<ClassDeclarationSyntax>();
				var classNamedTypeSymbol = classDeclaration != null ? context.SemanticModel.GetDeclaredSymbol(classDeclaration) : null;
				var isDerivedFromMonoBehaviour = classNamedTypeSymbol?.IsDerivedFrom("UnityEngine.MonoBehaviour") ?? false;
				var isDerivedFromIIdentifiable = classNamedTypeSymbol?.AllInterfaces.Any(x => x.ToString() == "Celezt.SaveSystem.IIdentifiable") ?? false;

				if (classDeclaration == null)   // If '[Save]' is not inside a class.
				{
					var typeDeclaration = memberDeclaration.GetParent<TypeDeclarationSyntax>()!;

					string typeKind = typeDeclaration.Kind() switch
					{
						SyntaxKind.StructDeclaration => "struct",
						SyntaxKind.InterfaceDeclaration => "interface",
						SyntaxKind.RecordDeclaration => "record",
						_ => "",
					};

					context.ReportDiagnostic(Diagnostic.Create(SSD.MustBeInsideAClass,
						attributeSyntax.GetLocation(), typeKind));
				}
				else if (!(isDerivedFromIIdentifiable || isDerivedFromMonoBehaviour))    // If the class is not derived from MonoBehaviour and has not implemented IIdentifiable.
				{
					context.ReportDiagnostic(Diagnostic.Create(SSD.MustImplementIIdentifiable,
						attributeSyntax.GetLocation(), classDeclaration.Identifier.ValueText));
				}
				else if (!classDeclaration.IsPartial()) // If the class does not contain the modifier 'partial'.
				{
					context.ReportDiagnostic(Diagnostic.Create(SSD.ClassMustBePartial,
						attributeSyntax.GetLocation(), classDeclaration.Identifier.ValueText));
				}
				else if (memberDeclaration is MethodDeclarationSyntax methodDeclaration)
				{
					if (methodDeclaration is { 
						ParameterList.Parameters.Count: 0, ReturnType: PredefinedTypeSyntax { Keyword: SyntaxToken { RawKind: (int)SyntaxKind.VoidKeyword } } })    // Invalid: () -> void
					{
						context.ReportDiagnostic(Diagnostic.Create(SSD.GetMethodMustReturnAndNoParameters,
							methodDeclaration.GetLocation(), methodDeclaration.Identifier.ValueText));
					}
					else if (methodDeclaration is { 
						ParameterList.Parameters.Count: 1, ReturnType: PredefinedTypeSyntax { Keyword: SyntaxToken { RawKind: not (int)SyntaxKind.VoidKeyword } } })     // Invalid: (var value) -> Type
					{
						context.ReportDiagnostic(Diagnostic.Create(SSD.SetMethodMustBeVoidAndHaveParameters,
							methodDeclaration.GetLocation(), methodDeclaration.Identifier.ValueText));
					}
				}
				else
				{
					lock (_hasCheckedRegisterSaveObject)
					{
						if (!_hasCheckedRegisterSaveObject.Contains(classNamedTypeSymbol!.ToString())) // Only check once per class.
						{
							_hasCheckedRegisterSaveObject.Add(classNamedTypeSymbol!.ToString());

							var registerSaveObjectInvocation = classDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>()
																	.FirstOrDefault(x => x.DescendantNodes().OfType<IdentifierNameSyntax>()
																		.Any(x => x.Identifier.ToString() == "RegisterSaveObject"));

							if (registerSaveObjectInvocation == null)
								context.ReportDiagnostic(Diagnostic.Create(SSD.MustCallRegisterSaveObjectInvocation,
									classDeclaration.GetLocation(), classDeclaration.Identifier.ValueText));
						}
					}
				}
			}
		}
	}
}
