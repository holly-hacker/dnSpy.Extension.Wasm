using System;
using System.Collections.Generic;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Extension.Wasm.Decompilers;

namespace dnSpy.Extension.Wasm.TreeView;

internal class FunctionsNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("f98e7381-444e-43aa-882d-84dcde4c56c9");

	private readonly WasmDocument _document;

	public FunctionsNode(WasmDocument document)
	{
		_document = document;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Library;

	public override void Initialize()
	{
		TreeNode.LazyLoading = true;
	}

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		output.Write("Functions");
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		// TODO: write list of functions with links
		return false;
	}

	public override IEnumerable<TreeNodeData> CreateChildren()
	{
		// this would have been ideal but doesn't seem to work
		// if (this.GetDocumentNode()?.Document is not WasmDocument wasmDocument)
		// 	yield break;

		var module = _document.Module;

		for (var i = 0; i < module.Functions.Count; i++)
		{
			yield return new FunctionNode(_document, i);
		}
	}
}

internal class FunctionNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("b2cdecbc-7f8b-464e-b3d2-fa2be4c3f68c");

	private readonly WasmDocument _document;
	public readonly int Index;

	public FunctionNode(WasmDocument document, int index)
	{
		_document = document;
		Index = index;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr)
	{
		return DsImages.MethodPublic;
	}

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		var name = _document.GetFunctionNameFromSectionIndex(Index);
		var function = _document.Module.Functions[Index];
		var type = _document.Module.Types[(int)function.Type];
		new TextColorWriter(output).FunctionDeclaration(name, type);
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		var writer = new DecompilerWriter(context.Output);
		var dec = new DisassemblerDecompiler();
		dec.DecompileByFunctionIndex(_document, writer, Index);

		return true;
	}
}
