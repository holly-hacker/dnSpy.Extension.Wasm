namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

/// <summary>
/// Duplicates the value on top of the stack.
/// </summary>
public class DuplicateStackValue : IntermediateInstruction
{
	public override int GetStackPushCount() => 2;
	public override int GetStackPopCount() => 1;
}
