using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using dnSpy.Contracts.ToolBars;
using dnSpy.Extension.Wasm.Decompilers;

namespace dnSpy.Extension.Wasm.Commands;

[ExportToolBarObject(OwnerGuid = ToolBarConstants.APP_TB_GUID, Group = WasmMenuConstants.GroupToolBarWasm, Order = 1)]
internal class WasmDecompilerComboBoxToolbarCommand : ToolBarObjectBase, INotifyPropertyChanged
{
	private readonly IWasmDecompilerService _decompilerService;
	private readonly ComboBox _comboBox;
	private object? _selectedItem;

	[ImportingConstructor]
	public WasmDecompilerComboBoxToolbarCommand(IWasmDecompilerService decompilerService)
	{
		_decompilerService = decompilerService;
		UpdateSelectedItem();
		_comboBox = new ComboBox {
			DisplayMemberPath = nameof(IWasmDecompiler.Name),
			Width = 90,
			ItemsSource = _decompilerService.AllDecompilers,
		};
		_comboBox.SetBinding(Selector.SelectedItemProperty, new Binding(nameof(SelectedItem)) {
			Source = this,
		});
		// decompilerService.DecompilerChanged += DecompilerService_DecompilerChanged;
	}

	public object? SelectedItem {
		get => _selectedItem;
		set
		{
			if (_selectedItem == value) return;

			_selectedItem = value;
			_decompilerService.SetCurrentDecompiler((value as IWasmDecompiler)!);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
		}
	}

	private void UpdateSelectedItem() => SelectedItem = _decompilerService.CurrentDecompiler;

	public override object GetUIObject(IToolBarItemContext context, IInputElement? commandTarget) => _comboBox;

	public event PropertyChangedEventHandler? PropertyChanged;
}
