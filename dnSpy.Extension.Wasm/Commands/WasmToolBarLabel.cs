using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.ToolBars;

namespace dnSpy.Extension.Wasm.Commands;

[ExportToolBarObject(OwnerGuid = ToolBarConstants.APP_TB_GUID, Group = WasmMenuConstants.GroupToolBarWasm, Order = 0)]
public class WasmToolBarLabel : ToolBarObjectBase
{
	private readonly Label _label;

	public WasmToolBarLabel()
	{
		_label = new Label { Content = "WebAssembly:" };
	}

	public override object GetUIObject(IToolBarItemContext context, IInputElement? commandTarget)
	{
		return _label;
	}
}
