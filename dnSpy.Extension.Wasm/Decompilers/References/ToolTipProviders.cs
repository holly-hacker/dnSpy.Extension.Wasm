using dnSpy.Contracts.Documents.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Images;

namespace dnSpy.Extension.Wasm.Decompilers.References;

[ExportDocumentViewerToolTipProvider]
public class LocalGlobalToolTipProvider : IDocumentViewerToolTipProvider
{
	public object? Create(IDocumentViewerToolTipProviderContext context, object? @ref)
	{
		switch (@ref)
		{
			case LocalReference local:
			{
				var toolTipProvider = context.Create();
				toolTipProvider.Image = local.IsArgument ? DsImages.Parameter : DsImages.LocalVariable;
				var writer = new TextColorWriter(toolTipProvider.Output);
				writer.Keyword(local.IsArgument ? "param" : "local").Space()
					.Local(local.Name, local, false).Punctuation(": ")
					.Keyword(local.Type.ToWasmType());
				return toolTipProvider.Create();
			}
			case GlobalReference global:
			{
				var toolTipProvider = context.Create();
				toolTipProvider.Image = DsImages.ConstantPublic;
				var writer = new TextColorWriter(toolTipProvider.Output);
				writer.Keyword("global").Space()
					.Global(global.Name, global, false).Punctuation(": ");
				if (global.Mutable) writer.Keyword("mut").Space();
				writer.Keyword(global.Type.ToWasmType());
				return toolTipProvider.Create();
			}
			default:
				return null;
		}
	}
}
