using System.Collections.Generic;
using dnSpy.Extension.Wasm.Decompilers.References;
using dnSpy.Extension.Wasm.TreeView;
using WebAssembly;

namespace dnSpy.Extension.Wasm.Decompilers;

internal class VariableInfo
{
	private readonly WasmDocument _document;
	private readonly int? _globalFunctionIndex;

	private readonly Dictionary<int, (string name, WebAssemblyValueType type, GlobalReference reference)> _globals = new();

	private readonly List<(string name, WebAssemblyValueType type, bool isParameter, LocalReference reference)> _locals = new();

	public VariableInfo(WasmDocument document, IList<Local> locals, WebAssemblyType functionType, int? globalFunctionIndex)
	{
		_document = document;
		_globalFunctionIndex = globalFunctionIndex;

		for (var i = 0; i < functionType.Parameters.Count; i++)
		{
			var paramType = functionType.Parameters[i];
			AddParam(paramType, i);
		}

		var localIndex = 0;
		foreach (var local in locals)
			for (var i = 0; i < local.Count; i++)
				AddLocal(local.Type, localIndex++);
	}

	public IReadOnlyList<(string name, WebAssemblyValueType type, bool isParameter, LocalReference reference)> Locals => _locals.AsReadOnly();

	public int ParamCount { get; private set; }

	private void AddParam(WebAssemblyValueType type, int i)
	{
		string? name = _globalFunctionIndex.HasValue
			? _document.TryGetLocalName(_globalFunctionIndex.Value, i)
			: null;

		name ??= $"arg_{i}";

		_locals.Add((name, type, true, new LocalReference(i, true)));
		ParamCount++;
	}

	private void AddLocal(WebAssemblyValueType type, int i)
	{
		string? name = _globalFunctionIndex.HasValue
			? _document.TryGetLocalName(_globalFunctionIndex.Value, i + ParamCount)
			: null;

		name ??= $"var_{i}";

		_locals.Add((name, type, false, new LocalReference(i, false)));
	}

	public (string name, WebAssemblyValueType type, GlobalReference reference) GetGlobal(int index)
	{
		if (_globals.TryGetValue(index, out var ret))
			return ret;

		return _globals[index] = (
			_document.GetGlobalName(index),
			_document.GetGlobalType(index),
			new GlobalReference(index)
		);
	}
}
