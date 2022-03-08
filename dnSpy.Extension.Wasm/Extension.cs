using System.Collections.Generic;
using dnSpy.Contracts.Extension;

namespace dnSpy.Extension.Wasm;

public class Extension : IExtension
{
	public ExtensionInfo ExtensionInfo => new()
	{
		ShortDescription = "Provides WebAssembly (WASM) support",
		Copyright = "Copyright (C) HoLLy",
	};

	public IEnumerable<string> MergedResourceDictionaries
	{
		get { yield break; }
	}

	public void OnEvent(ExtensionEvent @event, object? obj)
	{
	}
}
