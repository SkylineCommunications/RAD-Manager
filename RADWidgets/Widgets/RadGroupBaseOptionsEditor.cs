namespace RadWidgets.Widgets
{
	using System;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	/// <summary>
	/// Editor for RAD group options.
	/// </summary>
	public class RadGroupBaseOptionsEditor : Section
	{
		public const int MinimumDurationLowerLimit = 5;
		private readonly CheckBox _anomalyThresholdOverrideCheckBox;
		private readonly Numeric _anomalyThresholdNumeric;
		private readonly CheckBox _minimalDurationOverrideCheckBox;
		private readonly Time _minimalDurationTime;
		private readonly double _defaultAnomalyThreshold;
		private readonly int _defaultMinimalDuration;
		private bool _isValid;

		/// <summary>
		/// Initializes a new instance of the <see cref="RadGroupBaseOptionsEditor"/> class.
		/// </summary>
		/// <param name="columnCount">The number of columns the section should take (should be 2 or greater).</param>
		/// <param name="options">The initial settings to display (if any).</param>
		/// <param name="defaultAnomalyThreshold">The default anomaly threshold to use if not overridden.</param>
		/// <param name="defaultMinimalDuration">The default minimal duration to use if not overridden.</param>
		public RadGroupBaseOptionsEditor(
			int columnCount,
			double defaultAnomalyThreshold,
			int defaultMinimalDuration,
			int fieldWidth,
			RadGroupBaseOptions options = null)
		{
			_defaultAnomalyThreshold = defaultAnomalyThreshold;
			_defaultMinimalDuration = defaultMinimalDuration;

			_anomalyThresholdOverrideCheckBox = new CheckBox("Override default anomaly threshold")
			{
				IsChecked = options?.AnomalyThreshold != null,
				Tooltip = "Whether to override the default threshold for detecting anomalies. Anomalies are detected when the anomaly score exceeds this threshold. " +
				"With a high threshold less anomalies will be detected, with a low threshold more anomalies will be detected. If checked, the threshold can be set on the right.",
			};
			_anomalyThresholdOverrideCheckBox.Changed += (sender, args) => OnAnomalyThresholdOverrideCheckBoxChanged();

			_anomalyThresholdNumeric = new Numeric()
			{
				Minimum = 0,
				Value = options?.AnomalyThreshold ?? _defaultAnomalyThreshold,
				StepSize = 0.1,
				IsEnabled = options?.AnomalyThreshold != null,
				Tooltip = "The threshold for detecting anomalies.",
				Width = fieldWidth,
			};
			_anomalyThresholdNumeric.Changed += (sender, args) => OnAnomalyThresholdNumericChanged();

			_minimalDurationOverrideCheckBox = new CheckBox("Override default minimum anomaly duration (in minutes)")
			{
				IsChecked = options?.MinimalDuration != null,
				Tooltip = "Whether to override the default duration an anomaly must last before a suggestion event is generated. Note that changing this duration will also have an " +
				"effect on the anomaly score. If checked, the duration can be set on the right.",
			};
			_minimalDurationOverrideCheckBox.Changed += (sender, args) => OnMinimalDurationOverrideCheckBoxChanged();
			
			_minimalDurationTime = new Time()
			{
				HasSeconds = false,
				Minimum = TimeSpan.FromMinutes(0),
				TimeSpan = options?.MinimalDuration != null ? TimeSpan.FromMinutes(options.MinimalDuration.Value) : TimeSpan.FromMinutes(_defaultMinimalDuration),
				IsEnabled = options?.MinimalDuration != null,
				Width = fieldWidth,
			};
			_minimalDurationTime.Changed += (sender, args) => OnMinimalDurationTimeChanged();

			UpdateAnomalyThresholdNumericValidationState();
			UpdateMinimalDurationTimeValidationState();
			UpdateIsValid();

			int row = 0;
			AddWidget(_anomalyThresholdOverrideCheckBox, row, 0);
			AddWidget(_anomalyThresholdNumeric, row, 1, 1, columnCount - 1);
			++row;

			AddWidget(_minimalDurationOverrideCheckBox, row, 0);
			AddWidget(_minimalDurationTime, row, 1, 1, columnCount - 1);
		}

		public event EventHandler Changed;

		public event EventHandler ValidationChanged;

		public double? AnomalyThreshold
		{
			get
			{
				if (_anomalyThresholdOverrideCheckBox.IsChecked)
					return _anomalyThresholdNumeric.Value;
				else
					return null;
			}
		}

		public int? MinimalDuration
		{
			get
			{
				if (_minimalDurationOverrideCheckBox.IsChecked)
					return (int)_minimalDurationTime.TimeSpan.TotalMinutes;
				else
					return null;
			}
		}

		public bool IsValid => _isValid;

		private void UpdateAnomalyThresholdNumericValidationState()
		{
			if (_anomalyThresholdNumeric.Value > 0)
			{
				_anomalyThresholdNumeric.ValidationState = UIValidationState.Valid;
				_anomalyThresholdNumeric.ValidationText = string.Empty;
			}
			else
			{
				_anomalyThresholdNumeric.ValidationState = UIValidationState.Invalid;
				_anomalyThresholdNumeric.ValidationText = "Anomaly threshold must be greater than 0";
			}
		}

		private void UpdateMinimalDurationTimeValidationState()
		{
			if (_minimalDurationTime.TimeSpan.TotalMinutes >= MinimumDurationLowerLimit)
			{
				_minimalDurationTime.ValidationState = UIValidationState.Valid;
				_minimalDurationTime.ValidationText = string.Empty;
			}
			else
			{
				_minimalDurationTime.ValidationState = UIValidationState.Invalid;
				_minimalDurationTime.ValidationText = $"Minimum anomaly duration must be at least {MinimumDurationLowerLimit} minutes";
			}
		}

		private void UpdateIsValid()
		{
			bool isValid;
			if (_anomalyThresholdOverrideCheckBox.IsChecked && _anomalyThresholdNumeric.ValidationState != UIValidationState.Valid)
				isValid = false;
			else if (_minimalDurationOverrideCheckBox.IsChecked && _minimalDurationTime.ValidationState != UIValidationState.Valid)
				isValid = false;
			else
				isValid = true;

			if (_isValid != isValid)
			{
				_isValid = isValid;
				ValidationChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		private void OnAnomalyThresholdOverrideCheckBoxChanged()
		{
			_anomalyThresholdNumeric.IsEnabled = _anomalyThresholdOverrideCheckBox.IsChecked;
			if (!_anomalyThresholdOverrideCheckBox.IsChecked)
			{
				_anomalyThresholdNumeric.Value = _defaultAnomalyThreshold;
				UpdateAnomalyThresholdNumericValidationState();
			}

			UpdateIsValid();
			Changed?.Invoke(this, EventArgs.Empty);
		}

		private void OnAnomalyThresholdNumericChanged()
		{
			UpdateAnomalyThresholdNumericValidationState();
			UpdateIsValid();
			Changed?.Invoke(this, EventArgs.Empty);
		}

		private void OnMinimalDurationOverrideCheckBoxChanged()
		{
			_minimalDurationTime.IsEnabled = _minimalDurationOverrideCheckBox.IsChecked;
			if (!_minimalDurationOverrideCheckBox.IsChecked)
			{
				_minimalDurationTime.TimeSpan = TimeSpan.FromMinutes(_defaultMinimalDuration);
				UpdateMinimalDurationTimeValidationState();
			}

			Changed?.Invoke(this, EventArgs.Empty);
		}

		private void OnMinimalDurationTimeChanged()
		{
			UpdateMinimalDurationTimeValidationState();
			UpdateIsValid();
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}
}
