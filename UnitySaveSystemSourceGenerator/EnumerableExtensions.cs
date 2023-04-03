using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Celezt.SaveSystem.Generation
{
	internal static class EnumerableExtensions
	{
		/// <summary>
		/// Project the elements of a sequence until a valid element is found.
		/// </summary>
		public static TResult? SelectFirstOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
		{
			foreach (var element in source)
			{
				TResult? result = selector(element);

				if (result != null)
					return result;
			}

			return default(TResult);
		}
	}
}
