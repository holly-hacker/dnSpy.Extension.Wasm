using HoLLy.Decompiler.Core.FrontEnd;

namespace dnSpy.Extension.Wasm.Decompiling;

internal class WasmLocalVariable : IVariable
{
	public DataType DataType { get; }

	public WasmLocalVariable(DataType type)
	{
		DataType = type;
	}

	public override string ToString() => $"local {DataType}";
}
