namespace RadDataSources
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	public static class EnumExtensions
	{
		public static string GetDescription(this Enum value)
		{
			if (value == null)
				return string.Empty;

			var member = value.GetType().GetField(value.ToString());
			if (member == null)
				return value.ToString();

			var attr = (DescriptionAttribute)Attribute.GetCustomAttribute(member, typeof(DescriptionAttribute));
			return attr?.Description ?? value.ToString();
		}

		public static string[] GetDescriptions<TEnum>() where TEnum : struct, Enum
		{
			return Enum.GetValues(typeof(TEnum))
				.Cast<TEnum>()
				.Select(e => e.GetDescription())
				.ToArray();
		}

		public static bool TryParseDescription<TEnum>(string description, out TEnum value) where TEnum : struct, Enum
		{
			foreach (var enumValue in Enum.GetValues(typeof(TEnum)).Cast<TEnum>())
			{
				if (enumValue.GetDescription().Equals(description, StringComparison.OrdinalIgnoreCase))
				{
					value = enumValue;
					return true;
				}
			}

			value = default;
			return false;
		}
	}
}
