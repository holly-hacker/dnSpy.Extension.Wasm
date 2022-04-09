namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

/// <summary>
/// Pushes a value to the program stack.
/// </summary>
public class LoadConstant : IntermediateInstruction
{
	public LoadConstant(IConstantValue value)
	{
		Value = value;
	}

	public IConstantValue Value { get; }

	public override int GetStackPushCount() => 1;
	public override int GetStackPopCount() => 0;
}
