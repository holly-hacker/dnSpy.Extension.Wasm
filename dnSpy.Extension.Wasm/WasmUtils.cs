using System;
using WebAssembly;

namespace dnSpy.Extension.Wasm;

public static class WasmUtils
{
	public static string ToWasmType(this WebAssemblyValueType type) => type switch
	{
		WebAssemblyValueType.Int32 => "i32",
		WebAssemblyValueType.Int64 => "i64",
		WebAssemblyValueType.Float32 => "f32",
		WebAssemblyValueType.Float64 => "f64",
		_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
	};
}
