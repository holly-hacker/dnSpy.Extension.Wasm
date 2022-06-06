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
		var locals = new List<WasmLocalVariable>();
		var globals = new Dictionary<uint, WasmGlobalVariable>();

		foreach (var parameterType in _functionType.Parameters)
		{
			var type = WasmTypeToDecompilerType(parameterType);
			locals.Add(new WasmLocalVariable(type));
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
				locals.Add(new WasmLocalVariable(type));
			}
		}

		// the stack of wasm labels that branching instructions may jump to
		// these represent scopes, and will be popped when the scope exits
		var labelStack = new Stack<LabelInfo>();

		// queued labels that need to be pushed to the stack
		// the next instruction will have a label of this type
		var labelTypesToAdd = new Queue<LabelType>();

		// jumps that want their target to be the next instruction
		var jumpsToAdd = new Queue<Jump>();

		void AddInstruction(IntermediateInstruction i)
		{
			while (labelTypesToAdd.Any())
				labelStack.Push(new LabelInfo(i, labelTypesToAdd.Dequeue()));

			while (jumpsToAdd.Any())
				jumpsToAdd.Dequeue().SetTarget(i);

			instructions.Add(i);
		}

		LabelInfo GetLabel(uint i) => labelStack.ElementAt((int)i);

		void AddJumpOutOfBlock(uint index, bool conditional)
		{
			var label = GetLabel(index);

			switch (label.Type)
			{
				case LabelType.Loop:
					AddInstruction(new Jump(conditional, label.StartInstruction));
					break;
				case LabelType.Block:
				case LabelType.If:
					var jump = new Jump(conditional);
					AddInstruction(jump);
					label.JumpsToEnd.Enqueue(jump);
					break;
				case LabelType.Else:
					throw new NotImplementedException("branch from within else block");
				default:
					throw new ArgumentOutOfRangeException(nameof(label.Type), label.Type,
						$"Unknown label type: {label.Type}");
			}
		}


		// before doing anything, create a label for the function scope
		// this is used so return instructions have something to jump to
		labelTypesToAdd.Enqueue(LabelType.Block);

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

					labelTypesToAdd.Enqueue(LabelType.Block);
					break;
				}
				case Loop loop:
				{
					if (loop.Type != BlockType.Empty)
						throw new NotImplementedException("BlockType is not empty for Loop");

					labelTypesToAdd.Enqueue(LabelType.Loop);
					break;
				}
				case If @if:
				{
					if (@if.Type != BlockType.Empty)
						throw new NotImplementedException("BlockType is not empty for If");

					labelTypesToAdd.Enqueue(LabelType.If);
					var jump = new Jump(true);
					AddInstruction(jump);
					labelStack.Peek().JumpsToEnd.Enqueue(jump);
					break;
				}
				// TODO: Else
				case End:
				{
					// end the current scope by popping the label from the stack and queueing the relevant jumps
					// An edge case is an empty function: the label will not be popped yet because no instructions have
					// been emitted. In that case, dont pop anything.
					if (!labelStack.Any())
					{
						Debug.Assert(!labelStack.Any());
						Debug.Assert(labelTypesToAdd.Peek() == LabelType.Block);
						Debug.Assert(labelTypesToAdd.Count == 1);
						Debug.Assert(instructionIndex == _code.Count - 1);

						_ = labelTypesToAdd.Dequeue();
					}
					else
					{
						var label = labelStack.Pop();

						if (label.Type == LabelType.Else)
							throw new NotImplementedException("End for Else block");

						foreach (var jump in label.JumpsToEnd)
							jumpsToAdd.Enqueue(jump);
					}

					// add pseudo instruction to mark end of function
					if (instructionIndex == _code.Count - 1)
						AddInstruction(new EndOfFunction());
					break;
				}
				case Branch branch:
				{
					AddJumpOutOfBlock(branch.Index, false);
					break;
				}
				case BranchIf branchIf:
				{
					AddJumpOutOfBlock(branchIf.Index, true);
					break;
				}
				case BranchTable branchTable:
				{
					/*
					 * push some_index
					 *
					 * CASE_N:
					 *     dup
					 *     cmp 0
					 *     jmp_ne CASE_N+1
					 *     drop
					 *     jmp TARGET_N
					 *
					 * (repeat for all labels)
					 *
					 * DEFAULT:
					 *     jmp TARGET_DEFAULT
					 */

					var labels = branchTable.Labels;
					uint defaultLabel = branchTable.DefaultLabel;

					Jump? jumpToNext = null;
					for (var index = 0; index < labels.Count; index++)
					{
						uint labelIndex = labels[index];

						if (labelIndex == defaultLabel)
							continue;

						if (jumpToNext != null)
							jumpsToAdd.Enqueue(jumpToNext);

						AddInstruction(new DuplicateStackValue());
						AddInstruction(new LoadConstant(new I32Value(index)));
						AddInstruction(new BinaryOperator(BinaryOperationType.NotEqual, DataType.I32, DataType.I32, DataType.I32));
						AddInstruction(jumpToNext = new Jump(true));
						AddInstruction(new DropStackValue());
						AddJumpOutOfBlock(labelIndex, false);
					}

					if (jumpToNext != null)
						jumpsToAdd.Enqueue(jumpToNext);

					AddJumpOutOfBlock(defaultLabel, false);

					break;
				}
				#endregion

				case Return:
				{
					var label = GetLabel((uint)(labelStack.Count - 1)); // get the very last label

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

				// TODO: CallIndirect
				case Drop:
					AddInstruction(new DropStackValue());
					break;
				// TODO: Select
				case LocalGet get:
					AddInstruction(new LoadLocal(locals[(int)get.Index]));
					break;
				case LocalSet set:
					AddInstruction(new StoreLocal(locals[(int)set.Index]));
					break;
				case LocalTee tee:
				{
					var variable = locals[(int)tee.Index];
					AddInstruction(new StoreLocal(variable));
					AddInstruction(new LoadLocal(variable));
					break;
				}
				case GlobalGet get:
				{
					var global = globals.GetOrInsert(get.Index,
						() => new WasmGlobalVariable(
							WasmTypeToDecompilerType(_document.GetGlobalType((int)get.Index))));
					AddInstruction(new LoadGlobal(global));
					break;
				}
				case GlobalSet set:
				{
					var global = globals.GetOrInsert(set.Index,
						() => new WasmGlobalVariable(
							WasmTypeToDecompilerType(_document.GetGlobalType((int)set.Index))));
					AddInstruction(new StoreGlobal(global));
					break;
				}
				// TODO: {Global,Table}.{Get,Set}
				case MemoryReadInstruction memoryRead:
				{
					(WebAssemblyValueType type, int size) = memoryRead switch
					{
						Int32Load8Signed _ => (WebAssemblyValueType.Int32, 1),
						Int32Load8Unsigned _ => (WebAssemblyValueType.Int32, 1),
						Int32Load16Signed _ => (WebAssemblyValueType.Int32, 2),
						Int32Load16Unsigned _ => (WebAssemblyValueType.Int32, 2),
						Int32Load _ => (WebAssemblyValueType.Int32, 4),
						Int64Load8Signed _ => (WebAssemblyValueType.Int64, 1),
						Int64Load8Unsigned _ => (WebAssemblyValueType.Int64, 1),
						Int64Load16Signed _ => (WebAssemblyValueType.Int64, 2),
						Int64Load16Unsigned _ => (WebAssemblyValueType.Int64, 2),
						Int64Load32Signed _ => (WebAssemblyValueType.Int64, 4),
						Int64Load32Unsigned _ => (WebAssemblyValueType.Int64, 4),
						Int64Load _ => (WebAssemblyValueType.Int64, 8),
						_ => throw new ArgumentOutOfRangeException(nameof(memoryRead),
							"Unknown memory read instruction: " + memoryRead.GetType().Name),
					};

					if (memoryRead.Offset != 0)
					{
						AddInstruction(new LoadConstant(new I32Value((int)memoryRead.Offset)));
						AddInstruction(new BinaryOperator(BinaryOperationType.Add, DataType.I32, DataType.I32, DataType.I32));
					}

					AddInstruction(new LoadPointer(WasmTypeToDecompilerType(type), size));
					break;
				}
				case MemoryWriteInstruction memoryWrite:
				{
					(WebAssemblyValueType type, int size) = memoryWrite switch
					{
						Int32Store8 _ => (WebAssemblyValueType.Int32, 1),
						Int32Store16 _ => (WebAssemblyValueType.Int32, 2),
						Int32Store _ => (WebAssemblyValueType.Int32, 4),
						Int64Store8 _ => (WebAssemblyValueType.Int64, 1),
						Int64Store16 _ => (WebAssemblyValueType.Int64, 2),
						Int64Store32 _ => (WebAssemblyValueType.Int64, 4),
						Int64Store _ => (WebAssemblyValueType.Int64, 8),
						_ => throw new ArgumentOutOfRangeException(nameof(memoryWrite),
							"Unknown memory write instruction: " + memoryWrite.GetType().Name),
					};

					if (memoryWrite.Offset != 0)
					{
						// TODO: need to target the second value on the stack here
						// we can either store the top value on the stack in a temporary variable, or swap the first and
						// second values

						AddInstruction(new LoadConstant(new I32Value((int)memoryWrite.Offset)));
						AddInstruction(new BinaryOperator(BinaryOperationType.Add, DataType.I32, DataType.I32, DataType.I32));
					}

					AddInstruction(new StorePointer(WasmTypeToDecompilerType(type), size));
					break;
				}
				// TODO: memory size/grow
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
					AddInstruction(new ConversionOperator(DataType.I64, DataType.I32, ConversionType.Convert));
					break;
				case Int32TruncateFloat32Signed:
					AddInstruction(new ConversionOperator(DataType.F32, DataType.I32, ConversionType.ConvertSigned));
					break;
				case Int32TruncateFloat32Unsigned:
					AddInstruction(new ConversionOperator(DataType.F32, DataType.I32, ConversionType.Convert));
					break;
				case Int32TruncateFloat64Signed:
					AddInstruction(new ConversionOperator(DataType.F64, DataType.I32, ConversionType.ConvertSigned));
					break;
				case Int32TruncateFloat64Unsigned:
					AddInstruction(new ConversionOperator(DataType.F64, DataType.I32, ConversionType.Convert));
					break;
				case Int64ExtendInt32Signed:
					AddInstruction(new ConversionOperator(DataType.I32, DataType.I64, ConversionType.ConvertSigned));
					break;
				case Int64ExtendInt32Unsigned:
					AddInstruction(new ConversionOperator(DataType.I32, DataType.I64, ConversionType.Convert));
					break;
				case Int64TruncateFloat32Signed:
					AddInstruction(new ConversionOperator(DataType.F32, DataType.I64, ConversionType.ConvertSigned));
					break;
				case Int64TruncateFloat32Unsigned:
					AddInstruction(new ConversionOperator(DataType.F32, DataType.I64, ConversionType.Convert));
					break;
				case Int64TruncateFloat64Signed:
					AddInstruction(new ConversionOperator(DataType.F64, DataType.I64, ConversionType.ConvertSigned));
					break;
				case Int64TruncateFloat64Unsigned:
					AddInstruction(new ConversionOperator(DataType.F64, DataType.I64, ConversionType.Convert));
					break;
				case Float32ConvertInt32Signed:
					AddInstruction(new ConversionOperator(DataType.I32, DataType.F32, ConversionType.ConvertSigned));
					break;
				case Float32ConvertInt32Unsigned:
					AddInstruction(new ConversionOperator(DataType.I32, DataType.F32, ConversionType.Convert));
					break;
				case Float32ConvertInt64Signed:
					AddInstruction(new ConversionOperator(DataType.I64, DataType.F32, ConversionType.ConvertSigned));
					break;
				case Float32ConvertInt64Unsigned:
					AddInstruction(new ConversionOperator(DataType.I64, DataType.F32, ConversionType.Convert));
					break;
				case Float32DemoteFloat64:
					AddInstruction(new ConversionOperator(DataType.F64, DataType.F32, ConversionType.Convert));
					break;
				case Float64ConvertInt32Signed:
					AddInstruction(new ConversionOperator(DataType.I32, DataType.F64, ConversionType.ConvertSigned));
					break;
				case Float64ConvertInt32Unsigned:
					AddInstruction(new ConversionOperator(DataType.I32, DataType.F64, ConversionType.Convert));
					break;
				case Float64ConvertInt64Signed:
					AddInstruction(new ConversionOperator(DataType.I64, DataType.F64, ConversionType.ConvertSigned));
					break;
				case Float64ConvertInt64Unsigned:
					AddInstruction(new ConversionOperator(DataType.I64, DataType.F64, ConversionType.Convert));
					break;
				case Float64PromoteFloat32:
					AddInstruction(new ConversionOperator(DataType.F32, DataType.F64, ConversionType.Convert));
					break;
				case Int32ReinterpretFloat32:
					AddInstruction(new ConversionOperator(DataType.F32, DataType.I32, ConversionType.Reinterpret));
					break;
				case Int64ReinterpretFloat64:
					AddInstruction(new ConversionOperator(DataType.F64, DataType.I64, ConversionType.Reinterpret));
					break;
				case Float32ReinterpretInt32:
					AddInstruction(new ConversionOperator(DataType.I32, DataType.F32, ConversionType.Reinterpret));
					break;
				case Float64ReinterpretInt64:
					AddInstruction(new ConversionOperator(DataType.I64, DataType.F64, ConversionType.Reinterpret));
					break;
				// TODO: other operations
				default:
					throw new NotImplementedException("Translating opcode to IR not implemented: " + wasmInstruction.OpCode);
			}
		}

		Debug.Assert(!labelStack.Any(), "Label stack should be empty");
		Debug.Assert(!jumpsToAdd.Any(), "No queued jumps should be left");
		Debug.Assert(!labelTypesToAdd.Any(), "No queued labels should be left");

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
