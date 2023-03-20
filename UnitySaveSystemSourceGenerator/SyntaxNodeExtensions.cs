using Microsoft.CodeAnalysis;
using System;

namespace Celezt.SaveSystem.Generation
{
	public static class SyntaxNodeExtensions
	{
		public static T GetParent<T>(this SyntaxNode syntaxNode)
		{
			var parent = syntaxNode.Parent;
			while (true)
			{
				if (parent == null)
					throw new Exception("Parent is null");
				else if (parent is T t)
					return t;

				parent = parent.Parent;
			}
		}
	}
}

