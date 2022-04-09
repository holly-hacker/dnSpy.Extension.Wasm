namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

public abstract class IntermediateInstruction
{
	public abstract int GetStackPushCount();
	public abstract int GetStackPopCount();
}
