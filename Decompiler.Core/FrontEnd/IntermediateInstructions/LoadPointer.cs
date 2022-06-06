namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

public class LoadPointer : IntermediateInstruction
{
	public LoadPointer(DataType dataType, int size)
	{
		DataType = dataType;
		Size = size;
	}

	public DataType DataType { get; }
	public int Size { get; }

	public override int GetStackPushCount() => 1;
	public override int GetStackPopCount() => 1;
}
