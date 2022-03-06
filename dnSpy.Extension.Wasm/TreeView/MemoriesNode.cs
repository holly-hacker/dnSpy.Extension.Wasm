using System;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

namespace dnSpy.Extension.Wasm.TreeView;

internal class MemoriesNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("3ce90685-853b-4141-bbac-096efc76ff1b");

	private readonly WasmDocument _document;

	public MemoriesNode(WasmDocument document)
	{
		_document = document;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MemoryWindow;

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		output.Write("Memories");
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		var writer = new DecompilerWriter(context.Output);

		for (var i = 0; i < _document.Module.Memories.Count; i++)
		{
			var moduleMemory = _document.Module.Memories[i];
			writer.Keyword("memory").Space().Number(i).Punctuation(": ")
				.Limits(moduleMemory.ResizableLimits).EndLine();
		}

		return true;
	}
}
