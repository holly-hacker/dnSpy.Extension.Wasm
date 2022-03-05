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
	public LocalReference(int index, bool isArgument)
	{
		Index = index;
		IsArgument = isArgument;
	}

	public int Index { get; }
	public bool IsArgument { get; }
}

public class GlobalReference : IWasmReference
{
	public GlobalReference(int index)
	{
		Index = index;
	}

	public int Index { get; }
}
