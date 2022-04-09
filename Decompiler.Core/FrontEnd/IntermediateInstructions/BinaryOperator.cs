namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

public class BinaryOperator : IntermediateInstruction
{
	public BinaryOperator(BinaryOperationType type, DataType input1, DataType input2, DataType output)
	{
		Type = type;
		Input1 = input1;
		Input2 = input2;
		Output = output;
	}

	public BinaryOperationType Type { get; }
	public DataType Input1 { get; }
	public DataType Input2 { get; }
	public DataType Output { get; }

	public override int GetStackPushCount() => 1;
	public override int GetStackPopCount() => 2;
}

public enum BinaryOperationType
{
	Add,
	Sub,
	Mul,
	Div,
	DivS,
	Rem,
	RemS,
	And,
	Or,
	Xor,
	Shl,
	ShrZeroExtend,
	ShrSignExtend,
	Rotl,
	Rotr,
	Equal,
	NotEqual,
	LessThan,
	LessThanSigned,
	GreaterThan,
	GreaterThanSigned,
	LessThanOrEqual,
	LessThanOrEqualSigned,
	GreaterThanOrEqual,
	GreaterThanOrEqualSigned,
}
