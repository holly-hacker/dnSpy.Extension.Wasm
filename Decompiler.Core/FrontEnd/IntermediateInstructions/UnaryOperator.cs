namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

public class UnaryOperator : IntermediateInstruction
{
	public UnaryOperator(UnaryOperationType type, DataType input, DataType output)
	{
		Type = type;
		Input = input;
		Output = output;
	}

	public UnaryOperationType Type { get; }
	public DataType Input { get; }
	public DataType Output { get; }

	public override int GetStackPushCount() => 1;
	public override int GetStackPopCount() => 1;
}

public enum UnaryOperationType
{
}
