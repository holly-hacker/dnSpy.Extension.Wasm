using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.Extension.Wasm.TreeView;

internal abstract class WasmDocumentTreeNodeData : DocumentTreeNodeData
{
	public WasmDocument Document { get; }

	public WasmDocumentTreeNodeData(WasmDocument document)
	{
		Document = document;
	}
}
