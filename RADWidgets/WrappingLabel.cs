namespace RadWidgets
{
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class WrappingLabel : Label
	{
		private int? _maxTextWidth;
		private string _text;

		public WrappingLabel(int? maxTextWidth = 120) : this(string.Empty, maxTextWidth)
		{
		}

		public WrappingLabel(string text, int? maxTextWidth = 120)
		{
			_maxTextWidth = maxTextWidth;
			_text = text;
			UpdateText();
		}

		public int? MaxTextWidth
		{
			get => _maxTextWidth;
			set
			{
				if (_maxTextWidth == value)
					return;

				_maxTextWidth = value;
				UpdateText();
			}
		}

		public new string Text
		{
			get => _text;
			set
			{
				_text = value;
				UpdateText();
			}
		}

		private void UpdateText()
		{
			if (_maxTextWidth.HasValue)
				base.Text = string.Join("\n", RadWidgets.Utils.WordWrap(_text, _maxTextWidth.Value));
			else
				base.Text = _text;
		}
	}
}
