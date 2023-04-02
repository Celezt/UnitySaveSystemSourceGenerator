﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Celezt.SaveSystem.Generation
{
	internal static class StringExtensions
	{
		public static string ToSnakeCase(this string text, bool trimUnderscore = true)
		{
			if (string.IsNullOrEmpty(text))
				return text;

			Span<char> newText = stackalloc char[text.Length + Math.Min(2, text.Length / 5)];
			UnicodeCategory? previousCategory = default;
			int newTextIndex = 0;
			bool isTrimmable = true;

			for (var currentIndex = 0; currentIndex < text.Length; currentIndex++)
			{
				var currentChar = text[currentIndex];
				if (currentChar == '_')
				{
					if (trimUnderscore && isTrimmable)	// skip all _ until another letter is found.
						continue;

					newText[newTextIndex++] = '_';
					previousCategory = null;
					continue;
				}
				else 
					isTrimmable = false;

				var currentCategory = char.GetUnicodeCategory(currentChar);
				switch (currentCategory)
				{
					case UnicodeCategory.UppercaseLetter:
					case UnicodeCategory.TitlecaseLetter:
						if (previousCategory == UnicodeCategory.SpaceSeparator ||
							previousCategory == UnicodeCategory.LowercaseLetter ||
							previousCategory != UnicodeCategory.DecimalDigitNumber &&
							previousCategory != null &&
							currentIndex > 0 &&
							currentIndex + 1 < text.Length &&
							char.IsLower(text[currentIndex + 1]))
						{
							newText[newTextIndex++] = '_';
						}

						currentChar = char.ToLower(currentChar, CultureInfo.InvariantCulture);
						break;

					case UnicodeCategory.LowercaseLetter:
					case UnicodeCategory.DecimalDigitNumber:
						if (previousCategory == UnicodeCategory.SpaceSeparator)
						{
							newText[newTextIndex++] = '_';
						}
						break;

					default:
						if (previousCategory != null)
						{
							previousCategory = UnicodeCategory.SpaceSeparator;
						}
						continue;
				}

				newText[newTextIndex++] = currentChar;
				previousCategory = currentCategory;
			}
			
			return newText.Slice(0, newTextIndex).ToString();
		}
	}
}
