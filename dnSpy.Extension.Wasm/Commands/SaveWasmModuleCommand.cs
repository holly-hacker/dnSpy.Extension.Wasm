using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using dnSpy.Extension.Wasm.TreeView;
using Microsoft.Win32;

namespace dnSpy.Extension.Wasm.Commands;

[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID,
	Header = "Save WASM module",
	Icon = DsImagesAttribute.Save,
	Group = WasmMenuConstants.GroupAppMenuWasm,
	Order = 0)]
public class SaveWasmModuleCommand : MenuItemBase
{
	private readonly IDocumentTreeView _documentTreeView;

	[ImportingConstructor]
	public SaveWasmModuleCommand(IDocumentTreeView documentTreeView)
	{
		_documentTreeView = documentTreeView;
	}

	public override bool IsVisible(IMenuItemContext context)
	{
		return _documentTreeView.TreeView.TopLevelSelection
			.Any(n => n.GetAncestorOrSelf<WasmDocumentNode>()?.Document is not null);
	}

	public override void Execute(IMenuItemContext context)
	{
		var documents = _documentTreeView.TreeView.TopLevelSelection
			.Select(n => n.GetAncestorOrSelf<WasmDocumentNode>()?.Document)
			.Where(n => n is not null)
			.Distinct()
			.Select(n => n!) // ugh
			.ToArray();

		foreach (var document in documents)
		{
			var module = document.Module;

			var sfd = new SaveFileDialog
			{
				Title = "Save WASM module",
				FileName = Path.GetFileName(document.Filename),
				InitialDirectory = Path.GetDirectoryName(document.Filename),
				Filter = "WebAssembly modules|*.wasm|All files|*",
			};

			if (sfd.ShowDialog() == true)
			{
				using var openFile = sfd.OpenFile();
				module.WriteToBinary(openFile);
			}
		}
	}
}
