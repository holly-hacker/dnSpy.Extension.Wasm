using WebAssembly;

namespace dnSpy.Extension.Wasm.Decompilers.References;

/// <summary>
/// Marker interface to ensure <see cref="ReferenceDocumentTabContentProvider"/> is not too eager.
/// </summary>
internal interface IWasmReference { }

internal class FunctionReference : IWasmReference
{
	public FunctionReference(int globalFunctionIndex)
	{
		GlobalFunctionIndex = globalFunctionIndex;
	}

	public int GlobalFunctionIndex { get; }
}

public class LocalReference : IWasmReference
{
	public LocalReference(string name, WebAssemblyValueType type, int index, bool isArgument)
	{
		Name = name;
		Type = type;
		Index = index;
		IsArgument = isArgument;
	}

	public string Name { get; set; }
	public WebAssemblyValueType Type { get; set; }

	public int Index { get; }
	public bool IsArgument { get; }
}

public class GlobalReference : IWasmReference
{
	public GlobalReference(string name, WebAssemblyValueType type, bool mutable, int index)
	{
		Name = name;
		Type = type;
		Mutable = mutable;
		Index = index;
	}

	public string Name { get; set; }
	public WebAssemblyValueType Type { get; set; }
	public bool Mutable { get; set; }

	public int Index { get; }
}
