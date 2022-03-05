using System.Collections.Generic;
using dnSpy.Extension.Wasm.TreeView;
using WebAssembly;

namespace dnSpy.Extension.Wasm.Decompilers;

internal interface IWasmDecompiler
{
	void Decompile(WasmDocument doc, DecompilerWriter writer, string name, IList<Local> locals, IList<Instruction> code,
		WebAssemblyType functionType, int? globalIndex = null);
}
