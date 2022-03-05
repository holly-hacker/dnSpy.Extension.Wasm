using System;
using System.Collections.Generic;
using System.Linq;
using dnSpy.Extension.Wasm.TreeView;
using WebAssembly;
using WebAssembly.Instructions;

namespace dnSpy.Extension.Wasm.Decompilers;

internal class DisassemblerDecompiler : IWasmDecompiler
{
	public void Decompile(WasmDocument doc, DecompilerWriter writer, string name, IList<Local> locals, IList<Instruction> code, WebAssemblyType functionType)
	{
		writer.FunctionDeclaration(name, functionType);
		writer.EndLine();
		writer.Punctuation("{");
		writer.EndLine();
		writer.Indent();

		WriteLocals(writer, locals);

		if (locals.Any(l => l.Count > 0))
			writer.EndLine();

		WriteInstructions(writer, code);

		writer.DeIndent().Punctuation("}");
		writer.EndLine();
	}

	private void WriteLocals(DecompilerWriter writer, IEnumerable<Local> locals)
	{
		int localIndex = 0;
		foreach (var local in locals)
		{
			for (var i = 0; i < local.Count; i++)
			{
				writer.Local("local_" + localIndex).Punctuation(": ").Keyword(local.Type.ToWasmType());
				writer.EndLine();
				localIndex++;
			}
		}
	}

	private void WriteInstructions(DecompilerWriter writer, IList<Instruction> instructions)
	{
		for (var i = 0; i < instructions.Count; i++)
		{
			var instruction = instructions[i];

			switch (instruction)
			{
				case BlockTypeInstruction block:
				{
					writer.OpCode(instruction.OpCode).Space();

					if (block.Type != BlockType.Empty)
						writer.Keyword(block.Type.ToTypeString()).Space();

					writer.Punctuation("{");
					writer.Indent();
					break;
				}
				case End:
				{
					writer.OpCode(instruction.OpCode);

					// only close current block if we're not at the last instruction
					if (i != instructions.Count - 1)
						writer.EndLine().DeIndent().Punctuation("}");
					break;
				}
				case Branch branch:
				{
					writer.OpCode(instruction.OpCode).Space().Number(branch.Index);
					break;
				}
				case BranchIf branchIf:
				{
					writer.OpCode(instruction.OpCode).Space().Number(branchIf.Index);
					break;
				}
				case BranchTable branchTable:
				{
					writer.OpCode(instruction.OpCode).Space();
					foreach (uint label in branchTable.Labels)
					{
						writer.Number(label).Space();
					}

					writer.Number(branchTable.DefaultLabel);
					break;
				}
				case Call call:
				{
					writer.OpCode(instruction.OpCode).Space().Number(call.Index);
					break;
				}
				case CallIndirect callIndirect:
				{
					writer.OpCode(instruction.OpCode).Space().Number(callIndirect.Type);
					break;
				}
				case MemoryImmediateInstruction memImm:
				{
					writer.OpCode(instruction.OpCode)
						.Space()
						.Punctuation("(")
						.Text("align")
						.Punctuation("=")
						.Number((int)Math.Pow(2, (int)memImm.Flags))
						.Punctuation(", ")
						.Text("offset")
						.Punctuation("=")
						.Number(memImm.Offset)
						.Punctuation(")");
					break;
				}
				case Constant<int> constant:
				{
					writer.OpCode(instruction.OpCode).Space().Number(constant.Value);
					break;
				}
				case Constant<long> constant:
				{
					writer.OpCode(instruction.OpCode).Space().Number(constant.Value);
					break;
				}
				case Constant<float> constant:
				{
					writer.OpCode(instruction.OpCode).Space().Number(constant.Value);
					break;
				}
				case Constant<double> constant:
				{
					writer.OpCode(instruction.OpCode).Space().Number(constant.Value);
					break;
				}
				case SimpleInstruction:
				{
					writer.OpCode(instruction.OpCode);
					break;
				}
				case VariableAccessInstruction va:
				{
					writer.OpCode(instruction.OpCode).Space().Number(va.Index);
					break;
				}
				default:
				{
					Console.WriteLine("Unhandled instruction: " + instruction);
					writer.Text(instruction.ToString());
					break;
				}
			}

			writer.EndLine();
		}
	}
}
