using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;

namespace dnSpy.Extension.Wasm.Decompilers.References;

[ExportReferenceDocumentTabContentProvider(Order = 0)]
public class ReferenceDocumentTabContentProvider : IReferenceDocumentTabContentProvider
{
	public DocumentTabReferenceResult? Create(IDocumentTabService documentTabService, DocumentTabContent? sourceContent,
		object? @ref)
	{
		if (@ref is TextReference { Reference: IWasmReference } textReference)
		{
			var node = documentTabService.DocumentTreeView.FindNode(textReference.Reference);
			if (node == null)
				return null;

			var content = documentTabService.TryCreateContent(new[] { node });
			if (content == null)
				return null;

			return new DocumentTabReferenceResult(content);
		}
		return null;
	}
}
