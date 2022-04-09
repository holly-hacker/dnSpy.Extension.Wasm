namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

public class TrapInstruction : IntermediateInstruction
{
	public override int GetStackPushCount() => 0;
	public override int GetStackPopCount() => 0;
}
