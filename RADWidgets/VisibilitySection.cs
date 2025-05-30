namespace RadWidgets
{
	using System.Collections.Generic;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class VisibilitySection : Section
	{
		private readonly List<Widget> _childWidgets = new List<Widget>();
		private readonly List<Section> _childSections = new List<Section>();

		public override bool IsVisible
		{
			get => IsSectionVisible;
			set
			{
				if (IsSectionVisible == value)
					return;

				IsSectionVisible = value;

				foreach (var child in _childWidgets)
					child.IsVisible = value;

				foreach (var child in _childSections)
					child.IsVisible = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the section itself is visible. Changing this property will not show or hide any of the child widgets or sections.
		/// </summary>
		protected bool IsSectionVisible { get; set; } = true;

		public new void AddSection(Section section, ILayout layout)
		{
			_childSections.Add(section);
			base.AddSection(section, layout);
		}

		public new void AddSection(Section section, int row, int column)
		{
			_childSections.Add(section);
			base.AddSection(section, row, column);
		}

		public new void AddWidget(Widget widget, IWidgetLayout layout)
		{
			_childWidgets.Add(widget);
			base.AddWidget(widget, layout);
		}

		public new void AddWidget(
			Widget widget,
			int row,
			int column,
			HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment verticalAlignment = VerticalAlignment.Center)
		{
			_childWidgets.Add(widget);
			base.AddWidget(widget, row, column, horizontalAlignment, verticalAlignment);
		}

		public new void AddWidget(
			Widget widget,
			int row,
			int column,
			int rowSpan,
			int columnSpan,
			HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment verticalAlignment = VerticalAlignment.Center)
		{
			_childWidgets.Add(widget);
			base.AddWidget(widget, row, column, rowSpan, columnSpan, horizontalAlignment, verticalAlignment);
		}
	}
}
