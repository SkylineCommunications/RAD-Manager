namespace RetrainRADModel
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class CollapsibleCheckboxList<T> : Section
	{
		private readonly Label _label;
		private readonly CollapseButton _collapseButton;
		private readonly List<Tuple<Option<T>, CheckBox>> _optionCheckBoxes;

		public CollapsibleCheckboxList(IEnumerable<Option<T>> options, int columnSpan = 2) : base()
		{
			_label = new Label();

			_optionCheckBoxes = new List<Tuple<Option<T>, CheckBox>>();
			foreach (var option in options)
			{
				var checkBox = new CheckBox(option.DisplayValue);
				_optionCheckBoxes.Add(Tuple.Create(option, checkBox));
			}

			_collapseButton = new CollapseButton(_optionCheckBoxes.Select(t => t.Item2 as Widget).ToList(), true);

			int row = 0;
			AddWidget(_label, row, 0, 1, columnSpan - 1);
			AddWidget(_collapseButton, row, columnSpan - 1);
			row++;

			foreach (var (option, checkBox) in _optionCheckBoxes)
			{
				AddWidget(checkBox, row, 0, 1, columnSpan);
				row++;
			}
		}

		public string Text
		{
			get => _label.Text;
			set => _label.Text = value;
		}

		public string CollapseText
		{
			get => _collapseButton.CollapseText;
			set => _collapseButton.CollapseText = value;
		}

		public string ExpandText
		{
			get => _collapseButton.ExpandText;
			set => _collapseButton.ExpandText = value;
		}

		public string Tooltip
		{
			get => _label.Tooltip;
			set
			{
				_label.Tooltip = value;
				_collapseButton.Tooltip = value;
			}
		}

		public List<T> Checked
		{
			get
			{
				if (_collapseButton.IsCollapsed)
					return new List<T>();

				return _optionCheckBoxes.Where(t => t.Item2.IsChecked).Select(t => t.Item1.Value).ToList();
			}
		}
	}
}
