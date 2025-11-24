namespace RadWidgets.Widgets.Dialogs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class LabelEditor : Section, IValidationWidget
	{
		private readonly TextBox _textBox;

		public LabelEditor(int index, int textBoxColumnSpan, string parameterLabel = null)
		{
			string tooltip = $"The label of the parameter {index + 1}.";
			var label = new Label($"Parameter {index + 1}")
			{
				Tooltip = tooltip,
			};

			_textBox = new TextBox
			{
				Tooltip = tooltip,
				Text = parameterLabel ?? string.Empty,
				PlaceHolder = $"Parameter {index + 1}",
				MinWidth = 300,
			};
			_textBox.Changed += (sender, args) => Changed?.Invoke(this, EventArgs.Empty);

			AddWidget(label, 0, 0);
			AddWidget(_textBox, 0, 1, 1, textBoxColumnSpan);
		}

		public event EventHandler Changed;

		public string Label => _textBox.Text?.Trim();

		public UIValidationState ValidationState
		{
			get => _textBox.ValidationState;
			set => _textBox.ValidationState = value;
		}

		public string ValidationText
		{
			get => _textBox.ValidationText;
			set => _textBox.ValidationText = value;
		}
	}

	public class RadLabelEditorDialog : Dialog
	{
		private readonly List<LabelEditor> _labelEditors;
		private readonly WrappingLabel _detailsLabel;
		private readonly Button _okButton;

		public RadLabelEditorDialog(IEngine engine, List<string> labels) : base(engine)
		{
			Title = "Edit Parameter Labels";
			_labelEditors = new List<LabelEditor>(labels.Count);
			foreach (var label in labels)
			{
				var editor = new LabelEditor(_labelEditors.Count, 10, label);
				editor.Changed += (sender, args) => OnLabelChanged(sender as LabelEditor);
				_labelEditors.Add(editor);
			}

			int nrOfWhitespaces = _labelEditors.First().ColumnCount - 1;
			var whitespaces = new List<WhiteSpace>(nrOfWhitespaces);
			for (int i = 0; i < nrOfWhitespaces; ++i)
				whitespaces.Add(new WhiteSpace());

			_detailsLabel = new WrappingLabel(200);

			var cancelButton = new Button("Cancel")
			{
				Tooltip = "Discard the changes to the labels above.",
				MaxWidth = 150,
			};
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			_okButton = new Button("OK")
			{
				Style = ButtonStyle.CallToAction,
			};
			_okButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			OnLabelChanged(_labelEditors.First());

			int row = 0;
			foreach (var editor in _labelEditors)
				AddSection(editor, row++, 0);

			int col = 1;
			foreach (var ws in whitespaces)
				AddWidget(ws, row, col++);
			row++;

			AddWidget(_detailsLabel, row, 0, 1, _labelEditors.First().ColumnCount);
			row++;

			AddWidget(cancelButton, row, _labelEditors.First().ColumnCount - 2, horizontalAlignment: HorizontalAlignment.Right);
			AddWidget(_okButton, row, _labelEditors.First().ColumnCount - 1);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public List<string> GetLabels()
		{
			return _labelEditors.Select(e => e.Label).ToList();
		}

		private void SetLabelEditorsValid()
		{
			foreach (var editor in _labelEditors)
			{
				editor.ValidationState = UIValidationState.Valid;
				editor.ValidationText = string.Empty;
			}
		}

		private void OnLabelChanged(LabelEditor editor)
		{
			SetLabelEditorsValid();

			bool isEmpty = string.IsNullOrEmpty(editor.Label);
			if (_labelEditors.Any(e => string.IsNullOrEmpty(e.Label) != isEmpty))
			{
				_detailsLabel.Text = "You should either provide a label for all parameters or for none.";
				_detailsLabel.IsVisible = true;
				_okButton.Tooltip = "Set the parameter labels above. Note that before submitting the group, you should make sure to either provide a label for all parameters, or for none.";
				return;
			}

			if (!isEmpty)
			{
				// Here, a label is provided for all parameters
				var whitespaceLabels = _labelEditors.Where(e => string.IsNullOrWhiteSpace(e.Label));
				if (whitespaceLabels.Any())
				{
					_detailsLabel.Text = "Labels with only whitespace characters are not allowed.";
					_detailsLabel.IsVisible = true;
					_okButton.Tooltip = "Set the parameter labels above. Note that before submitting the group, you will have to fix all labels only containing whitespace characters.";

					foreach (var e in whitespaceLabels)
					{
						e.ValidationState = UIValidationState.Invalid;
						editor.ValidationText = "Labels with only whitespace characters are not allowed";
					}

					return;
				}

				var duplicateLabels = _labelEditors.GroupBy(e => e.Label, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1);
				if (duplicateLabels.Any())
				{
					_detailsLabel.Text = $"The labels {duplicateLabels.Select(g => g.Key).HumanReadableJoin()} are duplicated.";
					_detailsLabel.IsVisible = true;
					_okButton.Tooltip = "Set the parameter labels above. Note that before submitting the group, you will have to remove the duplicate labels.";

					foreach (var e in duplicateLabels.SelectMany(g => g))
					{
						e.ValidationState = UIValidationState.Invalid;
						e.ValidationText = "This label is duplicated";
					}

					return;
				}
			}

			_detailsLabel.Text = string.Empty;
			_detailsLabel.IsVisible = false;
			if (isEmpty)
				_okButton.Tooltip = "Set no parameter labels for the current group."; // Here all labels are empty
			else
				_okButton.Tooltip = "Set the parameter labels above for the current group."; // Here a label is provided for all parameters and no duplicates are found
		}
	}
}
