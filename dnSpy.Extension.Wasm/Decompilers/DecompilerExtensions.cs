using dnSpy.Extension.Wasm.TreeView;

namespace dnSpy.Extension.Wasm.Decompilers;

internal static class DecompilerExtensions
{
	public static void DecompileByFunctionIndex(this IWasmDecompiler decompiler, WasmDocument doc, DecompilerWriter writer, int sectionIndex)
	{
		string functionName = doc.GetFunctionNameFromSectionIndex(sectionIndex);
		var code = doc.Module.Codes[sectionIndex];
		var function = doc.Module.Functions[sectionIndex];
		var type = doc.Module.Types[(int)function.Type];
		var globalIndex = doc.ImportedFunctionCount + sectionIndex;
		decompiler.Decompile(doc, writer, functionName, code.Locals, code.Code, type, globalIndex);
	}
}
