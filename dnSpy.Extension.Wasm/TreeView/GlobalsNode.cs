using System;
using System.Collections.Generic;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Extension.Wasm.Decompilers;
using WebAssembly;

namespace dnSpy.Extension.Wasm.TreeView;

internal class GlobalsNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("9bfd4f05-9a4c-4bf0-9ff6-542366876bfa");

	private readonly WasmDocument _document;

	public GlobalsNode(WasmDocument document)
	{
		_document = document;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.LocalVariable;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		output.Write("Globals");
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO
		return false;
	}

	public override IEnumerable<TreeNodeData> CreateChildren()
	{
		return _document.Module.Globals.Select((global, i) => new GlobalNode(_document, global, i));
	}
}

internal class GlobalNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("e563d665-0da8-45cc-ab82-5cb2e5a4fcb4");

	private readonly WasmDocument _document;
	private readonly Global _global;
	private readonly int _index;

	public GlobalNode(WasmDocument document, Global global, int index)
	{
		_document = document;
		_global = global;
		_index = index;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	// TODO: different icons depending on global type (i32, i64, f32, f64)
	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.ConstantPublic;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		string name = _document.GetGlobalNameFromSectionIndex(_index);
		var writer = new TextColorWriter(output);

		writer.Keyword("global").Space().Text(name).Punctuation(": ");
		if (_global.IsMutable)
			writer.Keyword("mut").Space();
		writer.Keyword(_global.ContentType.ToWasmType());
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		string name = _document.GetGlobalNameFromSectionIndex(_index);
		var writer = new DecompilerWriter(context.Output);

		// same as above
		writer.Keyword("global").Space().Text(name).Punctuation(": ");
		if (_global.IsMutable)
			writer.Keyword("mut").Space();
		writer.Keyword(_global.ContentType.ToWasmType()).EndLine().EndLine();

		var disassembler = new DisassemblerDecompiler();
		disassembler.Decompile(_document, writer, "initialize", new List<Local>(), _global.InitializerExpression, new WebAssemblyType
		{
			Form = FunctionType.Function,
			Parameters = new List<WebAssemblyValueType>(),
			Returns = new List<WebAssemblyValueType> { _global.ContentType },
		});

		return true;
	}
}
