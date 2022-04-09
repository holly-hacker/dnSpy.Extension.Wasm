namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

public class StoreVariable : IntermediateInstruction
{
	public StoreVariable(IVariable variable)
	{
		Variable = variable;
	}

	public IVariable Variable { get; }

	public override int GetStackPushCount() => 0;
	public override int GetStackPopCount() => 1;
}
