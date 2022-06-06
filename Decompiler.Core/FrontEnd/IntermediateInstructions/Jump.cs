using System;

namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

public class Jump : IntermediateInstruction
{
	public Jump(bool conditional, IntermediateInstruction? targetInstruction = null)
	{
		Conditional = conditional;
		Target = targetInstruction;
	}

	public bool Conditional { get; }
	public IntermediateInstruction? Target { get; private set; }

	public override int GetStackPushCount() => 0;
	public override int GetStackPopCount() => Conditional ? 1 : 0;

	public void SetTarget(IntermediateInstruction i)
	{
		if (Target != null)
			throw new InvalidOperationException("Jump instruction already has a target");

		Target = i;
	}
}
