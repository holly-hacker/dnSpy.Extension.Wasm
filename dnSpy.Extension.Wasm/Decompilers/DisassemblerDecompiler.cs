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

		if (vars.Locals.Any(l => !l.isParameter))
			writer.EndLine();

		WriteInstructions(vars, doc, writer, code);

		writer.DeIndent().CloseBrace("}").EndLine();
	}

	private void WriteLocals(VariableInfo vars, DecompilerWriter writer)
	{
		foreach ((string name, var type, _, var reference) in vars.Locals.Where(l => !l.isParameter))
		{
			writer.Local(name, reference, true).Punctuation(": ").Keyword(type.ToWasmType());
			writer.EndLine();
		}
	}

	private void WriteInstructions(VariableInfo vars, WasmDocument doc, DecompilerWriter writer, IList<Instruction> instructions)
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

					var flags = CodeBracesRangeFlags.BraceKind_CurlyBraces;
					flags |= CodeBracesRangeFlags.BlockKind_Other;

					writer.OpenBrace("{", flags).Indent();
					break;
				}
				case End:
				{
					writer.OpCode(instruction.OpCode);

					// only close current block if we're not at the last instruction
					if (i != instructions.Count - 1)
						writer.EndLine().DeIndent().CloseBrace("}");
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
						writer.Number(label).Space();

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
					writer.OpCode(instruction.OpCode).Space().Number(callIndirect.Type);
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
							writer.Local(local.name, local.reference, false);
							break;
						}
						case GlobalGet or GlobalSet:
						{
							var global = vars.GetGlobal((int)va.Index);
							writer.Global(global.name, global.reference, false);
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
}
