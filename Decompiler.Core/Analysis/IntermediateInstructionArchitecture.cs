using System;
using System.Collections.Generic;
using Echo.Core.Code;
using HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;
using IVariable = HoLLy.Decompiler.Core.FrontEnd.IVariable;
using IEchoVariable = Echo.Core.Code.IVariable;

namespace HoLLy.Decompiler.Core.Analysis;

internal class IntermediateInstructionArchitecture : IInstructionSetArchitecture<IntermediateInstruction>, IStaticInstructionProvider<IntermediateInstruction>
{
	private readonly IList<IntermediateInstruction> _instructions;

	public IntermediateInstructionArchitecture(IList<IntermediateInstruction> instructions)
	{
		_instructions = instructions;
	}

	public IInstructionSetArchitecture<IntermediateInstruction> Architecture => this;

	public long GetOffset(in IntermediateInstruction instruction) => _instructions.IndexOf(instruction);

	public int GetSize(in IntermediateInstruction instruction) => 1;

	public InstructionFlowControl GetFlowControl(in IntermediateInstruction instruction)
	{
		return instruction switch
		{
			EndOfFunction => InstructionFlowControl.IsTerminator,
			TrapInstruction => InstructionFlowControl.IsTerminator,
			Jump => InstructionFlowControl.CanBranch,
			_ => InstructionFlowControl.Fallthrough,
		};
	}

	public int GetStackPushCount(in IntermediateInstruction instruction) => instruction.GetStackPopCount();

	public int GetStackPopCount(in IntermediateInstruction instruction) => instruction.GetStackPopCount();

	public int GetReadVariablesCount(in IntermediateInstruction instruction)
	{
		switch (instruction)
		{
			case LoadLocal:
			case StoreLocal:
				return 1;
			default:
				return 0;
		}
	}

	public int GetReadVariables(in IntermediateInstruction instruction, Span<IEchoVariable> variablesBuffer)
	{
		switch (instruction)
		{
			case LoadLocal lv:
				variablesBuffer[0] = new VariableWrapper(lv.Variable);
				return 1;
			case StoreLocal sv:
				variablesBuffer[0] = new VariableWrapper(sv.Variable);
				return 1;
			default:
				return 0;
		}
	}

	public int GetWrittenVariablesCount(in IntermediateInstruction instruction)
	{
		switch (instruction)
		{
			case StoreLocal:
				return 1;
			default:
				return 0;
		}
	}

	public int GetWrittenVariables(in IntermediateInstruction instruction, Span<IEchoVariable> variablesBuffer)
	{
		switch (instruction)
		{
			case StoreLocal sv:
				variablesBuffer[0] = new VariableWrapper(sv.Variable);
				return 1;
			default:
				return 0;
		}
	}

	public IntermediateInstruction GetInstructionAtOffset(long offset) => _instructions[(int)offset];

	private class VariableWrapper : IEchoVariable
	{
		private readonly IVariable _variable;

		public VariableWrapper(IVariable variable)
		{
			_variable = variable;
		}

		public string Name => "var " + _variable.DataType;
	}
}
