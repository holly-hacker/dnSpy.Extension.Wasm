using System.Linq;
using dnSpy.Contracts.Documents.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Extension.Wasm.TreeView;
using WebAssembly;

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
				toolTipProvider.Image = local.IsParameter ? DsImages.Parameter : DsImages.LocalVariable;
				var writer = new TextColorWriter(toolTipProvider.Output);
				writer.Keyword(local.IsParameter ? "param" : "local").Space()
					.Local(local.Name, local, false).Punctuation(": ")
					.Type(local.Type);
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
				writer.Type(global.Type);
				return toolTipProvider.Create();
			}
			default:
				return null;
		}
	}
}

[ExportDocumentViewerToolTipProvider]
public class MethodTooltipProvider : IDocumentViewerToolTipProvider
{
	public object? Create(IDocumentViewerToolTipProviderContext context, object? @ref)
	{
		if (@ref is FunctionReference fun)
		{
			// TODO: when selecting multiple nodes from different documents, this may select the wrong document!
			// include a document key to combat this? get the actual treenode?
			if (context.DocumentViewer.DocumentTab?.Content.Nodes.Select(n => n.GetDocumentNode()).Distinct().Count() > 1)
				return null;

			var documentNode = context.DocumentViewer.DocumentTab?.Content.Nodes
				.Select(n => n.GetDocumentNode())
				.OfType<WasmDocumentNode>().FirstOrDefault();
			var document = documentNode?.Document;

			if (document != null)
			{

				var toolTipProvider = context.Create();
				toolTipProvider.Image = DsImages.MethodPublic;
				var writer = new TextColorWriter(toolTipProvider.Output);

				var importedFunc = document.TryGetImport<Import.Function>(fun.GlobalFunctionIndex,
					document.ImportedFunctionCount, out int sectionIndex);

				if (importedFunc is not null)
				{
					var type = document.Module.Types[(int)importedFunc.TypeIndex];
					writer.Keyword("import").Space()
						.FunctionDeclaration(importedFunc.GetFullName(), type, fun.GlobalFunctionIndex);
				}
				else
				{
					var export = document.Module.Exports.FirstOrDefault(e => e.Kind == ExternalKind.Function && e.Index == sectionIndex);
					if (export is not null)
						writer.Keyword("export").Space();

					var functionName = document.GetFunctionNameFromSectionIndex(sectionIndex);
					var functionTypeIndex = document.Module.Functions[sectionIndex].Type;
					var functionType = document.Module.Types[(int)functionTypeIndex];

					writer.FunctionDeclaration(functionName, functionType, fun.GlobalFunctionIndex);
				}

				return toolTipProvider.Create();
			}
		}

		return null;
	}
}
