using System;
using System.Globalization;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using WebAssembly;
using WebAssembly.Instructions;

namespace dnSpy.Extension.Wasm.Decompilers;

public class DisassemblerDecompiler : IWasmDecompiler
{
	public void Decompile(IDecompileNodeContext context, int index, FunctionBody code, WebAssemblyType type)
	{
		context.Output.WriteLine(type.ToString(), BoxedTextColor.Text);
		context.Output.WriteLine();
		context.Output.Write("{", BoxedTextColor.Punctuation);

		int localIndex = 0;
		foreach (var local in code.Locals)
		{
			for (var i = 0; i < local.Count; i++)
			{
				context.Output.Write("\tlocal_" + localIndex, BoxedTextColor.Local);
				context.Output.Write(": ", BoxedTextColor.Punctuation);
				context.Output.Write(local.Type.ToWasmType(), BoxedTextColor.Keyword);
				context.Output.WriteLine();
				localIndex++;
			}
		}

		if (code.Locals.Any(l => l.Count > 0))
			context.Output.WriteLine();

		void WriteOpCode(OpCode opCode)
		{
			context.Output.Write(opCode.ToInstruction(), BoxedTextColor.AsmMnemonic);
		}

		void WriteSpace()
		{
			context.Output.Write(" ", BoxedTextColor.Text);
		}

		int indentation = 1;
		foreach (var instruction in code.Code)
		{
			context.Output.Write(new string('\t', indentation), BoxedTextColor.Punctuation);
			switch (instruction)
			{
				case BlockTypeInstruction block:
				{
					WriteOpCode(instruction.OpCode);
					if (block.Type != BlockType.Empty)
						WriteSpace();
					context.Output.Write(block.Type.ToTypeString(), BoxedTextColor.Keyword);

					context.Output.Write(" {", BoxedTextColor.Punctuation);
					indentation++;
					break;
				}
				case End:
				{
					WriteOpCode(instruction.OpCode);
					context.Output.WriteLine();
					indentation--;
					context.Output.Write(new string('\t', indentation) + "}", BoxedTextColor.Punctuation);
					break;
				}
				case Branch branch:
				{
					WriteOpCode(instruction.OpCode);
					WriteSpace();
					context.Output.Write(branch.Index.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number);
					break;
				}
				case BranchIf branchIf:
				{
					WriteOpCode(instruction.OpCode);
					WriteSpace();
					context.Output.Write(branchIf.Index.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number);
					break;
				}
				case Call call:
				{
					WriteOpCode(instruction.OpCode);
					WriteSpace();
					context.Output.Write(call.Index.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number);
					break;
				}
				case CallIndirect callIndirect:
				{
					WriteOpCode(instruction.OpCode);
					WriteSpace();
					context.Output.Write(callIndirect.Type.ToString(CultureInfo.InvariantCulture),
						BoxedTextColor.Number);
					break;
				}
				case MemoryImmediateInstruction memImm:
				{
					WriteOpCode(instruction.OpCode);
					WriteSpace();
					context.Output.Write("(", BoxedTextColor.Punctuation);
					context.Output.Write("align", BoxedTextColor.Text);
					context.Output.Write("=", BoxedTextColor.Punctuation);
					context.Output.Write(((int)Math.Pow(2, (int)memImm.Flags)).ToString(CultureInfo.InvariantCulture),
						BoxedTextColor.Number);
					context.Output.Write(", ", BoxedTextColor.Punctuation);
					context.Output.Write("offset", BoxedTextColor.Text);
					context.Output.Write("=", BoxedTextColor.Punctuation);
					context.Output.Write(memImm.Offset.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number);
					context.Output.Write(")", BoxedTextColor.Punctuation);
					break;
				}
				case Constant<int> constant:
				{
					WriteOpCode(instruction.OpCode);
					WriteSpace();
					context.Output.Write(constant.Value.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number);
					break;
				}
				case Constant<long> constant:
				{
					WriteOpCode(instruction.OpCode);
					WriteSpace();
					context.Output.Write(constant.Value.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number);
					break;
				}
				case Constant<float> constant:
				{
					WriteOpCode(instruction.OpCode);
					WriteSpace();
					context.Output.Write(constant.Value.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number);
					break;
				}
				case Constant<double> constant:
				{
					WriteOpCode(instruction.OpCode);
					context.Output.Write(constant.Value.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number);
					break;
				}
				case SimpleInstruction:
				{
					WriteOpCode(instruction.OpCode);
					break;
				}
				case VariableAccessInstruction va:
				{
					WriteOpCode(instruction.OpCode);
					WriteSpace();
					context.Output.Write(va.Index.ToString(), BoxedTextColor.Number);
					break;
				}
				default:
				{
					Console.WriteLine("Unhandled instruction: " + instruction);
					context.Output.Write(instruction.ToString(), BoxedTextColor.Text);
					break;
			}
			}

			context.Output.WriteLine();
		}
	}
}
