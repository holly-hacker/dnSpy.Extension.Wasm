namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

public class StoreLocal : IntermediateInstruction
{
	public StoreLocal(IVariable variable)
	{
		Variable = variable;
	}

	public IVariable Variable { get; }

	public override int GetStackPushCount() => 0;
	public override int GetStackPopCount() => 1;
}
