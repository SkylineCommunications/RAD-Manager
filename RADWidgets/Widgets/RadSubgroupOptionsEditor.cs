namespace RadWidgets.Widgets
{
	using System;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class RadSubgroupOptionsEditor : Section
	{
		private readonly RadGroupBaseOptionsEditor _baseOptionsEditor;

		public RadSubgroupOptionsEditor(RadHelper radHelper, int columnCount, int fieldWidth, RadGroupOptions parentOptions, RadSubgroupOptions options = null)
		{
			_baseOptionsEditor = new RadGroupBaseOptionsEditor(columnCount,
				parentOptions?.GetAnomalyThresholdOrDefault(radHelper) ?? radHelper.DefaultAnomalyThreshold,
				parentOptions?.GetMinimalDurationOrDefault(radHelper) ?? radHelper.DefaultMinimumAnomalyDuration,
				fieldWidth,
				options);

			AddSection(_baseOptionsEditor, 0, 0);
		}

		public event EventHandler ValidationChanged
		{
			add => _baseOptionsEditor.ValidationChanged += value;
			remove => _baseOptionsEditor.ValidationChanged -= value;
		}

		public event EventHandler Changed
		{
			add => _baseOptionsEditor.Changed += value;
			remove => _baseOptionsEditor.Changed -= value;
		}

		public RadSubgroupOptions Options
		{
			get
			{
				return new RadSubgroupOptions
				{
					AnomalyThreshold = _baseOptionsEditor.AnomalyThreshold,
					MinimalDuration = _baseOptionsEditor.MinimalDuration,
				};
			}
		}

		public bool IsValid => _baseOptionsEditor.IsValid;
	}
}
