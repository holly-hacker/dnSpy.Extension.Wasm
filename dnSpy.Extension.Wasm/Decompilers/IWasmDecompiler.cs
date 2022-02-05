using dnSpy.Contracts.Documents.Tabs.DocViewer;
using WebAssembly;

namespace dnSpy.Extension.Wasm.Decompilers;

public interface IWasmDecompiler
{
	void Decompile(IDecompileNodeContext context, int index, FunctionBody code, WebAssemblyType type);
}
