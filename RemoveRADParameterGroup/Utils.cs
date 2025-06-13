namespace RemoveRADParameterGroup
{
	using System;
	using System.Collections.Generic;

	public static class Utils
	{
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
