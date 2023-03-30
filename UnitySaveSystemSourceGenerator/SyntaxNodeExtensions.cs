using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

		/// <summary>
		/// Creates a new tree of nodes with the specified node removed.
		/// </summary>
		/// <typeparam name="TRoot">The type of the root node.</typeparam>
		/// <param name="root">The root node from which to remove a descendant node from.</param>
		/// <param name="node">The node to remove.</param>
		/// <param name="options">Options that determine how the node's trivia is treated.</param>
		public static TRoot RemoveNode<TRoot>(this TRoot root, SyntaxNode? node, SyntaxRemoveOptions options) where TRoot : SyntaxNode
		{
			if (node == null)
				return root;
			
			return Microsoft.CodeAnalysis.SyntaxNodeExtensions.RemoveNode(root, node, options) ?? root;
		}

			public static bool IsDerivedFrom(this INamedTypeSymbol symbol, string typeFullName)
		{
			while (symbol.BaseType != null)
			{
				if (symbol.ToString() == typeFullName) 
					return true;

				symbol = symbol.BaseType;
			}

			return false;
		}
	}
}

