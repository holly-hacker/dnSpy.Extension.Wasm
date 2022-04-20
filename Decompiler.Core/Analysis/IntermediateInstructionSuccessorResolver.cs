using System;
using Echo.ControlFlow;
using Echo.ControlFlow.Construction;
using Echo.ControlFlow.Construction.Static;
using Echo.Core.Code;
using HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

namespace HoLLy.Decompiler.Core.Analysis;

public class IntermediateInstructionSuccessorResolver : IStaticSuccessorResolver<IntermediateInstruction>
{
	private readonly IInstructionSetArchitecture<IntermediateInstruction> _architecture;

	public IntermediateInstructionSuccessorResolver(IInstructionSetArchitecture<IntermediateInstruction> architecture)
	{
		_architecture = architecture;
	}

	public int GetSuccessorsCount(in IntermediateInstruction instruction)
	{
		return instruction switch
		{
			Jump { Conditional: true } => 2,
			TrapInstruction or EndOfFunction => 0,
			_ => 1,
		};
	}

	public int GetSuccessors(in IntermediateInstruction instruction, Span<SuccessorInfo> successorsBuffer)
	{
		switch (instruction)
		{
			case Jump j:
			{
				long offset = _architecture.GetOffset(instruction);

				if (j.Conditional)
				{
					successorsBuffer[0] = new SuccessorInfo(
						offset + 1,
						ControlFlowEdgeType.FallThrough);
					successorsBuffer[1] = new SuccessorInfo(
						_architecture.GetOffset(j.Target!),
						ControlFlowEdgeType.Conditional);
				}
				else
				{
					successorsBuffer[0] = new SuccessorInfo(
						_architecture.GetOffset(j.Target!),
						ControlFlowEdgeType.Unconditional);
				}

				return j.Conditional ? 2 : 1;
			}
			case TrapInstruction:
			case EndOfFunction:
				return 0;
			default:
			{
				long offset = _architecture.GetOffset(instruction);
				successorsBuffer[0] = new SuccessorInfo(offset + 1, ControlFlowEdgeType.FallThrough);
				return 1;
			}
		}
	}
}
