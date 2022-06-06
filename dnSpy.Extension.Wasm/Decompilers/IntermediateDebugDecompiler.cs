using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Decompiler;
using dnSpy.Extension.Wasm.Decompiling;
using dnSpy.Extension.Wasm.TreeView;
using HoLLy.Decompiler.Core.FrontEnd;
using HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;
using WebAssembly;

namespace dnSpy.Extension.Wasm.Decompilers;

[Export(typeof(IWasmDecompiler))]
internal class IntermediateDebugDecompiler : IWasmDecompiler
{
	public string Name => "[DEBUG] IR";
	public int Order => 1;

	public void Decompile(WasmDocument doc, DecompilerWriter writer, string name, IList<Local> locals, IList<Instruction> code,
		WebAssemblyType functionType, int? globalIndex = null)
	{
		IDecompilerFrontend converter = new WasmDecompilerFrontend(doc, locals, code, functionType);

		var instructions = converter.Convert();
		for (var index = 0; index < instructions.Count; index++)
		{
			var instruction = instructions[index];
			writer.Number(index, "0000").Text(":").Space();
			switch (instruction)
			{
				case LoadConstant loadConstant:
					writer.Text("load_const").Space();
					_ = loadConstant.Value switch
					{
						F32Value f32Value => writer.Number(f32Value.Value),
						F64Value f64Value => writer.Number(f64Value.Value),
						I32Value i32Value => writer.Number(i32Value.Value),
						I64Value i64Value => writer.Number(i64Value.Value),
						_ => writer.Keyword(loadConstant.Value.ToString()!),
					};
					writer.EndLine();
					break;
				case LoadLocal loadVariable:
					writer.Text("load_var").Space().Keyword(loadVariable.Variable.ToString()!).EndLine();
					break;
				case StoreLocal storeVariable:
					writer.Text("store_var").Space().Keyword(storeVariable.Variable.ToString()!).EndLine();
					break;
				case LoadGlobal loadVariable:
					writer.Text("load_global").Space().Keyword(loadVariable.Variable.ToString()!).EndLine();
					break;
				case StoreGlobal storeVariable:
					writer.Text("store_global").Space().Keyword(storeVariable.Variable.ToString()!).EndLine();
					break;
				case LoadPointer loadPointer:
					writer.Text("load_pointer").Space().Keyword($"{loadPointer.DataType}_{loadPointer.Size}").EndLine();
					break;
				case StorePointer storePointer:
					writer.Text("store_pointer").Space().Keyword($"{storePointer.DataType}_{storePointer.Size}").EndLine();
					break;
				case DuplicateStackValue:
					writer.Text("dup").EndLine();
					break;
				case DropStackValue:
					writer.Text("drop").EndLine();
					break;

				case UnaryOperator unaryOperator:
					writer.Text("un_op").Space().Keyword(unaryOperator.Type.ToString()).EndLine();
					break;
				case BinaryOperator binaryOperator:
					writer.Text("bin_op").Space().Keyword(binaryOperator.Type.ToString()).EndLine();
					break;
				case ConversionOperator conversionOperator:
					writer.Text("conv_op").Space().Keyword(conversionOperator.Type.ToString()).EndLine();
					break;

				case Jump jump:
					Debug2.Assert(jump.Target is not null);
					int target = instructions.IndexOf(jump.Target);
					writer.Text(jump.Conditional ? "jmp_if" : "jmp").Space().Number(target, "0000").EndLine();
					break;

				case CallFunction call:
					writer.Text("call").Space()
						.OpenBrace("(", CodeBracesRangeFlags.Parentheses)
						.Text(string.Join(", ", call.InputParameters)).CloseBrace(")")
						.Space().Punctuation("->").Space()
						.OpenBrace("(", CodeBracesRangeFlags.Parentheses)
						.Text(string.Join(", ", call.InputParameters)).CloseBrace(")")
						.EndLine();
					break;

				case EndOfFunction:
					writer.Text("end_function").EndLine();
					break;

				case TrapInstruction trapInstruction:
					writer.Text("TRAP").EndLine();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(instruction), instruction, $"Unknown instruction type {instruction.GetType().Name}");
			}
		}
	}
}
