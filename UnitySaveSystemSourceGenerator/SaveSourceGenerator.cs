using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
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
				.WithSimpleXmlComment("Register all content inside the class that has 'SaveAttribute' to the save system.<br/><br/>" +
				"The name of a variable/property/method is used as ID by being converted to snake_case. 'Get' and 'Set' in front of a method name is ignored.<br/>" +
				"There can only be one get and set per ID. Entries with the same ID (e.g. int intValue, int GetIntValue()) is overwritten based on priority:" +
				"<list type=\"number\"><item>Method</item><item>Property</item><item>Field</item></list>" +
				"Recommended to call this from 'Awake()'.")
				.WithBody(blockSyntax);

		private BlockSyntax CreateSaveContent(SemanticModel semanticModel, SyntaxToken entryKeyToken, List<MemberDeclarationSyntax> memberDeclarations)
		{
			var entries = new Dictionary<string, (ISymbol? Get, ISymbol? Set, AttributeData Attribute)>();

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

			ExpressionSyntax SetSubEntry(ExpressionSyntax expression, string id, ISymbol? getSymbol, ISymbol? setSymbol, AttributeData attribute) =>
				InvocationExpression(
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, IdentifierName((int)attribute.ConstructorArguments[0].Value! switch
					{
						0 => "SetSubEntry",             // Default
						1 => "SetPersistentSubEntry",   // Persistent
						_ => throw new NotSupportedException($"Raw SaveSetting: {(int)attribute.ConstructorArguments[0].Value!} is not supported"),
					}))
						.WithOperatorToken(Token(SyntaxKind.DotToken)))
						.WithArgumentList(ArgumentList(
							SeparatedList<ArgumentSyntax>(new Func<List<SyntaxNodeOrToken>>(() =>
							{
								var syntaxNodeOrTokens = new List<SyntaxNodeOrToken>()
								{
									Argument(								// .SetSubEntry("{}",
										LiteralExpression(SyntaxKind.StringLiteralExpression,
											Literal(id)))
								};

								if (getSymbol != null) // Get | Save value.
								{
									syntaxNodeOrTokens.Add(Token(SyntaxKind.CommaToken));
									syntaxNodeOrTokens.Add(Argument(
										ParenthesizedLambdaExpression()
											.WithExpressionBody(getSymbol switch
											{
												IMethodSymbol => InvocationExpression(IdentifierName(getSymbol.Name)),  // () => {Identifier}(),
												_ => IdentifierName(getSymbol.Name),                                    // () => {Identifier},
											})));
								}

								if (setSymbol != null) // Set | Load value.
								{
									ITypeSymbol typeSymbol = setSymbol switch
									{
										IFieldSymbol fieldSymbol => fieldSymbol.Type,
										IPropertySymbol propertySymbol => propertySymbol.Type,
										IMethodSymbol methodSymbol => methodSymbol.Parameters.First().Type,
										_ => throw new Exception($"{setSymbol.GetType()} is not supported."),
									};

									syntaxNodeOrTokens.Add(Token(SyntaxKind.CommaToken));
									syntaxNodeOrTokens.Add(Argument(
										SimpleLambdaExpression(
												Parameter(
													Identifier("value")))
											.WithExpressionBody(setSymbol switch
											{
												IMethodSymbol =>
												InvocationExpression(                               // value => {Identifier}({Type}value);						
														IdentifierName(setSymbol.Name))
													.WithArgumentList(ArgumentList(
														SingletonSeparatedList<ArgumentSyntax>(
															Argument(CastExpression(
																IdentifierName(typeSymbol
																	.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),    // global::Namespace.Type
																IdentifierName("value")))))),
												_ =>
												AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,    // value => {Identifier} = ({Type})value);
													IdentifierName(setSymbol.Name), CastExpression(
														IdentifierName(typeSymbol
															.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),        // global::Namespace.Type
														IdentifierName("value")))
											})));
								}

								return syntaxNodeOrTokens;
							})())));

			foreach (var memberDeclaration in memberDeclarations) // e.g. string variable1, variable2, variable3 = ... or string Variable { get; set } = ...
			{
				void AddEntry(SyntaxNode syntaxNode)
				{
					int Priority(ISymbol kind) => kind switch
					{
						IMethodSymbol => 2,
						IPropertySymbol => 1,
						_ => 0,
					};

					ISymbol symbol = semanticModel.GetDeclaredSymbol(syntaxNode)!;

					if (!(symbol is IFieldSymbol or IPropertySymbol or IMethodSymbol))
						throw new NullReferenceException($"{memberDeclaration.GetType()} is not supported declaration");

					AttributeData attribute = symbol.GetAttributes().First(x => x.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Celezt.SaveSystem.SaveAttribute");
					TypedConstant optionalIdentifier = attribute.NamedArguments.FirstOrDefault(x => x.Key == "Identifier").Value;
					
					string id = optionalIdentifier.IsNull ? symbol switch
					{
						IMethodSymbol => symbol.Name.TrimDecorations("Set", "Get").ToSnakeCase(),
						_ => symbol.Name.ToSnakeCase(),
					} : ((string)optionalIdentifier.Value!).ToSnakeCase();	// Use optional identifier if it is not null.

					switch (symbol) // Get | Save value.
					{
						case IFieldSymbol:
						case IPropertySymbol:
						case IMethodSymbol { ReturnsVoid: false, Parameters.IsEmpty: true }:
							entries.TryGetValue(id, out var entry);
							ISymbol? getSymbol = entry.Get;

							if (getSymbol == null || (getSymbol != null && Priority(symbol) > Priority(getSymbol)))
								entries[id] = (symbol, entry.Set, attribute);

							break;
					}

					switch (symbol) // Set | Load value.
					{
						case IFieldSymbol { IsReadOnly: false, IsConst: false }:
						case IPropertySymbol { IsReadOnly: false }:
						case IMethodSymbol { ReturnsVoid: true, Parameters.Length: 1 }:
							entries.TryGetValue(id, out var entry);
							ISymbol? setSymbol = entry.Set;

							if (setSymbol == null || (setSymbol != null && Priority(symbol) > Priority(setSymbol)))
								entries[id] = (entry.Get, symbol, attribute);

							break;
					}
				}

				if (memberDeclaration is PropertyDeclarationSyntax or MethodDeclarationSyntax)
					AddEntry(memberDeclaration);
				else if (memberDeclaration is FieldDeclarationSyntax fieldDeclaration)    // If value is of type field.
					foreach (var variableSyntax in fieldDeclaration.Declaration.Variables) // e.g. variable1
						AddEntry(variableSyntax);
			}

			ExpressionSyntax expressionSyntax = GetEntryKey();	// Deepest expression in the tree.
			foreach (var entry in entries)
				expressionSyntax = SetSubEntry(expressionSyntax, entry.Key, entry.Value.Get, entry.Value.Set, entry.Value.Attribute);

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
			
			var memberDeclaration = (MemberDeclarationSyntax?)attributeSyntax.GetParent<FieldDeclarationSyntax, PropertyDeclarationSyntax, MethodDeclarationSyntax>();
			var classDeclaration = attributeSyntax.GetParent<ClassDeclarationSyntax>();
			var namespaceDeclaration = attributeSyntax.GetParent<NamespaceDeclarationSyntax>();
			
			if (memberDeclaration is null)
				return;

			if (classDeclaration is null)
				return;

			if (!classDeclaration.IsPartial())
				return;

			if (memberDeclaration 
				is MethodDeclarationSyntax { ParameterList.Parameters.Count: 0, ReturnType: PredefinedTypeSyntax { Keyword: SyntaxToken { RawKind: (int)SyntaxKind.VoidKeyword } } }			// Invalid: () -> void
				or MethodDeclarationSyntax { ParameterList.Parameters.Count: 1, ReturnType: PredefinedTypeSyntax { Keyword: SyntaxToken { RawKind: not (int)SyntaxKind.VoidKeyword } } })		// Invalid: (var value) -> Type
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

