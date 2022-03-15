using System;
using System.Collections.Generic;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Extension.Wasm.Decompilers;
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
					yield return new FunctionExportNode(_document, export);
					break;
				case ExternalKind.Table:
					yield return new TableExportNode(_document, export);
					break;
				case ExternalKind.Memory:
					yield return new MemoryExportNode(_document, export);
					break;
				case ExternalKind.Global:
					yield return new GlobalExportNode(_document, export);
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

	private readonly WasmDocument _document;
	private readonly Export _export;

	public FunctionExportNode(WasmDocument document, Export export)
	{
		_document = document;
		_export = export;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MethodPublic;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		var name = _document.GetFunctionName((int)_export.Index);
		var type = _document.GetFunctionType((int)_export.Index);
		new TextColorWriter(output).FunctionDeclaration(name, type);
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		if (_export.Index < _document.ImportedFunctionCount)
			return false;

		var writer = new DecompilerWriter(context.Output);
		var decompiler = new DisassemblerDecompiler();
		decompiler.DecompileByFunctionIndex(_document, writer, (int)_export.Index - _document.ImportedFunctionCount);
		return true;
	}
}

internal class TableExportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("ffc6be04-00ff-4b5a-b30d-a89e636d6132");

	private readonly WasmDocument _document;
	private readonly Export _export;

	public TableExportNode(WasmDocument document, Export export)
	{
		_document = document;
		_export = export;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	private Table Table => _document.Module.Tables[(int)_export.Index - _document.ImportedTableCount];

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Metadata;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		new TextColorWriter(output)
			.Keyword("table").Space()
			.Text(_export.Name).Punctuation(": ")
			.Limits(Table.ResizableLimits);
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		new DecompilerWriter(context.Output)
			.Keyword("export").Space()
			.Keyword("table").Space()
			.Text(_export.Name).Punctuation(": ")
			.Limits(Table.ResizableLimits);
		return true;
	}
}

internal class MemoryExportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("9788005b-a75d-42e5-830b-672bffeb1437");

	private readonly WasmDocument _document;
	private readonly Export _export;

	public MemoryExportNode(WasmDocument document, Export export)
	{
		_document = document;
		_export = export;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	private Memory Memory => _document.Module.Memories[(int)_export.Index - _document.ImportedMemoryCount];

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MemoryWindow;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		new TextColorWriter(output)
			.Keyword("memory").Space()
			.Text(_export.Name).Punctuation(": ")
			.Limits(Memory.ResizableLimits);
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		new DecompilerWriter(context.Output)
			.Keyword("export").Space()
			.Keyword("memory").Space()
			.Text(_export.Name).Punctuation(": ")
			.Limits(Memory.ResizableLimits);
		return true;
	}
}

internal class GlobalExportNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("b3f9e9d0-6d28-4fcb-8040-86598533b1f6");

	private readonly WasmDocument _document;
	private readonly Export _export;

	public GlobalExportNode(WasmDocument document, Export export)
	{
		_document = document;
		_export = export;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	private Global Global => _document.Module.Globals[(int)_export.Index - _document.ImportedGlobalCount];

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.ConstantPublic;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		var writer = new TextColorWriter(output);

		writer.Keyword("global").Space()
			.Text(_export.Name).Punctuation(": ");
		if (Global.IsMutable)
			writer.Keyword("mut").Space();
		writer.Type(Global.ContentType);
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		var writer = new DecompilerWriter(context.Output);

		// same as above
		writer.Keyword("export").Space()
			.Keyword("global").Space()
			.Text(_export.Name).Punctuation(": ");
		if (Global.IsMutable)
			writer.Keyword("mut").Space();
		writer.Type(Global.ContentType).EndLine().EndLine();

		var disassembler = new DisassemblerDecompiler();
		disassembler.Decompile(_document, writer, "initialize", new List<Local>(), Global.InitializerExpression, new WebAssemblyType
		{
			Form = FunctionType.Function,
			Parameters = new List<WebAssemblyValueType>(),
			Returns = new List<WebAssemblyValueType> { Global.ContentType },
		});

		return true;
	}
}
