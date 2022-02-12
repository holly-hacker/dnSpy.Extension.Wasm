using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using WebAssembly;

namespace dnSpy.Extension.Wasm.TreeView;

internal class WasmDocument : DsDocument
{
	public static readonly Guid MyGuid = new("4b25abff-9d40-4ce0-89d6-544128b8dbd1");

	public WasmDocument(string path)
	{
		Filename = path;

		// parse
		Module = Module.ReadFromBinary(path);
	}

	public Module Module { get; }

	/// <remarks>
	/// Returning non-null here implies we support serialization. I assume this means caching the decompiled output to
	/// disk so it won't have to happen again when reloading?
	/// </remarks>
	public override DsDocumentInfo? SerializedDocument => new DsDocumentInfo(Filename, MyGuid);

	public override IDsDocumentNameKey Key => new FilenameKey(Filename);
}

[Export(typeof(IDsDocumentProvider))]
internal class WasmDocumentProvider : IDsDocumentProvider
{
	public double Order => 0;

	public IDsDocument? Create(IDsDocumentService documentService, DsDocumentInfo documentInfo)
		=> CanCreateFor(documentService, documentInfo) ? new WasmDocument(documentInfo.Name) : null;

	public IDsDocumentNameKey? CreateKey(IDsDocumentService documentService, DsDocumentInfo documentInfo)
		=> CanCreateFor(documentService, documentInfo) ? new FilenameKey(documentInfo.Name) : null;

	private static bool CanCreateFor(IDsDocumentService documentService, DsDocumentInfo documentInfo)
	{
		// Handle existing wasm documents.
		if (documentInfo.Type == WasmDocument.MyGuid)
			return true;

		// Handle opening files that have the .wasm extension.
		if (documentInfo.Type == DocumentConstants.DOCUMENTTYPE_FILE &&
		    documentInfo.Name.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase))
			return true;

		return false;
	}
}

internal class WasmDocumentNode : DsDocumentNode, IDecompileSelf
{
	public static readonly Guid MyGuid = new("bd2d0a98-c7ec-4d2a-ad77-fcc75cc8944d");

	private readonly WasmDocument _document;

	public WasmDocumentNode(WasmDocument document) : base(document)
	{
		_document = document;
	}

	public override Guid Guid => MyGuid;

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.BinaryFile;

	/// <remarks> Writes to the tree view node and tab header text </remarks>
	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		output.WriteFilename(Path.GetFileName(_document.Filename));
	}

	/// <remarks> Writes to the editor pane </remarks>
	public bool Decompile(IDecompileNodeContext context)
	{
		context.ContentTypeString = Constants.ContentTypeWasmInfo;

		var items = new[]
		{
			("Functions", _document.Module.Functions.Count),
			("Codes", _document.Module.Codes.Count),
			("Types", _document.Module.Types.Count),
			("Imports", _document.Module.Imports.Count),
			("Exports", _document.Module.Exports.Count),
			("Tables", _document.Module.Tables.Count),
		};

		foreach ((string? name, int count) in items)
		{
			context.Output.Write(name, BoxedTextColor.Keyword);
			context.Output.Write(": ", BoxedTextColor.Operator);
			context.Output.Write(count.ToString(), BoxedTextColor.Number);
			context.Output.WriteLine();
		}

		return true;
	}

	public override IEnumerable<TreeNodeData> CreateChildren()
	{
		yield return new ExportsNode(_document.Module);
		yield return new FunctionsNode(_document.Module);
	}
}

[ExportDsDocumentNodeProvider]
internal class WasmDocumentNodeProvider : IDsDocumentNodeProvider
{
	public DsDocumentNode? Create(IDocumentTreeView documentTreeView, DsDocumentNode? owner, IDsDocument document)
	{
		return document is WasmDocument doc
			? new WasmDocumentNode(doc)
			: null;
	}
}