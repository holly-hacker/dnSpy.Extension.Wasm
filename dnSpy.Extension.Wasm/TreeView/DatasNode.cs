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

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MemoryWindow;

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
		if (_data.InitializerExpression.Any())
			yield return new InstructionListNode(_wasmDocument, _data.InitializerExpression, "Initializer");
	}
}
