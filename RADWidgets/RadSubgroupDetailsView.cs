namespace RadWidgets
{
	using System.Collections.Generic;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class RadSubgroupDetailsView : VisibilitySection
	{
		private readonly Label _groupNameLabel;
		private readonly Label _invalidSelectionLabel;
		private readonly Label _detailsLabel;

		public RadSubgroupDetailsView()
		{
			_groupNameLabel = new Label()
			{
				Tooltip = "The name of the selected subgroup.",
				Style = TextStyle.Heading,
			};

			_invalidSelectionLabel = new Label()
			{
				Tooltip = "Invalid selection.",
			};

			_detailsLabel = new Label()
			{
				Tooltip = "The parameters and options of the selected subgroup.",
				MinWidth = 300,
			};

			AddWidget(_groupNameLabel, 0, 0, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_invalidSelectionLabel, 1, 0, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_detailsLabel, 2, 0, 2, 1, verticalAlignment: VerticalAlignment.Top);
		}

		public void ShowError(string text)
		{
			_groupNameLabel.Text = string.Empty;
			_invalidSelectionLabel.Text = text;
			_detailsLabel.Text = string.Empty;

			UpdateVisibility();
		}

		public void ShowSubgroup(RadSubgroupSelectorItem item, List<string> parameterLabels, RadGroupOptions parentOptions)
		{
			_groupNameLabel.Text = item.DisplayValue;
			_invalidSelectionLabel.Text = string.Empty;

			List<string> parameterTexts = new List<string>(parameterLabels.Count);
			for (int i = 0; i < parameterLabels.Count; ++i)
			{
				string label = string.IsNullOrEmpty(parameterLabels[i]) ? $"Parameter {i + 1}" : parameterLabels[i];
				if (i < item.Parameters.Count && item.Parameters[i] != null)
					parameterTexts.Add($"  {label}: {item.Parameters[i].ToString()}");
				else
					parameterTexts.Add($"  {label}: Not set");
			}

			var parameterText = string.Join("\n", parameterTexts);
			double anomalyThreshold = item.Options.GetAnomalyThresholdOrDefault(parentOptions.AnomalyThreshold);
			string anomalyThresholdText = item.Options.AnomalyThreshold.HasValue ? anomalyThreshold.ToString() : $"{anomalyThreshold} (same as parent group)";
			int minimalDuration = item.Options.GetMinimalDurationOrDefault(parentOptions.MinimalDuration);
			string minimalDurationText = item.Options.MinimalDuration.HasValue ? $"{minimalDuration} minutes" : $"{minimalDuration} minutes (same as parent group)";
			_detailsLabel.Text = $"Parameters:\n{parameterText}\n\n" +
				$"Options:\n" +
				$"  Anomaly threshold: {anomalyThresholdText}\n" +
				$"  Minimal anomaly duration: {minimalDurationText}";

			UpdateVisibility();
		}

		private bool GetGroupDetailsVisible()
		{
			return string.IsNullOrEmpty(_invalidSelectionLabel.Text);
		}

		private void UpdateVisibility()
		{
			bool groupDetailsVisible = GetGroupDetailsVisible();
			_detailsLabel.IsVisible = IsSectionVisible && groupDetailsVisible;
			_groupNameLabel.IsVisible = IsSectionVisible && groupDetailsVisible;
			_invalidSelectionLabel.IsVisible = IsSectionVisible && !groupDetailsVisible;
		}
	}
}
