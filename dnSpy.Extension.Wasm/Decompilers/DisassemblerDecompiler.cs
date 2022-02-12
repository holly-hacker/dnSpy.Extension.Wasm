using System;
using System.Linq;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using WebAssembly;
using WebAssembly.Instructions;

namespace dnSpy.Extension.Wasm.Decompilers;

public class DisassemblerDecompiler : IWasmDecompiler
{
	public void Decompile(IDecompileNodeContext context, int index, FunctionBody code, WebAssemblyType type)
	{
		var writer = new DecompilerWriter(context.Output);

		writer.FunctionDeclaration($"function_{index}", type);
		writer.EndLine();
		writer.Punctuation("{");
		writer.EndLine();
		writer.Indent();

		int localIndex = 0;
		foreach (var local in code.Locals)
		{
			for (var i = 0; i < local.Count; i++)
			{
				writer.Local("local_" + localIndex).Punctuation(": ").Keyword(local.Type.ToWasmType());
				writer.EndLine();
				localIndex++;
			}
		}

		if (code.Locals.Any(l => l.Count > 0))
			writer.EndLine();

		foreach (var instruction in code.Code)
		{
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
