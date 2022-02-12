using System;
using System.Collections.Generic;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using WebAssembly;

namespace dnSpy.Extension.Wasm.TreeView;

internal class ExportsNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("1cfe7bfb-94e0-4399-a860-31b154bd0ba5");

	private readonly WasmDocument _document;

	public ExportsNode(WasmDocument document)
	{
		_document = document;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Output;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		output.Write("Exports");
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO: write list of functions with links
		return false;
	}

	public override IEnumerable<TreeNodeData> CreateChildren()
	{
		var module = _document.Module;
		foreach (var export in module.Exports)
		{
			switch (export.Kind)
			{
				case ExternalKind.Function:
					var function = module.Functions[(int)export.Index];
					var functionType = module.Types[(int)function.Type];
					yield return new FunctionExportNode(export.Name, functionType);
					break;
				case ExternalKind.Table:
					var table = module.Tables[(int)export.Index];
					yield return new TableExportNode(export.Name, table);
					break;
				case ExternalKind.Memory:
					var memory = module.Memories[(int)export.Index];
					yield return new MemoryExportNode(export.Name, memory);
					break;
				case ExternalKind.Global:
					var global = module.Globals[(int)export.Index];
					yield return new GlobalExportNode(export.Name, global);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}

internal class FunctionExportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("7e8e4b7f-7cc0-4caa-bd2f-b08705c7c0c7");

	private readonly string _name;
	private readonly WebAssemblyType _type;

	public FunctionExportNode(string name, WebAssemblyType type)
	{
		_name = name;
		_type = type;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MethodPublic;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		new TextColorWriter(output).FunctionDeclaration(_name, _type);
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO
		return false;
	}
}

internal class TableExportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("ffc6be04-00ff-4b5a-b30d-a89e636d6132");

	private readonly string _name;
	private readonly Table _table;

	public TableExportNode(string name, Table table)
	{
		_name = name;
		_table = table;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Metadata;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		// TODO
		new TextColorWriter(output)
			.Text("Table")
			.Space()
			.Text(_name)
			.Punctuation(":")
			.Space()
			.Text(_table.ToString());
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO
		return false;
	}
}

internal class MemoryExportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("9788005b-a75d-42e5-830b-672bffeb1437");

	private readonly string _name;
	private readonly Memory _memory;

	public MemoryExportNode(string name, Memory memory)
	{
		_name = name;
		_memory = memory;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MemoryWindow;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		// TODO
		new TextColorWriter(output)
			.Text("Memory")
			.Space()
			.Text(_name)
			.Punctuation(":")
			.Space()
			.Text(_memory.ToString());
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO
		return false;
	}
}

internal class GlobalExportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("b3f9e9d0-6d28-4fcb-8040-86598533b1f6");

	private readonly string _name;
	private readonly Global _global;

	public GlobalExportNode(string name, Global memory)
	{
		_name = name;
		_global = memory;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.ConstantPublic;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		// TODO
		new TextColorWriter(output)
			.Text("Global")
			.Space()
			.Text(_name)
			.Punctuation(":")
			.Space()
			.Text(_global.ToString());
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO
		return false;
	}
}
