namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.AutomationUI.Objects;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	/// <summary>
	/// Abstract class for items that can be selected in a <see cref="MultiSelector{T}" /> widget.
	/// </summary>
	public abstract class MultiSelectorItem : Section
	{
		/// <summary>
		/// Gets the key of the item. This key is used to uniquely identify an item: no two items with the same key can be selected at the same time.
		/// </summary>
		/// <returns>The key.</returns>
		public abstract string GetKey();

		/// <summary>
		/// Gets the display value of the item. This value is shown in the widget.
		/// </summary>
		/// <returns>The display value.</returns>
		public abstract string GetDisplayValue();
	}

	/// <summary>
	/// Selector widget to select a single item in a MultiSelector widget.
	/// </summary>
	/// <typeparam name="T">The type of the items that can be selected.</typeparam>
	public abstract class MultiSelectorItemSelector<T> : Section where T : MultiSelectorItem
	{
		/// <summary>
		/// Gets the selected item in the widget.
		/// </summary>
		public abstract T SelectedItem { get; }
	}

	/// <summary>
	/// Widget allowing to select multiple items of a certain type. The number of columns and rows the widget takes up is determined by the itemSelector:
	/// it will take (itemSelector.RowCount + 2) rows and (itemSelector.ColumnCount + 1) columns.
	/// </summary>
	/// <typeparam name="T">The type of the items that can be selected.</typeparam>
	public abstract class MultiSelector<T> : VisibilitySection where T : MultiSelectorItem
	{
		private readonly MultiSelectorItemSelector<T> _itemSelector;
		private readonly Label _noItemsSelectedLabel;
		private readonly TreeView _selectedItemsView;
		private readonly Dictionary<string, T> _selectedItems = new Dictionary<string, T>();
		private readonly Button _addButton;
		private readonly Button _removeButton;

		/// <summary>
		/// Initializes a new instance of the <see cref="MultiSelector{T}"/> class.
		/// </summary>
		/// <param name="itemSelector">The widget to select a single item.</param>
		/// <param name="noItemsSelectedText">Text to show when no items are selected.</param>
		/// <param name="selectedItems">The initially selected items, or null.</param>
		protected MultiSelector(MultiSelectorItemSelector<T> itemSelector, List<T> selectedItems = null, string noItemsSelectedText = "") : base()
		{
			_itemSelector = itemSelector;

			_noItemsSelectedLabel = new Label(noItemsSelectedText)
			{
				MinHeight = 100,
			};

			_selectedItemsView = new TreeView(new List<TreeViewItem>())
			{
				IsReadOnly = false,
				MinHeight = 100,
			};
			SetSelected(selectedItems);

			_addButton = new Button("Add");
			_addButton.Pressed += AddButton_Pressed;

			_removeButton = new Button("Remove");
			_removeButton.Pressed += RemoveButton_Pressed;

			Changed += (sender, args) => OnChanged();
			OnChanged();

			int row = 0;
			AddSection(_itemSelector, row, 0);
			AddWidget(_addButton, row + _itemSelector.RowCount - 1, _itemSelector.ColumnCount);
			row += _itemSelector.RowCount;

			AddWidget(_noItemsSelectedLabel, row, 0, 1, _itemSelector.ColumnCount);
			AddWidget(_selectedItemsView, row + 1, 0, 2, _itemSelector.ColumnCount);
			AddWidget(_removeButton, row, _itemSelector.ColumnCount, 2, 1, verticalAlignment: VerticalAlignment.Top);
		}

		/// <summary>
		/// Emitted when an item has been added or removed
		/// </summary>
		public event EventHandler Changed;

		/// <summary>
		/// Gets or sets the tooltip of the add button.
		/// </summary>
		public string AddButtonTooltip
		{
			get => _addButton.Tooltip;
			set => _addButton.Tooltip = value;
		}

		/// <summary>
		/// Gets or sets the tooltip of the remove button.
		/// </summary>
		public string RemoveButtonTooltip
		{
			get => _removeButton.Tooltip;
			set => _removeButton.Tooltip = value;
		}

		/// <summary>
		/// Gets or sets the text shown when no items are selected.
		/// </summary>
		public string NoItemsSelectedText
		{
			get => _noItemsSelectedLabel.Text;
			set
			{
				_noItemsSelectedLabel.Text = value;
			}
		}

		/// <inheritdoc />
		public override bool IsVisible
		{
			// Note: we had to override this, since otherwise all child widgets are made visible when this is set to true.
			get => IsSectionVisible;
			set
			{
				if (value == IsSectionVisible)
					return;

				IsSectionVisible = value;

				_itemSelector.IsVisible = value;
				_addButton.IsVisible = value;
				_removeButton.IsVisible = value;
				UpdateTreeViewVisibility();
			}
		}

		/// <summary>
		/// Gets the widget to select a single item.
		/// </summary>
		protected MultiSelectorItemSelector<T> ItemSelector => _itemSelector;

		/// <summary>
		/// Get the currently selected items.
		/// </summary>
		/// <returns>The currently selected items.</returns>
		public IEnumerable<T> GetSelected()
		{
			return _selectedItems.Values;
		}

		/// <summary>
		/// Sets the selected items.
		/// </summary>
		/// <param name="selected">The new selected items (empty or null if none are selected).</param>
		public void SetSelected(IEnumerable<T> selected)
		{
			_selectedItems.Clear();

			if (selected == null)
			{
				_selectedItemsView.Items = new List<TreeViewItem>();
				Changed?.Invoke(this, EventArgs.Empty);
				return;
			}

			foreach (var item in selected)
			{
				var key = item.GetKey();
				if (!_selectedItems.ContainsKey(key))
					_selectedItems.Add(key, item);
			}

			_selectedItemsView.Items = _selectedItems.Select(p => new TreeViewItem(p.Value.GetDisplayValue(), p.Key)).ToList();
			Changed?.Invoke(this, EventArgs.Empty);
		}

		protected virtual bool AddItem(T item)
		{
			string key = item.GetKey();
			if (_selectedItems.ContainsKey(key))
				return false;

			_selectedItems.Add(key, item);
			var treeViewItem = new TreeViewItem(item.GetDisplayValue(), key);
			_selectedItemsView.Items = _selectedItemsView.Items.Concat(new List<TreeViewItem>() { treeViewItem }).ToList();
			Changed?.Invoke(this, EventArgs.Empty);

			return true;
		}

		private void UpdateTreeViewVisibility()
		{
			if (!IsSectionVisible)
			{
				_noItemsSelectedLabel.IsVisible = false;
				_selectedItemsView.IsVisible = false;
			}
			else if (_selectedItems.Count == 0)
			{
				_noItemsSelectedLabel.IsVisible = true;
				_selectedItemsView.IsVisible = false;
			}
			else
			{
				_noItemsSelectedLabel.IsVisible = false;
				_selectedItemsView.IsVisible = true;
			}
		}

		private void OnChanged()
		{
			UpdateTreeViewVisibility();
			_removeButton.IsEnabled = _selectedItems.Count > 0;
		}

		private void AddButton_Pressed(object sender, EventArgs e)
		{
			var selectedItem = _itemSelector.SelectedItem;
			if (selectedItem == null)
				return;

			AddItem(selectedItem);
		}

		private void RemoveButton_Pressed(object sender, EventArgs e)
		{
			var newItems = _selectedItemsView.Items.Where(i => !i.IsChecked).ToList();
			if (_selectedItemsView.Items.Count() != newItems.Count)
			{
				foreach (var item in _selectedItemsView.Items.Where(i => i.IsChecked))
					_selectedItems.Remove(item.KeyValue);

				_selectedItemsView.Items = newItems;
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}