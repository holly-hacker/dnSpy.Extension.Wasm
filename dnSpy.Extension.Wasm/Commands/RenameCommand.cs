using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using dnSpy.Extension.Wasm.Decompilers.References;
using dnSpy.Extension.Wasm.TreeView;

namespace dnSpy.Extension.Wasm.Commands;

[ExportMenuItem(Header = "Set symbol name",
	Icon = DsImagesAttribute.Edit,
	Group = WasmMenuConstants.GroupCtxMenuWasm,
	Order = 0)]
internal class RenameCommand : MenuItemBase
{
	private readonly IDocumentTreeView _documentTreeView;

	[ImportingConstructor]
	public RenameCommand(IDocumentTreeView documentTreeView)
	{
		_documentTreeView = documentTreeView;
	}

	public override bool IsVisible(IMenuItemContext context)
	{
		if (context.Find<IDocumentViewer>() is null)
			return false;

		var reference = context.Find<TextReference>()?.Reference as IWasmReference;
		return reference is FunctionReference or GlobalReference && GetWasmDocument() is not null;
	}

	public override void Execute(IMenuItemContext context)
	{
		var docViewer = context.Find<IDocumentViewer>();
		var reference = context.Find<TextReference>()?.Reference as IWasmReference;

		var doc = GetWasmDocument();
		if (doc is null)
			return;

		switch (reference)
		{
			case FunctionReference function:
			{
				string originalName = doc.GetFunctionName(function.GlobalFunctionIndex);

				var name = MsgBox.Instance.Ask<string?>("Enter a new function name", originalName, "Rename function");
				if (name == null)
					return;

				// set name
				doc.NameSection ??= new NameSection();
				doc.NameSection.FunctionNames ??= new Dictionary<int, string>();
				doc.NameSection.FunctionNames[function.GlobalFunctionIndex] = name;

				// invalidate document, forces it to reload
				var docTabService = docViewer.DocumentTab?.DocumentTabService;
				docTabService?.RefreshModifiedDocument(doc);

				// re-render all tree view nodes so they use the correct name
				// NOTE: should update just the correct ones individually. this may include import or export nodes
				_documentTreeView.TreeView.RefreshAllNodes();
				break;
			}
		}
	}

	private WasmDocument? GetWasmDocument()
	{
		var documents = _documentTreeView.TreeView.TopLevelSelection
			.Select(n => n.GetAncestorOrSelf<WasmDocumentNode>()?.Document)
			.Where(n => n is not null)
			.Distinct()
			.ToArray();

		return documents.Length == 1 ? documents[0] : null;
	}
}
