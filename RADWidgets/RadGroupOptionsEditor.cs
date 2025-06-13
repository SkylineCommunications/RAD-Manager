namespace RadWidgets
{
	using System;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	/// <summary>
	/// Editor for RAD group options.
	/// </summary>
	public class RadGroupOptionsEditor : Section
	{
		private readonly CheckBox _updateModelCheckBox;
		private readonly RadGroupBaseOptionsEditor _baseOptionsEditor;

		/// <summary>
		/// Initializes a new instance of the <see cref="RadGroupOptionsEditor"/> class.
		/// </summary>
		/// <param name="columnCount">The number of columns the section should take (should be 2 or greater).</param>
		/// <param name="options">The initial settings to display (if any).</param>
		public RadGroupOptionsEditor(
			int columnCount,
			RadGroupOptions options = null)
		{
			_updateModelCheckBox = new CheckBox("Update model on new data?")
			{
				IsChecked = options?.UpdateModel ?? false,
				Tooltip = "Whether to continuously update the RAD model when new trend data is available. If not selected, the model will only be trained after " +
				"creation and when you manually specify a training range.",
			};

			_baseOptionsEditor = new RadGroupBaseOptionsEditor(columnCount, options);
			_baseOptionsEditor.Changed += (sender, args) => Changed?.Invoke(this, EventArgs.Empty);

			int row = 0;
			AddWidget(_updateModelCheckBox, row++, 0, 1, columnCount);
			AddSection(_baseOptionsEditor, row, 0);
		}

		public event EventHandler Changed;

		public RadGroupOptions Options
		{
			get
			{
				return new RadGroupOptions(UpdateModel, _baseOptionsEditor.AnomalyThreshold, _baseOptionsEditor.MinimalDuration);
			}
		}

		private bool UpdateModel => _updateModelCheckBox.IsChecked;
	}
}
