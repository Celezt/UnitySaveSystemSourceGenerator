using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Celezt.SaveSystem.Generation
{
	[Generator]
	public class SaveSourceGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			var receiver = (MainSyntaxReceiver)context.SyntaxReceiver;

			var output = @"
public class Test 
{
	public static void P() => Console.WriteLine(""Hello Code Generation! How are you?"");
}
";
			context.AddSource("hello.g.cs", output);
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new MainSyntaxReceiver());
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
		public List<FieldDeclarationSyntax> Fields { get; } = new();
		public List<ClassDeclarationSyntax> Classes { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is not AttributeSyntax { Name: IdentifierNameSyntax{Identifier.Text: "Save"} } attribute)
				return;

			var fieldDeclaration = attribute.GetParent<FieldDeclarationSyntax>();
			var classDeclaration = attribute.GetParent<ClassDeclarationSyntax>();

			Fields.Add(fieldDeclaration);
			Classes.Add(classDeclaration);
		}
	}
}

