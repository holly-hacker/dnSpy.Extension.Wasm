using dnSpy.Contracts.Documents.Tabs.DocViewer;

namespace dnSpy.Extension.Wasm.TreeView;

internal abstract class HexViewerNode : WasmDocumentTreeNodeData, IDecompileSelf
{
	protected HexViewerNode(WasmDocument document) : base(document)
	{
	}

	public bool Decompile(IDecompileNodeContext context) => false;

	public abstract string GetName();
	public abstract byte[] GetHexData();
}
