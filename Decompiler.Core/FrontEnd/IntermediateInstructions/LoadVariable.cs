namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

/// <summary>
/// Pushes a value from a variable (such as a register) onto the stack.
/// </summary>
public class LoadVariable : IntermediateInstruction
{
	public LoadVariable(IVariable variable)
	{
		Variable = variable;
	}

	public IVariable Variable { get; }

	public override int GetStackPushCount() => 1;
	public override int GetStackPopCount() => 0;
}
