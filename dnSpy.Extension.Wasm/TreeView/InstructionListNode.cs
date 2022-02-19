using System;
using System.Collections.Generic;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Extension.Wasm.Decompilers;
using WebAssembly;

namespace dnSpy.Extension.Wasm.TreeView;

internal class InstructionListNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("dbf2fa46-d3a7-4ae4-90ff-96a20be6bff1");

	private readonly WasmDocument _document;
	private readonly IList<Instruction> _instructions;
	private readonly string _name;

	public InstructionListNode(WasmDocument document, IList<Instruction> instructions, string name)
	{
		_document = document;
		_instructions = instructions;
		_name = name;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Assembly;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		output.Write(_name);
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		var writer = new DecompilerWriter(context.Output);

		new DisassemblerDecompiler().WriteInstructions(writer, _instructions);

		return true;
	}
}
