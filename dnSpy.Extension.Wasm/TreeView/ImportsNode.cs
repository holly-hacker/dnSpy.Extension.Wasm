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

internal class ImportsNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("d09cbe50-b61a-4af2-9e1c-711b4c750ec4");

	private readonly Module _module;

	public ImportsNode(Module module)
	{
		_module = module;
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
		foreach (var import in _module.Imports)
		{
			switch (import)
			{
				case Import.Function function:
					yield return new FunctionImportNode(function);
					break;
				case Import.Table table:
					yield return new TableImportNode(table);
					break;
				case Import.Memory memory:
					yield return new MemoryImportNode(memory);
					break;
				case Import.Global global:
					yield return new GlobalImportNode(global);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}

internal class FunctionImportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("97a6413a-36b5-42b4-b490-fb6b8e0cd713");

	private readonly Import.Function _function;

	public FunctionImportNode(Import.Function function)
	{
		_function = function;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MethodPublic;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		// TODO
		new TextColorWriter(output).Text(_function.ToString());
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

	private readonly Import.Global _global;

	public GlobalImportNode(Import.Global global)
	{
		_global = global;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.ConstantPublic;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		// TODO
		new TextColorWriter(output).Text(_global.ToString());
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO
		return false;
	}
}
