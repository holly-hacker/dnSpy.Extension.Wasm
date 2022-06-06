namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

/// <summary>
/// Pushes a value from a global, managed variable (defined out of the scope of the current function) onto the stack.
/// </summary>
/// <remarks>This should not be used for raw pointers, see <see cref="LoadPointer"/> instead.</remarks>
public class LoadGlobal : IntermediateInstruction
{
	public LoadGlobal(IVariable variable)
	{
		Variable = variable;
	}

	public IVariable Variable { get; }

	public override int GetStackPushCount() => 1;
	public override int GetStackPopCount() => 0;
}
