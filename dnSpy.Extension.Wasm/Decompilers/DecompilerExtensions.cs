using dnSpy.Extension.Wasm.TreeView;

namespace dnSpy.Extension.Wasm.Decompilers;

internal static class DecompilerExtensions
{
	public static void DecompileByFunctionIndex(this IWasmDecompiler decompiler, WasmDocument doc, DecompilerWriter writer, int index)
	{
		string functionName = doc.GetFunctionNameFromSectionIndex(index);
		var code = doc.Module.Codes[index];
		var function = doc.Module.Functions[index];
		var type = doc.Module.Types[(int)function.Type];
		decompiler.Decompile(doc, writer, functionName, code.Locals, code.Code, type, index);
	}
}
