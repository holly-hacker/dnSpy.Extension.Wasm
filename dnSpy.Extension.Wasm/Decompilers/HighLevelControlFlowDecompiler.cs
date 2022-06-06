using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Extension.Wasm.Decompiling;
using dnSpy.Extension.Wasm.TreeView;
using HoLLy.Decompiler.Core.Analysis;
using HoLLy.Decompiler.Core.Analysis.AST;
using HoLLy.Decompiler.Core.FrontEnd;
using WebAssembly;

namespace dnSpy.Extension.Wasm.Decompilers;

[Export(typeof(IWasmDecompiler))]
internal class HighLevelControlFlowDecompiler : IWasmDecompiler
{
	public string Name => "[DEBUG] HL Control Flow AST";
	public int Order => 4;

	public void Decompile(WasmDocument doc, DecompilerWriter writer, string name, IList<Local> locals, IList<Instruction> code,
		WebAssemblyType functionType, int? globalIndex = null)
	{
		IDecompilerFrontend converter = new WasmDecompilerFrontend(doc, locals, code, functionType);
		var instructions = converter.Convert();
		var astGen = new AstGenerator(instructions);
		var ast = astGen.CreateControlFlowAst();

		if (ast.GetNodes().Count() != 1)
			throw new Exception("Failed to reduce the CFG to a single AST node");

		var node = ast.GetNodes().Single().ControlFlowNode;
		Write(writer, node);
	}

	private static void Write(DecompilerWriter writer, IHighLevelControlFlowNode node)
	{
		switch (node)
		{
			case IfThenNode ifThen:
			{
				Write(writer, ifThen.Head);
				writer.Keyword("if").Space().Keyword(ifThen.LoopCondition ? "true" : "false").Space()
					.OpenBrace("{", CodeBracesRangeFlags.ConditionalBraces).EndLine().Indent();
				Write(writer, ifThen.OnCondition);
				writer.DeIndent().CloseBrace("}").EndLine();
				break;
			}
			case IfThenElseNode ifThenElse:
			{
				Write(writer, ifThenElse.Head);
				writer.Keyword("if").Space().Keyword("true").Space()
					.OpenBrace("{", CodeBracesRangeFlags.ConditionalBraces).EndLine().Indent();
				Write(writer, ifThenElse.OnTrue);
				writer.DeIndent().CloseBrace("}").Space().Keyword("else").Space()
					.OpenBrace("{", CodeBracesRangeFlags.ConditionalBraces).EndLine().Indent();
				Write(writer, ifThenElse.OnFalse);
				writer.DeIndent().CloseBrace("}").EndLine();
				break;
			}
			case WhileNode @while:
			{
				writer.Keyword("while").Space().Keyword(@while.LoopCondition ? "true" : "false").Space()
					.OpenBrace("{", CodeBracesRangeFlags.ConditionalBraces).EndLine().Indent();
				writer.Comment("// loop condition start").EndLine();
				Write(writer, @while.Head);
				writer.Comment("// loop condition end").EndLine();
				Write(writer, @while.LoopBody);
				writer.DeIndent().CloseBrace("}").EndLine();
				break;
			}
			case DoWhileNode doWhile:
			{
				writer.Keyword("do").Space().OpenBrace("{", CodeBracesRangeFlags.ConditionalBraces).EndLine().Indent();
				Write(writer, doWhile.Head);
				writer.DeIndent().CloseBrace("}").Space()
					.Keyword("while").Space().Keyword(doWhile.LoopCondition ? "true" : "false").EndLine();
				break;
			}
			case SequenceNode sequence:
			{
				foreach (var listNode in sequence.Nodes)
					Write(writer, listNode);
				break;
			}
			case IntermediateInstructionListNode list:
			{
				foreach (var instruction in list.Instructions)
				{
					writer.Text(instruction.GetType().Name).Punctuation(";").EndLine();
				}
				break;
			}
			default:
			{
				throw new NotSupportedException(node.GetType().ToString());
			}
		}
	}
}
