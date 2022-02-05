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

namespace dnSpy.Extension.Wasm;

internal class FunctionsNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("f98e7381-444e-43aa-882d-84dcde4c56c9");

	private readonly Module _module;

	public FunctionsNode(Module module)
	{
		_module = module;
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

		var mod = _module;
		for (var i = 0; i < mod.Functions.Count; i++)
		{
			var function = mod.Functions[i];
			var code = mod.Codes[i];
			var type = mod.Types[(int)function.Type];

			yield return new FunctionNode(i, code, type);
		}
	}
}

internal class FunctionNode : DocumentTreeNodeData, IDecompileSelf
{
	public static readonly Guid MyGuid = new("b2cdecbc-7f8b-464e-b3d2-fa2be4c3f68c");

	private readonly int _index;
	private readonly FunctionBody _code;
	private readonly WebAssemblyType _type;

	public FunctionNode(int index, FunctionBody code, WebAssemblyType type)
	{
		_index = index;
		_code = code;
		_type = type;
	}

	public override Guid Guid => MyGuid;
	public override NodePathName NodePathName => new(Guid);

	protected override ImageReference GetIcon(IDotNetImageService dnImgMgr)
	{
		return DsImages.MethodPublic;
	}

	protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options)
	{
		output.Write(BoxedTextColor.StaticMethod, $"func_{_index}");

		output.Write(BoxedTextColor.Punctuation, "(");
		bool firstParameter = true;
		foreach (var parameter in _type.Parameters)
		{
			if (!firstParameter)
				output.Write(BoxedTextColor.Punctuation, ", ");
			firstParameter = false;

			output.Write(BoxedTextColor.Keyword, parameter.ToWasmType());
		}

		output.Write(BoxedTextColor.Punctuation, ")");

		if (_type.Returns.Any())
		{
			output.Write(BoxedTextColor.Punctuation, ": ");

			if (_type.Returns.Count > 1) output.Write(BoxedTextColor.Punctuation, "(");

			bool firstReturnParameter = true;
			foreach (var returnParameter in _type.Returns)
			{
				if (!firstReturnParameter)
					output.Write(BoxedTextColor.Punctuation, ", ");
				firstReturnParameter = false;

				output.Write(BoxedTextColor.Keyword, returnParameter.ToWasmType());
			}

			if (_type.Returns.Count > 1) output.Write(BoxedTextColor.Punctuation, ")");
		}
	}

	public bool Decompile(IDecompileNodeContext context)
	{
		var dec = new DisassemblerDecompiler();
		dec.Decompile(context, _index, _code, _type);

		return true;
	}
}
