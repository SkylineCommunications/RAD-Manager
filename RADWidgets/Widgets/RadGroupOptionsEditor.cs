namespace RadWidgets.Widgets
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
		/// <param name="radHelper">RadHelper instance to use.</param>
		/// <param name="columnCount">The number of columns the section should take (should be 2 or greater).</param>
		/// <param name="options">The initial settings to display (if any).</param>
		/// <param name="fieldWidth">The width of the input fields.</param>
		public RadGroupOptionsEditor(
			RadHelper radHelper,
			int columnCount,
			int fieldWidth,
			RadGroupOptions options = null)
		{
			_updateModelCheckBox = new CheckBox("Adapt model to new data")
			{
				IsChecked = options?.UpdateModel ?? false,
				Tooltip = "Whether to continuously update the RAD model when new trend data is available. If not selected, the model will remain static after creation, " +
				"unless you manually specify a training range.",
			};

			_baseOptionsEditor = new RadGroupBaseOptionsEditor(columnCount, radHelper.DefaultAnomalyThreshold, radHelper.DefaultMinimumAnomalyDuration, fieldWidth,
				options);

			int row = 0;
			AddWidget(_updateModelCheckBox, row++, 0, 1, columnCount);
			AddSection(_baseOptionsEditor, row, 0);
		}

		public event EventHandler Changed
		{
			add => _baseOptionsEditor.Changed += value;
			remove => _baseOptionsEditor.Changed -= value;
		}

		public event EventHandler ValidationChanged
		{
			add => _baseOptionsEditor.ValidationChanged += value;
			remove => _baseOptionsEditor.ValidationChanged -= value;
		}

		public RadGroupOptions Options
		{
			get
			{
				return new RadGroupOptions(UpdateModel, _baseOptionsEditor.AnomalyThreshold, _baseOptionsEditor.MinimalDuration);
			}
		}

		public bool IsValid => _baseOptionsEditor.IsValid;

		private bool UpdateModel => _updateModelCheckBox.IsChecked;
	}
}
