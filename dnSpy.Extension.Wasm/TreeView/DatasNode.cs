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

internal class DatasNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("af4d9d2f-7be2-4b6e-bdcf-a6212759567d");

	private readonly WasmDocument _document;

	public DatasNode(WasmDocument document)
	{
		_document = document;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Binary;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		output.Write("Data");
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO
		return false;
	}

	public override IEnumerable<TreeNodeData> CreateChildren()
	{
		return _document.Module.Data.Select((data, i) => new DataNode(_document, data, i));
	}
}

internal class DataNode : HexViewerNode
{
	public static readonly Guid MyGuid = new("893542d1-d1bc-4a03-bcaa-70d288e28189");

	private readonly WasmDocument _wasmDocument;
	private readonly Data _data;
	private readonly int _index;

	public DataNode(WasmDocument wasmDocument, Data data, int index)
	{
		_wasmDocument = wasmDocument;
		_data = data;
		_index = index;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	public override byte[] GetHexData() => _data.RawData.ToArray();

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MemoryWindow;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		var writer = new TextColorWriter(output);
		writer.Keyword("data").Space().Number(_index);
	}

	public override string GetName() => $"data {_index}";

	public override IEnumerable<TreeNodeData> CreateChildren()
	{
		yield return new DataInitializerNode(_wasmDocument, _data);
	}
}

internal class DataInitializerNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("dbf2fa46-d3a7-4ae4-90ff-96a20be6bff1");

	private readonly WasmDocument _document;
	private readonly Data _data;

	public DataInitializerNode(WasmDocument document, Data data)
	{
		_document = document;
		_data = data;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Assembly;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		output.Write("Initializer");
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		var writer = new DecompilerWriter(context.Output);

		var disassembler = new DisassemblerDecompiler();
		disassembler.Decompile(_document, writer, "get_offset", new List<Local>(), _data.InitializerExpression, new WebAssemblyType
		{
			Form = FunctionType.Function,
			Parameters = new List<WebAssemblyValueType>(),
			Returns = new List<WebAssemblyValueType> { WebAssemblyValueType.Int32 },
		});

		return true;
	}
}
