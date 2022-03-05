using System.Linq;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Extension.Wasm.TreeView;
using WebAssembly;

namespace dnSpy.Extension.Wasm.Decompilers.References;

[ExportDocumentTreeNodeDataFinder]
internal class WasmTreeNodeDataFinder : IDocumentTreeNodeDataFinder
{
	public DocumentTreeNodeData? FindNode(IDocumentTreeView documentTreeView, object? @ref)
	{
		if (@ref is not IWasmReference)
			return null;

		// may not be ideal, what if the user has another item selected somehow?
		var selectedItem = documentTreeView.TreeView.SelectedItem;

		if (selectedItem.GetTopNode() is not WasmDocumentNode documentNode)
			return null;

		var document = documentNode.Document;

		switch (@ref)
		{
			case FunctionReference funcRef:
			{
				documentNode.TreeNode.EnsureChildrenLoaded();

				var import = document.TryGetImport<Import.Function>(funcRef.GlobalFunctionIndex, document.ImportedFunctionCount, out int sectionIndex);

				// check if function is an import
				if (import is not null)
				{
					foreach (var node in documentNode.TreeNode.Descendants().Where(d => d.Data is ImportsNode))
						node.EnsureChildrenLoaded();

					// relying on Import.Function being reference equal, which is not ideal
					// alternative would be storing an index in the FunctionImportNode which is a lot better
					return documentNode.TreeNode
						.Descendants()
						.Select(d => d.Data).OfType<FunctionImportNode>()
						.FirstOrDefault(f => f.Function == import);
				}
				else
				{
					foreach (var node in documentNode.TreeNode.Descendants().Where(d => d.Data is FunctionsNode))
						node.EnsureChildrenLoaded();

					return documentNode.TreeNode
						.Descendants()
						.Select(d => d.Data).OfType<FunctionNode>()
						.FirstOrDefault(f => f.Index == sectionIndex);
				}
			}
		}

		return null;
	}
}
