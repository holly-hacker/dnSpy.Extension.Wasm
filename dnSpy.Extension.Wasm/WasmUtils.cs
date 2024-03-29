using System;
using System.Collections.Generic;
using System.IO;
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
			.GetCustomAttribute<OpCodeCharacteristicsAttribute>()!
			.Name;
	}

	public static string GetFullName(this Import import) => $"{import.Module}::{import.Field}";

	public static uint ReadULEB128(this BinaryReader br)
	{
		uint val = 0;
		int shift = 0;

		while (true)
		{
			byte b = br.ReadByte();

			val |= (uint)((b & 0x7f) << shift);
			if ((b & 0x80) == 0) break;
			shift += 7;
		}

		return val;
	}

	public static void WriteULEB128(this BinaryWriter bw, uint val)
	{
		do
		{
			var toWrite = (byte)(val & 0x7F);
			val >>= 7;

			if (val > 0)
				toWrite |= 0x80;

			bw.Write(toWrite);
		} while (val > 0);
	}

	public static IList<byte> ToULEB128(this uint val)
	{
		var bytes = new List<byte>(5);
		do
		{
			var toWrite = (byte)(val & 0x7F);
			val >>= 7;

			if (val > 0)
				toWrite |= 0x80;

			bytes.Add(toWrite);
		} while (val > 0);

		return bytes;
	}
}
