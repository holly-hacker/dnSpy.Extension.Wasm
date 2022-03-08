using System;
using System.Collections.Generic;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Extension.Wasm.TreeView;
using WebAssembly;
using WebAssembly.Instructions;

namespace dnSpy.Extension.Wasm.Decompilers;

internal class DisassemblerDecompiler : IWasmDecompiler
{
	public void Decompile(WasmDocument doc, DecompilerWriter writer, string name, IList<Local> locals, IList<Instruction> code, WebAssemblyType functionType, int? globalIndex = null)
	{
		var vars = new VariableInfo(doc, locals, functionType, globalIndex);

		// TODO: write parameter names
		writer.FunctionDeclaration(name, functionType, globalIndex, true, vars).EndLine();
		writer.OpenBrace("{", CodeBracesRangeFlags.LocalFunctionBraces).EndLine();
		writer.Indent();

		WriteLocals(vars, writer);

		if (vars.Locals.Any(l => !l.IsArgument))
			writer.EndLine();

		WriteInstructions(vars, doc, writer, code);

		writer.DeIndent().CloseBrace("}").EndLine();
	}

	private void WriteLocals(VariableInfo vars, DecompilerWriter writer)
	{
		foreach (var local in vars.Locals.Where(l => !l.IsArgument))
		{
			writer.Local(local.Name, local, true).Punctuation(": ").Type(local.Type);
			writer.EndLine();
		}
	}

	private void WriteInstructions(VariableInfo vars, WasmDocument doc, DecompilerWriter writer, IList<Instruction> instructions)
	{
		var labelStack = new Stack<BlockReference>();

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

					var flags = CodeBracesRangeFlags.BraceKind_CurlyBraces;
					flags |= CodeBracesRangeFlags.BlockKind_Other;

					var label = new BlockReference(labelStack.Count);
					writer.Label($"'{label}", label, true).Space().OpenBrace("{", flags).Indent();
					labelStack.Push(label);
					break;
				}
				case End:
				{
					writer.OpCode(instruction.OpCode);

					// only close current block if we're not at the last instruction
					if (i != instructions.Count - 1)
					{
						writer.EndLine().DeIndent().CloseBrace("}");
						labelStack.Pop();
					}
					break;
				}
				case Branch branch:
				{
					var label = labelStack.ElementAt((int)branch.Index);
					writer.OpCode(instruction.OpCode).Space().Label(label.ToString(), label);
					break;
				}
				case BranchIf branchIf:
				{
					var label = labelStack.ElementAt((int)branchIf.Index);
					writer.OpCode(instruction.OpCode).Space().Label(label.ToString(), label);
					break;
				}
				case BranchTable branchTable:
				{
					writer.OpCode(instruction.OpCode).Space();
					foreach (uint labelIndex in branchTable.Labels)
					{
						var label = labelStack.ElementAt((int)labelIndex);
						writer.Label(label.ToString(), label).Space();
					}

					writer.Number(branchTable.DefaultLabel);
					break;
				}
				case Call call:
				{
					var function = doc.GetFunctionName((int)call.Index);
					var type = doc.GetFunctionType((int)call.Index);
					// dont want to pass in a decompiler context here
					writer.OpCode(instruction.OpCode).Space().FunctionDeclaration(function, type, (int)call.Index, false);
					break;
				}
				case CallIndirect callIndirect:
				{
					var type = doc.Module.Types[(int)callIndirect.Type];
					writer.OpCode(instruction.OpCode).Space().FunctionSignature(type);
					break;
				}
				case MemoryImmediateInstruction memImm:
				{
					writer.OpCode(instruction.OpCode)
						.Space()
						.OpenBrace("(", CodeBracesRangeFlags.Parentheses)
						.Text("align")
						.Punctuation("=")
						.Number((int)Math.Pow(2, (int)memImm.Flags))
						.Punctuation(", ")
						.Text("offset")
						.Punctuation("=")
						.Number(memImm.Offset)
						.CloseBrace(")");
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
				case MemoryGrow:
				case MemorySize:
				case SimpleInstruction:
				{
					writer.OpCode(instruction.OpCode);
					break;
				}
				case VariableAccessInstruction va:
				{
					writer.OpCode(instruction.OpCode).Space();

					switch (va)
					{
						case LocalGet or LocalSet or LocalTee:
						{
							var local = vars.Locals[(int)va.Index];
							writer.Local(local.Name, local, false);
							break;
						}
						case GlobalGet or GlobalSet:
						{
							var global = vars.GetGlobal((int)va.Index);
							writer.Global(global.Name, global, false);
							break;
						}
						default:
							writer.Number(va.Index);
							break;
					}

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

	private class BlockReference
	{
		private readonly int _index;

		public BlockReference(int index)
		{
			_index = index;
		}

		public override string ToString()
		{
			return $"block_{_index}";
		}
	}
}
