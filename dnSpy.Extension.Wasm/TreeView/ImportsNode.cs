using System;
using System.Collections.Generic;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using WebAssembly;

namespace dnSpy.Extension.Wasm.TreeView;

internal class ImportsNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("d09cbe50-b61a-4af2-9e1c-711b4c750ec4");

	private readonly WasmDocument _document;

	public ImportsNode(WasmDocument document)
	{
		_document = document;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.DownloadNoColor;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		output.Write("Imports");
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO
		return false;
	}

	public override IEnumerable<TreeNodeData> CreateChildren()
	{
		var (functionIndex, tableIndex, memoryIndex, globalIndex) = (0, 0, 0, 0);
		return _document.Module.Imports.Select(import => (TreeNodeData)(import switch
		{
			Import.Function function => new FunctionImportNode(_document, function, functionIndex++),
			Import.Table table => new TableImportNode(table, tableIndex++),
			Import.Memory memory => new MemoryImportNode(memory, memoryIndex++),
			Import.Global global => new GlobalImportNode(global, globalIndex++),
			_ => throw new ArgumentOutOfRangeException()
		}));
	}
}

internal class FunctionImportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("97a6413a-36b5-42b4-b490-fb6b8e0cd713");

	private readonly WasmDocument _document;
	private readonly Import.Function _function;

	public FunctionImportNode(WasmDocument document, Import.Function function, int functionIndex)
	{
		FunctionIndex = functionIndex;
		_document = document;
		_function = function;
	}

	public int FunctionIndex { get; }

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MethodPublic;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		var writer = new TextColorWriter(output);

		var type = _document.Module.Types[(int)_function.TypeIndex];
		writer.FunctionDeclaration(_function.GetFullName(), type);
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		var writer = new DecompilerWriter(context.Output);

		var type = _document.Module.Types[(int)_function.TypeIndex];
		writer.Keyword("import").Space()
			.FunctionDeclaration(_function.GetFullName(), type);

		return true;
	}
}

internal class TableImportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("bc0ccbd9-4445-4484-b6ae-f32b69203928");

	private readonly Import.Table _table;

	public TableImportNode(Import.Table table, int tableIndex)
	{
		TableIndex = tableIndex;
		_table = table;
	}

	public int TableIndex { get; }

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Metadata;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		new TextColorWriter(output)
			.Keyword("table").Space()
			.Text(_table.GetFullName()).Punctuation(": ")
			.Limits(_table.Definition!.ResizableLimits);
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		new DecompilerWriter(context.Output)
			.Keyword("import").Space()
			.Keyword("table").Space()
			.Text(_table.GetFullName()).Punctuation(": ")
			.Limits(_table.Definition!.ResizableLimits);
		return true;
	}
}

internal class MemoryImportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("88e710e0-8ed1-4152-a161-c9f66fb539f7");

	private readonly Import.Memory _memory;

	public MemoryImportNode(Import.Memory memory, int memoryIndex)
	{
		MemoryIndex = memoryIndex;
		_memory = memory;
	}

	public int MemoryIndex { get; }

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MemoryWindow;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		new TextColorWriter(output)
			.Keyword("memory").Space()
			.Text(_memory.GetFullName()).Punctuation(": ")
			.Limits(_memory.Type!.ResizableLimits);
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		new DecompilerWriter(context.Output)
			.Keyword("import").Space()
			.Keyword("memory").Space()
			.Text(_memory.GetFullName()).Punctuation(": ")
			.Limits(_memory.Type!.ResizableLimits);
		return true;
	}
}

internal class GlobalImportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("264203ea-16c1-4c38-b6bd-f99bd5ca4c49");

	private readonly Import.Global _global;

	public GlobalImportNode(Import.Global global, int globalIndex)
	{
		_global = global;
		GlobalIndex = globalIndex;
	}

	public int GlobalIndex { get; }

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.ConstantPublic;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		var writer = new TextColorWriter(output);

		writer.Keyword("global").Space()
			.Text(_global.GetFullName()).Punctuation(": ");
		if (_global.IsMutable)
			writer.Keyword("mut").Space();
		writer.Type(_global.ContentType);
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		var writer = new DecompilerWriter(context.Output);

		writer.Keyword("import").Space()
			.Keyword("global").Space()
			.Text(_global.GetFullName()).Punctuation(": ");
		if (_global.IsMutable)
			writer.Keyword("mut").Space();
		writer.Type(_global.ContentType);

		return true;
	}
}
