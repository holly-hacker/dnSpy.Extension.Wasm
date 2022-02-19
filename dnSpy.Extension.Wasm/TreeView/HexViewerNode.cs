using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.Extension.Wasm.TreeView;

public abstract class HexViewerNode : DocumentTreeNodeData, IDecompileSelf
{
	public bool Decompile(IDecompileNodeContext context) => false;

	public abstract string GetName();
	public abstract byte[] GetHexData();
}
