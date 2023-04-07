using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Celezt.SaveSystem.Generation
{
	internal static class SyntaxNodeExtensions
	{
		/// <summary>
		///	Get syntaxNode parent recursively until the parent is found or null.
		/// </summary>
		public static SyntaxNode? GetParent<T1, T2, T3>(this SyntaxNode syntaxNode) where T1 : SyntaxNode where T2 : SyntaxNode where T3 : SyntaxNode
		{
			var parent = syntaxNode.Parent;
			while (parent != null)
			{
				if (parent is T1 or T2 or T3)
					return parent;

				parent = parent.Parent;
			}

			return null;
		}

		/// <summary>
		///	Get syntaxNode parent recursively until the parent is found or null.
		/// </summary>
		public static SyntaxNode? GetParent<T1, T2>(this SyntaxNode syntaxNode) where T1 : SyntaxNode where T2 : SyntaxNode
		{
			var parent = syntaxNode.Parent;
			while (parent != null)
			{
				if (parent is T1 or T2)
					return parent;

				parent = parent.Parent;
			}

			return null;
		}

		/// <summary>
		///	Get syntaxNode parent recursively until the parent is found or null.
		/// </summary>
		public static T? GetParent<T>(this SyntaxNode syntaxNode) where T : SyntaxNode
		{
			var parent = syntaxNode.Parent;
			while (parent != null)
			{
				if (parent is T t)
					return t;

				parent = parent.Parent;
			}

			return null;
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

		/// <summary>
		/// If the type contains the 'partial' keyword or not.
		/// </summary>
		/// <param name="typeDeclaration"></param>
		/// <returns></returns>
		public static bool IsPartial(this TypeDeclarationSyntax typeDeclaration) =>
			typeDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword));
	}
}

