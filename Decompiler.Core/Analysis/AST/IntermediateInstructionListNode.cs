using System.Collections.Generic;
using HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

namespace HoLLy.Decompiler.Core.Analysis.AST;

public class IntermediateInstructionListNode : IHighLevelControlFlowNode
{
	public IntermediateInstructionListNode(IList<IntermediateInstruction> instructions)
	{
		Instructions = instructions;
	}

	public IHighLevelControlFlowNode Head => this;
	public IList<IntermediateInstruction> Instructions { get; }

	public override string ToString() => $"{Instructions.Count} instructions;";
}
