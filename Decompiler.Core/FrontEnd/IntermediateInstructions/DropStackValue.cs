namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

/// <summary>
/// Pops a value from the stack and voids it.
/// </summary>
public class DropStackValue : IntermediateInstruction
{
	public override int GetStackPushCount() => 0;
	public override int GetStackPopCount() => 1;
}
