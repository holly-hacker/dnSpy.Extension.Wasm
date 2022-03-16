using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

	private int? _importedFunctionCount;
	private int? _importedTableCount;
	private int? _importedMemoryCount;
	private int? _importedGlobalCount;

	public WasmDocument(string path)
	{
		Filename = path;

		// parse
		Module = Module.ReadFromBinary(path);

		try
		{
			var nameSection = Module.CustomSections.SingleOrDefault(s => s.Name == "name" && s.PrecedingSection == Section.Data);
			if (nameSection != null)
			{
				NameSection = NameSection.Read(nameSection.Content.ToArray());
			}
		}
		catch (Exception e)
		{
			Debug.WriteLine("Failed to parse Name section: " + e);
		}
	}

	public Module Module { get; }
	public NameSection? NameSection { get; set; }

	public int ImportedFunctionCount => _importedFunctionCount ??= Module.Imports.OfType<Import.Function>().Count();
	public int ImportedTableCount => _importedTableCount ??= Module.Imports.OfType<Import.Table>().Count();
	public int ImportedMemoryCount => _importedMemoryCount ??= Module.Imports.OfType<Import.Memory>().Count();
	public int ImportedGlobalCount => _importedGlobalCount ??= Module.Imports.OfType<Import.Global>().Count();

	/// <remarks>
	/// Returning non-null here implies we support serialization. I assume this means caching the decompiled output to
	/// disk so it won't have to happen again when reloading?
	/// </remarks>
	public override DsDocumentInfo? SerializedDocument => new DsDocumentInfo(Filename, MyGuid);

	public override IDsDocumentNameKey Key => new FilenameKey(Filename);

	public void SaveToStream(Stream stream)
	{
		if (NameSection is not null)
		{
			var nameSection = Module.CustomSections.FirstOrDefault(s => s.PrecedingSection == Section.Data && s.Name == "name");
			if (nameSection is null)
			{
				nameSection = new CustomSection
				{
					Name = "name",
					PrecedingSection = Section.Data,
				};
				Module.CustomSections.Add(nameSection);
			}

			nameSection.Content = NameSection.ToList();
		}

		Module.WriteToBinary(stream);
	}

	public string GetFunctionNameFromSectionIndex(int index)
	{
		if (NameSection?.FunctionNames?.TryGetValue(index + ImportedFunctionCount, out string foundName) == true)
			return foundName!;

		var export = Module.Exports.SingleOrDefault(e => e.Kind == ExternalKind.Function && e.Index - ImportedFunctionCount == index);

		if (export is { })
			return export.Name;

		return $"function_{index}";
	}

	public string GetFunctionName(int fullIndex)
	{
		if (NameSection?.FunctionNames?.TryGetValue(fullIndex, out string foundName) == true)
			return foundName!;

		if (TryGetImport<Import.Function>(fullIndex, ImportedFunctionCount, out int sectionIndex) is { } import)
			return import.GetFullName();

		return GetFunctionNameFromSectionIndex(sectionIndex);
	}

	public WebAssemblyType GetFunctionType(int fullIndex)
	{
		uint typeIndex = TryGetImport<Import.Function>(fullIndex, ImportedFunctionCount, out int sectionIndex) switch
		{
			{ } import => import.TypeIndex,
			_ => Module.Functions[sectionIndex].Type,
		};

		return Module.Types[(int)typeIndex];
	}

	public string GetGlobalNameFromSectionIndex(int index)
	{
		var export = Module.Exports.SingleOrDefault(e => e.Kind == ExternalKind.Global && e.Index - ImportedGlobalCount == index);

		return export switch
		{
			{ } => export.Name,
			_ => $"global_{index}",
		};
	}

	public string GetGlobalName(int fullIndex)
	{
		return TryGetImport<Import.Global>(fullIndex, ImportedGlobalCount, out int sectionIndex) switch
		{
			{ } import => import.GetFullName(),
			_ => GetGlobalNameFromSectionIndex(sectionIndex),
		};
	}

	public WebAssemblyValueType GetGlobalType(int fullIndex)
	{
		return TryGetImport<Import.Global>(fullIndex, ImportedGlobalCount, out int sectionIndex) switch
		{
			{ } import => import.ContentType,
			_ => Module.Globals[sectionIndex].ContentType,
		};
	}

	public bool GetGlobalMutable(int fullIndex)
	{
		return TryGetImport<Import.Global>(fullIndex, ImportedGlobalCount, out int sectionIndex) switch
		{
			{ } import => import.IsMutable,
			_ => Module.Globals[sectionIndex].IsMutable,
		};
	}

	public string? TryGetLocalName(int function, int local)
	{
		return NameSection?.LocalNames?.TryGetValue(function, out var locals) == true
		       && locals?.TryGetValue(local, out string found) == true
			? found
			: null;
	}

	public T? TryGetImport<T>(int fullIndex, int importCount, out int sectionIndex) where T : Import
	{
		Debug.Assert(importCount <= Module.Imports.Count);

		if (fullIndex < importCount)
		{
			sectionIndex = -1;
			return Module.Imports.OfType<T>().Skip(fullIndex).FirstOrDefault()
			       ?? throw new Exception($"Could not find import of type {typeof(T)} and index {fullIndex}");
		}

		sectionIndex = fullIndex - importCount;
		return null;
	}
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

	public WasmDocumentNode(WasmDocument document) : base(document)
	{
	}

	public new WasmDocument Document => (WasmDocument)base.Document;

	public override Guid Guid => MyGuid;

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.BinaryFile;

	/// <remarks> Writes to the tree view node and tab header text </remarks>
	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		output.WriteFilename(options == DocumentNodeWriteOptions.ToolTip
			? Document.Filename
			: Path.GetFileName(Document.Filename));
	}

	/// <remarks> Writes to the editor pane </remarks>
	public bool Decompile(IDecompileNodeContext context)
	{
		context.ContentTypeString = Constants.ContentTypeWasmInfo;
		var writer = new DecompilerWriter(context.Output);

		writer.WriteFilename(Document.Filename).EndLine().EndLine();

		var items = new[]
		{
			("Functions", Document.Module.Functions.Count),
			("Elements", Document.Module.Elements.Count),
			("Globals", Document.Module.Globals.Count),
			("Types", Document.Module.Types.Count),
			("Imports", Document.Module.Imports.Count),
			("Exports", Document.Module.Exports.Count),
			("Tables", Document.Module.Tables.Count),
			("Memories", Document.Module.Memories.Count),
		};

		foreach ((string? name, int count) in items)
		{
			writer.Keyword(name).Punctuation(": ").Number(count);
			writer.EndLine();
		}

		if (Document.Module.Start != null)
		{
			var globalFunctionIndex = (int)Document.Module.Start.Value;

			var functionName = Document.GetFunctionName(globalFunctionIndex);
			var functionType = Document.GetFunctionType(globalFunctionIndex);

			writer.EndLine().Keyword("start").Punctuation(": ")
				.FunctionDeclaration(functionName, functionType, globalFunctionIndex);
		}

		return true;
	}

	public override IEnumerable<TreeNodeData> CreateChildren()
	{
		if (Document.Module.Data.Any())
			yield return new DatasNode(Document);
		if (Document.Module.Memories.Any())
			yield return new MemoriesNode(Document);
		if (Document.Module.Globals.Any())
			yield return new GlobalsNode(Document);
		if (Document.Module.Imports.Any())
			yield return new ImportsNode(Document);
		if (Document.Module.Exports.Any())
			yield return new ExportsNode(Document);
		if (Document.Module.Functions.Any())
			yield return new FunctionsNode(Document);
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
