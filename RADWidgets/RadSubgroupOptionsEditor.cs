namespace RadWidgets
{
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class RadSubgroupOptionsEditor : Section
    {
        private readonly RadGroupBaseOptionsEditor _baseOptionsEditor;

        public RadSubgroupOptionsEditor(int columnCount, RadGroupOptions parentOptions, RadSubgroupOptions options = null)
        {
            _baseOptionsEditor = new RadGroupBaseOptionsEditor(columnCount, options,
				parentOptions?.GetAnomalyThresholdOrDefault() ?? RadGroupOptions.DefaultAnomalyThreshold,
				parentOptions?.GetMinimalDurationOrDefault() ?? RadGroupOptions.DefaultMinimalDuration);

            AddSection(_baseOptionsEditor, 0, 0);
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
    }
}
