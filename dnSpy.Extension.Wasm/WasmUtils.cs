using System;
using System.Linq;
using System.Reflection;
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

	public static string ToTypeString(this BlockType blockType) => blockType switch
	{
		BlockType.Int32 => "i32",
		BlockType.Int64 => "i64",
		BlockType.Float32 => "f32",
		BlockType.Float64 => "f64",
		BlockType.Empty => "",
		_ => "?",
	};

	public static string ToInstruction(this OpCode opcode)
	{
		// may want to cache/memoize this?
		return typeof(OpCode)
			.GetMember(opcode.ToString())
			.Single()
			.GetCustomAttribute<OpCodeCharacteristicsAttribute>()
			.Name;
	}
}
