using System;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Extension.Wasm.TreeView;

namespace dnSpy.Extension.Wasm.Decompilers;

internal interface IWasmDecompilerService
{
	IWasmDecompiler[] AllDecompilers { get; }
	IWasmDecompiler CurrentDecompiler { get; }

	void SetCurrentDecompiler(IWasmDecompiler decompiler);
}

[Export(typeof(IWasmDecompilerService))]
internal class WasmDecompilerService : IWasmDecompilerService
{
	private readonly Lazy<IDocumentTabService> _documentTabService;

	[ImportingConstructor]
	public WasmDecompilerService([ImportMany] IWasmDecompiler[] decompilers, Lazy<IDocumentTabService> documentTabService)
	{
		_documentTabService = documentTabService;
		AllDecompilers = decompilers.OrderBy(d => d.Order).ToArray();
		CurrentDecompiler = AllDecompilers.First();
	}

	public IWasmDecompiler[] AllDecompilers { get; }

	public IWasmDecompiler CurrentDecompiler { get; private set; }

	public void SetCurrentDecompiler(IWasmDecompiler decompiler)
	{
		CurrentDecompiler = decompiler;

		var documentService = _documentTabService.Value;
		var documentTab = documentService.ActiveTab;
		bool isWasmDoc = documentTab?.Content.Nodes.OfType<WasmDocumentTreeNodeData>().Any() == true;

		if (isWasmDoc)
			documentService.Refresh<WasmDocumentTreeNodeData>();
	}
}
