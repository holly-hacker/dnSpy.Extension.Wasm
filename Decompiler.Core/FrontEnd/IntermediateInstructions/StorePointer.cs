namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

/// <summary>
/// Writes to an arbitrary memory location.
/// </summary>
/// <remarks>
/// The top value on the stack must be the value to write, and the second value must contain the address.
/// </remarks>
public class StorePointer : IntermediateInstruction
{
	public StorePointer(DataType dataType, int size)
	{
		DataType = dataType;
		Size = size;
	}

	public DataType DataType { get; }
	public int Size { get; }

	public override int GetStackPushCount() => 0;
	public override int GetStackPopCount() => 2;
}
