namespace RemoveRADParameterGroup
{
	using System;
	using System.Collections.Generic;

	public static class Utils
	{
		/// <summary>
		/// Wrap the text to the specified maximum line length. Source: https://gist.github.com/anderssonjohan/660952.
		/// </summary>
		/// <param name="text">The text to wrap.</param>
		/// <param name="maxLineLength">The maximal line length.</param>
		/// <returns>The wrapped text.</returns>
		public static List<string> WordWrap(string text, int maxLineLength)
		{
			var list = new List<string>();

			int currentIndex;
			var lastWrap = 0;
			var whitespace = new[] { ' ', '\r', '\n', '\t' };
			var breakChars = new[] { ' ', ',', '.', '?', '!', ':', ';', '-', '\n', '\r', '\t' };
			do
			{
				if (lastWrap + maxLineLength > text.Length)
					currentIndex = text.Length;
				else
					currentIndex = text.LastIndexOfAny(breakChars, Math.Min(text.Length - 1, lastWrap + maxLineLength)) + 1;
				if (currentIndex <= lastWrap)
					currentIndex = Math.Min(lastWrap + maxLineLength, text.Length);
				list.Add(text.Substring(lastWrap, currentIndex - lastWrap).Trim(whitespace));
				lastWrap = currentIndex;
			}
			while (currentIndex < text.Length);

			return list;
		}

		/// <summary>
		/// If the given string is longer than <paramref name="maxLength"/>, shorten it to that length and append "..." to the end.
		/// </summary>
		/// <param name="text">The string to shorten.</param>
		/// <param name="maxLength">The maximal length of the resulting string.</param>
		/// <returns>The shortened string.</returns>
		public static string Shorten(string text, int maxLength)
		{
			if (text.Length <= maxLength)
				return text;

			return text.Substring(0, maxLength - 3) + "...";
		}
	}
}
