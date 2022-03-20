using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using dnSpy.Contracts.Documents;
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
internal class SaveWasmModuleCommand : MenuItemBase
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
		var selectedDocuments = _documentTreeView.TreeView.TopLevelSelection
			.Select(n => n.GetAncestorOrSelf<WasmDocumentNode>()?.Document)
			.Where(n => n is not null)
			.Distinct()
			.Select(n => n!) // ugh
			.ToArray();

		foreach (var document in selectedDocuments)
		{
			var sfd = new SaveFileDialog
			{
				Title = "Save WASM module",
				FileName = Path.GetFileName(document.Filename),
				InitialDirectory = Path.GetDirectoryName(document.Filename),
				Filter = "WebAssembly modules|*.wasm|All files|*",
			};

			if (sfd.ShowDialog() == true)
			{
				using (var openFile = sfd.OpenFile())
					document.SaveToStream(openFile);

				// if saved to a new file, open and select it
				if (document.Filename != sfd.FileName)
				{
					var openedDocument = _documentTreeView.DocumentService.TryGetOrCreate(DsDocumentInfo.CreateDocument(sfd.FileName));
					if (openedDocument == null)
						continue;

					Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
						var node = _documentTreeView.FindNode(openedDocument);
						if (node is not null)
							_documentTreeView.TreeView.SelectItems(new[] { node });
					}));
				}
			}
		}
	}
}
