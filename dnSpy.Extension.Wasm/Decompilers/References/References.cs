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
