using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnSpy.Extension.Wasm.TreeView;
using HoLLy.Decompiler.Core.FrontEnd;
using HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;
using WebAssembly;
using WebAssembly.Instructions;

namespace dnSpy.Extension.Wasm.Decompiling;

internal class WasmDecompilerFrontend : IDecompilerFrontend
{
	private readonly WasmDocument _document;
	private readonly IList<Local> _locals;
	private readonly IList<Instruction> _code;
	private readonly WebAssemblyType _functionType;

	public WasmDecompilerFrontend(WasmDocument document, IList<Local> locals, IList<Instruction> code, WebAssemblyType functionType)
	{
		_document = document;
		_locals = locals;
		_code = code;
		_functionType = functionType;
	}

	public IList<IntermediateInstruction> Convert()
	{
		static DataType WasmTypeToDecompilerType(WebAssemblyValueType wasmType)
		{
			return wasmType switch
			{
				WebAssemblyValueType.Int32 => DataType.I32,
				WebAssemblyValueType.Int64 => DataType.I64,
				WebAssemblyValueType.Float32 => DataType.F32,
				WebAssemblyValueType.Float64 => DataType.F64,
				_ => throw new ArgumentOutOfRangeException(),
			};
		}

		var instructions = new List<IntermediateInstruction>();
		var variables = new List<WasmLocalVariable>();

		foreach (var parameterType in _functionType.Parameters)
		{
			var type = WasmTypeToDecompilerType(parameterType);
			variables.Add(new WasmLocalVariable(type));
		}

		foreach (var local in _locals)
		{
			for (var i = 0; i < local.Count; i++)
			{
				var type = local.Type switch
				{
					WebAssemblyValueType.Int32 => DataType.I32,
					WebAssemblyValueType.Int64 => DataType.I64,
					WebAssemblyValueType.Float32 => DataType.F32,
					WebAssemblyValueType.Float64 => DataType.F64,
					_ => throw new ArgumentOutOfRangeException(),
				};
				variables.Add(new WasmLocalVariable(type));
			}
		}

		// wasm labels that branching instructions may jump to
		var labels = new Stack<LabelInfo>();

		// queued labels that need to be pushed to the stack
		var labelsToAdd = new Queue<LabelType>();

		// jumps that want their target to be the next instruction
		var jumpsToAdd = new Queue<Jump>();

		void AddInstruction(IntermediateInstruction i)
		{
			while (labelsToAdd.Any())
				labels.Push(new LabelInfo(i, labelsToAdd.Dequeue()));

			while (jumpsToAdd.Any())
				jumpsToAdd.Dequeue().SetTarget(i);

			instructions.Add(i);
		}

		LabelInfo GetLabel(uint i) => labels.ElementAt((int)i);

		// before doing anything, create a label for the function scope
		// this is used so return instructions have something to jump to
		labelsToAdd.Enqueue(LabelType.Block);

		for (var instructionIndex = 0; instructionIndex < _code.Count; instructionIndex++)
		{
			var wasmInstruction = _code[instructionIndex];
			switch (wasmInstruction)
			{
				case Unreachable:
					AddInstruction(new TrapInstruction());
					break;
				case NoOperation:
					break;
				#region Control
				case Block block:
				{
					if (block.Type != BlockType.Empty)
						throw new NotImplementedException("BlockType is not empty for Block");

					labelsToAdd.Enqueue(LabelType.Block);
					break;
				}
				case Loop loop:
				{
					if (loop.Type != BlockType.Empty)
						throw new NotImplementedException("BlockType is not empty for Loop");

					labelsToAdd.Enqueue(LabelType.Loop);
					break;
				}
				case If @if:
				{
					if (@if.Type != BlockType.Empty)
						throw new NotImplementedException("BlockType is not empty for If");

					labelsToAdd.Enqueue(LabelType.If);
					var jump = new Jump(true);
					AddInstruction(jump);
					labels.Peek().JumpsToEnd.Enqueue(jump);
					break;
				}
				// TODO: Else
				case End:
				{
					// TODO: can throw if a label is queued but not added yet (eg fn {nop; end;})
					var label = labels.Pop();

					if (label.Type == LabelType.Else)
						throw new NotImplementedException("End for Else block");

					foreach (var jump in label.JumpsToEnd)
						jumpsToAdd.Enqueue(jump);

					// add pseudo instruction to mark end of function
					if (instructionIndex == _code.Count - 1)
					{
						// end of function, expecting just 1 remaining block label
						Debug.Assert(!labels.Any());
						Debug.Assert(label.Type == LabelType.Block);

						AddInstruction(new EndOfFunction());
					}
					break;
				}
				case Branch branch:
				{
					var label = GetLabel(branch.Index);

					switch (label.Type)
					{
						case LabelType.Loop:
							AddInstruction(new Jump(false, label.StartInstruction));
							break;
						case LabelType.Block:
						case LabelType.If:
							var jump = new Jump(false);
							AddInstruction(jump);
							label.JumpsToEnd.Enqueue(jump);
							break;
						case LabelType.Else:
							throw new NotImplementedException("branch from within else block");
						default:
							throw new ArgumentOutOfRangeException(nameof(label.Type), label.Type, $"Unknown label type: {label.Type}");
					}
					break;
				}
				case BranchIf branchIf:
				{
					var label = GetLabel(branchIf.Index);

					switch (label.Type)
					{
						case LabelType.Loop:
							AddInstruction(new Jump(true, label.StartInstruction));
							break;
						case LabelType.Block:
						case LabelType.If:
							var jump = new Jump(true);
							AddInstruction(jump);
							label.JumpsToEnd.Enqueue(jump);
							break;
						case LabelType.Else:
							throw new NotImplementedException("branch_if from within else block");
						default:
							throw new ArgumentOutOfRangeException(nameof(label.Type), label.Type, $"Unknown label type: {label.Type}");
					}
					break;
				}
				// TODO: BrTable
				#endregion

				case Return:
				{
					var label = GetLabel((uint)(labels.Count - 1)); // get the very last label

					Debug.Assert(label.Type == LabelType.Block, "label for return instruction should be block");

					var jump = new Jump(false);
					AddInstruction(jump);
					label.JumpsToEnd.Enqueue(jump);
					break;
				}

				case Call call:
				{
					// call.Index
					var type = _document.GetFunctionType((int)call.Index);
					AddInstruction(new CallFunction(
						type.Parameters.Select(WasmTypeToDecompilerType).ToArray(),
						type.Returns.Select(WasmTypeToDecompilerType).ToArray()));
					break;
				}

				// TODO: Call, CallIndirect, Drop, Select
				case LocalGet get:
					AddInstruction(new LoadVariable(variables[(int)get.Index]));
					break;
				case LocalSet set:
					AddInstruction(new StoreVariable(variables[(int)set.Index]));
					break;
				case LocalTee tee:
				{
					var variable = variables[(int)tee.Index];
					AddInstruction(new StoreVariable(variable));
					AddInstruction(new LoadVariable(variable));
					break;
				}
				// TODO: {Global,Table}.{Get,Set}
				// TODO: memory load/store/size/grow
				case Int32Constant i32:
					AddInstruction(new LoadConstant(new I32Value(i32.Value)));
					break;
				case Int64Constant i64:
					AddInstruction(new LoadConstant(new I64Value(i64.Value)));
					break;
				case Float32Constant f32:
					AddInstruction(new LoadConstant(new F32Value(f32.Value)));
					break;
				case Float64Constant f64:
					AddInstruction(new LoadConstant(new F64Value(f64.Value)));
					break;

				#region Int32 comparison ops
				case Int32EqualZero:
					AddInstruction(new LoadConstant(new I32Value(0)));
					AddInstruction(new BinaryOperator(BinaryOperationType.Equal, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32Equal:
					AddInstruction(new BinaryOperator(BinaryOperationType.Equal, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32NotEqual:
					AddInstruction(new BinaryOperator(BinaryOperationType.NotEqual, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32LessThanUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.LessThan, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32LessThanSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.LessThanSigned, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32GreaterThanUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.GreaterThan, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32GreaterThanSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.GreaterThanSigned, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32LessThanOrEqualUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.LessThanOrEqual, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32LessThanOrEqualSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.LessThanOrEqualSigned, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32GreaterThanOrEqualUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.GreaterThanOrEqual, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32GreaterThanOrEqualSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.GreaterThanOrEqualSigned, DataType.I32, DataType.I32, DataType.I32));
					break;
				#endregion

				#region Int64 comparison ops
				case Int64EqualZero:
					AddInstruction(new LoadConstant(new I64Value(0)));
					AddInstruction(new BinaryOperator(BinaryOperationType.Equal, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64Equal:
					AddInstruction(new BinaryOperator(BinaryOperationType.Equal, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64NotEqual:
					AddInstruction(new BinaryOperator(BinaryOperationType.NotEqual, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64LessThanUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.LessThan, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64LessThanSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.LessThanSigned, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64GreaterThanUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.GreaterThan, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64GreaterThanSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.GreaterThanSigned, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64LessThanOrEqualUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.LessThanOrEqual, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64LessThanOrEqualSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.LessThanOrEqualSigned, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64GreaterThanOrEqualUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.GreaterThanOrEqual, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64GreaterThanOrEqualSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.GreaterThanOrEqualSigned, DataType.I64, DataType.I64, DataType.I64));
					break;
				#endregion

				#region Float32 comparison ops
				case Float32Equal:
					AddInstruction(new BinaryOperator(BinaryOperationType.Equal, DataType.F32, DataType.F32, DataType.F32));
					break;
				case Float32NotEqual:
					AddInstruction(new BinaryOperator(BinaryOperationType.NotEqual, DataType.F32, DataType.F32, DataType.F32));
					break;
				case Float32LessThan:
					AddInstruction(new BinaryOperator(BinaryOperationType.LessThan, DataType.F32, DataType.F32, DataType.F32));
					break;
				case Float32GreaterThan:
					AddInstruction(new BinaryOperator(BinaryOperationType.GreaterThan, DataType.F32, DataType.F32, DataType.F32));
					break;
				case Float32LessThanOrEqual:
					AddInstruction(new BinaryOperator(BinaryOperationType.LessThanOrEqual, DataType.F32, DataType.F32, DataType.F32));
					break;
				case Float32GreaterThanOrEqual:
					AddInstruction(new BinaryOperator(BinaryOperationType.GreaterThanOrEqual, DataType.F32, DataType.F32, DataType.F32));
					break;
				#endregion

				#region Float64 comparison ops
				case Float64Equal:
					AddInstruction(new BinaryOperator(BinaryOperationType.Equal, DataType.F64, DataType.F64, DataType.F64));
					break;
				case Float64NotEqual:
					AddInstruction(new BinaryOperator(BinaryOperationType.NotEqual, DataType.F64, DataType.F64, DataType.F64));
					break;
				case Float64LessThan:
					AddInstruction(new BinaryOperator(BinaryOperationType.LessThan, DataType.F64, DataType.F64, DataType.F64));
					break;
				case Float64GreaterThan:
					AddInstruction(new BinaryOperator(BinaryOperationType.GreaterThan, DataType.F64, DataType.F64, DataType.F64));
					break;
				case Float64LessThanOrEqual:
					AddInstruction(new BinaryOperator(BinaryOperationType.LessThanOrEqual, DataType.F64, DataType.F64, DataType.F64));
					break;
				case Float64GreaterThanOrEqual:
					AddInstruction(new BinaryOperator(BinaryOperationType.GreaterThanOrEqual, DataType.F64, DataType.F64, DataType.F64));
					break;
				#endregion

				// TODO: Int32 unary ops

				#region Int32 binary ops
				case Int32Add:
					AddInstruction(new BinaryOperator(BinaryOperationType.Add, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32Subtract:
					AddInstruction(new BinaryOperator(BinaryOperationType.Sub, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32Multiply:
					AddInstruction(new BinaryOperator(BinaryOperationType.Mul, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32DivideSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.DivS, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32DivideUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.Div, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32RemainderSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.RemS, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32RemainderUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.Rem, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32And:
					AddInstruction(new BinaryOperator(BinaryOperationType.And, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32Or:
					AddInstruction(new BinaryOperator(BinaryOperationType.Or, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32ExclusiveOr:
					AddInstruction(new BinaryOperator(BinaryOperationType.Xor, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32ShiftLeft:
					AddInstruction(new BinaryOperator(BinaryOperationType.Shl, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32ShiftRightSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.ShrZeroExtend, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32ShiftRightUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.ShrSignExtend, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32RotateLeft:
					AddInstruction(new BinaryOperator(BinaryOperationType.Rotl, DataType.I32, DataType.I32, DataType.I32));
					break;
				case Int32RotateRight:
					AddInstruction(new BinaryOperator(BinaryOperationType.Rotr, DataType.I32, DataType.I32, DataType.I32));
					break;
				#endregion

				// TODO: Int64 unary ops

				#region Int64 binary ops
				case Int64Add:
					AddInstruction(new BinaryOperator(BinaryOperationType.Add, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64Subtract:
					AddInstruction(new BinaryOperator(BinaryOperationType.Sub, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64Multiply:
					AddInstruction(new BinaryOperator(BinaryOperationType.Mul, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64DivideSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.DivS, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64DivideUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.Div, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64RemainderSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.RemS, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64RemainderUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.Rem, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64And:
					AddInstruction(new BinaryOperator(BinaryOperationType.And, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64Or:
					AddInstruction(new BinaryOperator(BinaryOperationType.Or, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64ExclusiveOr:
					AddInstruction(new BinaryOperator(BinaryOperationType.Xor, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64ShiftLeft:
					AddInstruction(new BinaryOperator(BinaryOperationType.Shl, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64ShiftRightSigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.ShrZeroExtend, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64ShiftRightUnsigned:
					AddInstruction(new BinaryOperator(BinaryOperationType.ShrSignExtend, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64RotateLeft:
					AddInstruction(new BinaryOperator(BinaryOperationType.Rotl, DataType.I64, DataType.I64, DataType.I64));
					break;
				case Int64RotateRight:
					AddInstruction(new BinaryOperator(BinaryOperationType.Rotr, DataType.I64, DataType.I64, DataType.I64));
					break;
				#endregion

				// TODO: Float32 unary ops

				#region Float32 binary ops
				case Float32Add:
					AddInstruction(new BinaryOperator(BinaryOperationType.Add, DataType.F32, DataType.F32, DataType.F32));
					break;
				case Float32Subtract:
					AddInstruction(new BinaryOperator(BinaryOperationType.Sub, DataType.F32, DataType.F32, DataType.F32));
					break;
				case Float32Multiply:
					AddInstruction(new BinaryOperator(BinaryOperationType.Mul, DataType.F32, DataType.F32, DataType.F32));
					break;
				case Float32Divide:
					AddInstruction(new BinaryOperator(BinaryOperationType.Div, DataType.F32, DataType.F32, DataType.F32));
					break;
				// TODO: min, max, copysign
				#endregion

				// TODO: Float64 unary ops

				#region Float64 binary ops
				case Float64Add:
					AddInstruction(new BinaryOperator(BinaryOperationType.Add, DataType.F64, DataType.F64, DataType.F64));
					break;
				case Float64Subtract:
					AddInstruction(new BinaryOperator(BinaryOperationType.Sub, DataType.F64, DataType.F64, DataType.F64));
					break;
				case Float64Multiply:
					AddInstruction(new BinaryOperator(BinaryOperationType.Mul, DataType.F64, DataType.F64, DataType.F64));
					break;
				case Float64Divide:
					AddInstruction(new BinaryOperator(BinaryOperationType.Div, DataType.F64, DataType.F64, DataType.F64));
					break;
				// TODO: min, max, copysign
				#endregion

				case Int32WrapInt64:
					AddInstruction(
						new ConversionOperator(DataType.I64, DataType.I32, ConversionType.Convert));
					break;
				// TODO: other operations
				case Int32ReinterpretFloat32:
					AddInstruction(new ConversionOperator(DataType.F32, DataType.I32, ConversionType.Reinterpret));
					break;
				case Float32ReinterpretInt32:
					AddInstruction(new ConversionOperator(DataType.I32, DataType.F32, ConversionType.Reinterpret));
					break;
				// TODO: other operations
				default:
					throw new NotImplementedException("Not opcode implemented: " + wasmInstruction.OpCode);
			}
		}

		Debug.Assert(!labels.Any(), "Label stack should be empty");
		Debug.Assert(!jumpsToAdd.Any(), "No queued jumps should be left");
		Debug.Assert(!labelsToAdd.Any(), "No queued labels should be left");

		Debug.Assert(!instructions.OfType<Jump>().Any(j => j.Target is null), "No jumps with null targets should be left");

		return instructions;
	}

	private struct LabelInfo
	{
		public LabelInfo(IntermediateInstruction startInstruction, LabelType type)
		{
			StartInstruction = startInstruction;
			Type = type;
		}

		public IntermediateInstruction StartInstruction { get; set; }
		public Queue<Jump> JumpsToEnd { get; set; } = new();
		public LabelType Type { get; set; }
	}

	private enum LabelType
	{
		Block,
		If,
		Else,
		Loop,
	}
}
