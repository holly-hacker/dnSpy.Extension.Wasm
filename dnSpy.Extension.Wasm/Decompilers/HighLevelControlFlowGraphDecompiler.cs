using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using dnSpy.Extension.Wasm.Decompiling;
using dnSpy.Extension.Wasm.TreeView;
using Echo.Core.Graphing;
using Echo.Core.Graphing.Serialization.Dot;
using HoLLy.Decompiler.Core.Analysis;
using HoLLy.Decompiler.Core.FrontEnd;
using WebAssembly;

namespace dnSpy.Extension.Wasm.Decompilers;

[Export(typeof(IWasmDecompiler))]
internal class HighLevelControlFlowGraphDecompiler : IWasmDecompiler
{
	public string Name => "[DEBUG] HL Control Flow DotGraph";
	public int Order => 3;

	public void Decompile(WasmDocument doc, DecompilerWriter writer, string name, IList<Local> locals, IList<Instruction> code,
		WebAssemblyType functionType, int? globalIndex = null)
	{
		IDecompilerFrontend converter = new WasmDecompilerFrontend(doc, locals, code, functionType);
		var instructions = converter.Convert();
		var astGen = new AstGenerator(instructions);
		var ast = astGen.CreateControlFlowAst();

		var sw = new StringWriter();
		var dw = new DotWriter(sw) { NodeAdorner = new StandardNodeAdorner(), };
		dw.Write(ast);

		writer.Text(sw.ToString());
	}

	private class StandardNodeAdorner : IDotNodeAdorner
	{
		public IDictionary<string, string> GetNodeAttributes(INode node, long id) => new Dictionary<string, string>
			{ ["label"] = node.ToString()!, };
	}
}
