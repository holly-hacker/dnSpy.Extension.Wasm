namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

public class EndOfFunction : IntermediateInstruction
{
	public override int GetStackPushCount() => 0;
	public override int GetStackPopCount() => 0; // TODO: may want to pop ret values
}
