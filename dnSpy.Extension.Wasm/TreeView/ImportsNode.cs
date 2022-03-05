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
		return _document.Module.Imports.Select(import => (TreeNodeData)(import switch
		{
			Import.Function function => new FunctionImportNode(function),
			Import.Table table => new TableImportNode(table),
			Import.Memory memory => new MemoryImportNode(memory),
			Import.Global global => new GlobalImportNode(global),
			_ => throw new ArgumentOutOfRangeException(),
		}));
	}
}

internal class FunctionImportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("97a6413a-36b5-42b4-b490-fb6b8e0cd713");

	public readonly Import.Function Function;

	public FunctionImportNode(Import.Function function)
	{
		Function = function;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MethodPublic;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		// TODO
		new TextColorWriter(output).Text(Function.ToString());
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO
		return false;
	}
}

internal class TableImportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("bc0ccbd9-4445-4484-b6ae-f32b69203928");

	private readonly Import.Table _table;

	public TableImportNode(Import.Table table)
	{
		_table = table;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Metadata;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		// TODO
		new TextColorWriter(output).Text(_table.ToString());
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO
		return false;
	}
}

internal class MemoryImportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("88e710e0-8ed1-4152-a161-c9f66fb539f7");

	private readonly Import.Memory _memory;

	public MemoryImportNode(Import.Memory memory)
	{
		_memory = memory;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MemoryWindow;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		// TODO
		new TextColorWriter(output).Text(_memory.ToString());
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO
		return false;
	}
}

internal class GlobalImportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("264203ea-16c1-4c38-b6bd-f99bd5ca4c49");

	public GlobalImportNode(Import.Global global)
	{
		Global = global;
	}

	public Import.Global Global { get; }

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.ConstantPublic;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		// TODO
		new TextColorWriter(output).Text(Global.ToString());
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO
		return false;
	}
}
