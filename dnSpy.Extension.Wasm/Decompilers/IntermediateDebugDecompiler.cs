using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Extension.Wasm.Decompiling;
using dnSpy.Extension.Wasm.TreeView;
using HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;
using WebAssembly;

namespace dnSpy.Extension.Wasm.Decompilers;

[Export(typeof(IWasmDecompiler))]
internal class IntermediateDebugDecompiler : IWasmDecompiler
{
	public string Name => "DEBUG: IR";
	public int Order => 1;

	public void Decompile(WasmDocument doc, DecompilerWriter writer, string name, IList<Local> locals, IList<Instruction> code,
		WebAssemblyType functionType, int? globalIndex = null)
	{
		// TODO: write function declaration

		try
		{
			var converter = new WasmToIlConverter(doc, locals, code, functionType);

			var instructions = converter.Convert();
			for (var index = 0; index < instructions.Count; index++)
			{
				var instruction = instructions[index];
				writer.Number(index, "0000").Text(":").Space();
				switch (instruction)
				{
					case LoadConstant loadConstant:
						writer.Text("load_const").Space().Keyword(loadConstant.Value.ToString()).EndLine();
						break;
					case LoadVariable loadVariable:
						writer.Text("load_var").Space().Keyword(loadVariable.Variable.ToString()).EndLine();
						break;
					case StoreVariable storeVariable:
						writer.Text("store_var").Space().Keyword(storeVariable.Variable.ToString()).EndLine();
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
						int target = instructions.IndexOf(jump.Target);
						writer.Text(jump.Conditional ? "jmp_if" : "jmp").Space().Number(target, "0000").EndLine();
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
		catch (Exception e)
		{
			writer.Text(e.ToString());
		}
	}
}
