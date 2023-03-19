using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Celezt.SaveSystem
{
	[Generator]
	public class SaveSourceGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			var output = @"
public class Test 
{
	public static void P() => Console.WriteLine(""Hello Code Generation!"");
}
";
			context.AddSource("hello.g.cs", output);
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new SaveSyntaxReceiver());
		}
	}

	public class SaveSyntaxReceiver : ISyntaxReceiver
	{
		public int Index { get; set; }

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is ClassDeclarationSyntax)
			{

			}
		}
	}
}

