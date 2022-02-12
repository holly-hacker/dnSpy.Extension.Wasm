using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Extension.Wasm.TreeView;
using WebAssembly;

namespace dnSpy.Extension.Wasm.Decompilers;

internal interface IWasmDecompiler
{
	void Decompile(WasmDocument doc, IDecompileNodeContext context, int index, FunctionBody code, WebAssemblyType type);
}
