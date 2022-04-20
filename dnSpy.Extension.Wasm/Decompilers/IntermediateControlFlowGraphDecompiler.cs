using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Extension.Wasm.Decompiling;
using dnSpy.Extension.Wasm.TreeView;
using HoLLy.Decompiler.Core.Analysis;
using HoLLy.Decompiler.Core.FrontEnd;
using WebAssembly;

namespace dnSpy.Extension.Wasm.Decompilers;

[Export(typeof(IWasmDecompiler))]
internal class IntermediateControlFlowGraphDecompiler : IWasmDecompiler
{
	public string Name => "[DEBUG] IR Control Flow DotGraph";
	public int Order => 2;

	public void Decompile(WasmDocument doc, DecompilerWriter writer, string name, IList<Local> locals, IList<Instruction> code,
		WebAssemblyType functionType, int? globalIndex = null)
	{
		try
		{
			IDecompilerFrontend converter = new WasmDecompilerFrontend(doc, locals, code, functionType);
			var instructions = converter.Convert();
			var astGen = new AstGenerator(instructions);
			string text = astGen.CreateCfgDotString();

			writer.Text(text);
		}
		catch (Exception e)
		{
			writer.Text(e.ToString());
		}
	}
}
