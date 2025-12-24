namespace RadWidgets.Widgets.Generic
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
				checkBox.Changed += (sender, args) => Changed?.Invoke(this, EventArgs.Empty);
				_optionCheckBoxes.Add(Tuple.Create(option, checkBox));
			}

			_collapseButton = new CollapseButton(_optionCheckBoxes.Select(t => t.Item2 as Widget).ToList(), true);

			int row = 0;
			AddWidget(_label, row, 0, 1, columnSpan - 1);
			AddWidget(_collapseButton, row, columnSpan - 1);
			row++;

			foreach (var (_, checkBox) in _optionCheckBoxes)
			{
				AddWidget(checkBox, row, 0, 1, columnSpan);
				row++;
			}
		}

		public event EventHandler Changed;

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

		public List<T> GetChecked()
		{
			if (_collapseButton.IsCollapsed)
				return new List<T>();

			return _optionCheckBoxes.Where(t => t.Item2.IsChecked).Select(t => t.Item1.Value).ToList();
		}

		public List<T> GetUnchecked()
		{
			if (_collapseButton.IsCollapsed)
				return _optionCheckBoxes.Select(t => t.Item1.Value).ToList();

			return _optionCheckBoxes.Where(t => !t.Item2.IsChecked).Select(t => t.Item1.Value).ToList();
		}

		public void SetChecked(IEnumerable<T> valuesToCheck)
		{
			if (valuesToCheck == null)
				return;

			bool expand = false;

			var valuesSet = new HashSet<T>(valuesToCheck);
			foreach (var (option, checkBox) in _optionCheckBoxes)
			{
				if (valuesSet.Contains(option.Value))
				{
					checkBox.IsChecked = true;
					expand = true;
				}
				else
				{
					checkBox.IsChecked = false;
				}
			}

			if (_collapseButton.IsCollapsed && expand)
				_collapseButton.IsCollapsed = false;

			Changed?.Invoke(this, EventArgs.Empty);
		}

		public void UncheckAll()
		{
			foreach (var (_, checkBox) in _optionCheckBoxes)
			{
				checkBox.IsChecked = false;
			}

			if (!_collapseButton.IsCollapsed)
				_collapseButton.IsCollapsed = true;

			Changed?.Invoke(this, EventArgs.Empty);
		}

		public void CheckAll()
		{
			foreach (var (_, checkBox) in _optionCheckBoxes)
			{
				checkBox.IsChecked = true;
			}

			if (_collapseButton.IsCollapsed && _optionCheckBoxes.Any())
				_collapseButton.IsCollapsed = false;

			Changed?.Invoke(this, EventArgs.Empty);
		}
	}
}
