using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace dnSpy.Extension.Wasm;

/// <summary>
/// A custom section as defined by the spec: https://webassembly.github.io/spec/core/appendix/custom.html#name-section
/// <br/>
/// It contains symbol names for functions, modules and locals.
/// </summary>
internal class NameSection
{
	public string? ModuleName { get; set; }

	/// <summary> Maps function indices in function space (ie. including imports) to names. </summary>
	public IReadOnlyDictionary<int, string>? FunctionNames => _functionNames;

	/// <summary>
	/// Maps function indices in function space (ie. including imports) to a map of local indices to local names.
	/// </summary>
	public IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>? LocalNames => _localNames;

	private Dictionary<int, string>? _functionNames;
	private Dictionary<int, IReadOnlyDictionary<int, string>>? _localNames;

	public static NameSection Read(byte[] data)
	{
		using var ms = new MemoryStream(data);
		using var br = new BinaryReader(ms, Encoding.UTF8);

		var section = new NameSection();

		// subsections occur in order of increasing index if present

		if (br.PeekChar() == (byte)Subsection.ModuleName)
		{
			br.ReadByte();

			var sectionLength = br.ReadULEB128();
			var sectionStartIndex = br.BaseStream.Position;

			section.ModuleName = br.ReadString();

			if (sectionStartIndex + sectionLength != br.BaseStream.Position)
			{
				throw new Exception($"Module section length mismatch: {sectionLength} expected, {br.BaseStream.Position = sectionStartIndex} read");
			}
		}

		if (br.PeekChar() == (byte)Subsection.FunctionNames)
		{
			br.ReadByte();

			var sectionLength = br.ReadULEB128();
			var sectionStartIndex = br.BaseStream.Position;

			var nameCount = br.ReadULEB128();
			var dic =  new Dictionary<int, string>((int)nameCount);

			for (var i = 0; i < nameCount; i++)
			{
				var functionIndex = (int)br.ReadULEB128();
				dic[functionIndex] = br.ReadString();
			}

			if (sectionStartIndex + sectionLength != br.BaseStream.Position)
			{
				throw new Exception($"Module section length mismatch: {sectionLength} expected, {br.BaseStream.Position = sectionStartIndex} read");
			}

			section._functionNames = dic;
		}

		if (br.PeekChar() == (byte)Subsection.LocalNames)
		{
			br.ReadByte();

			var sectionLength = br.ReadULEB128();
			var sectionStartIndex = br.BaseStream.Position;

			var functionCount = br.ReadULEB128();
			var functions = new Dictionary<int, IReadOnlyDictionary<int, string>>();

			for (var i = 0; i < functionCount; i++)
			{
				var functionIndex = (int)br.ReadULEB128();
				var localCount = (int)br.ReadULEB128();

				var locals = new Dictionary<int, string>();

				for (var j = 0; j < localCount; j++)
				{
					var localIndex = (int)br.ReadULEB128();
					var localName = br.ReadString();
					locals[localIndex] = localName;
				}

				functions[functionIndex] = locals;
			}

			if (sectionStartIndex + sectionLength != br.BaseStream.Position)
			{
				throw new Exception($"Module section length mismatch: {sectionLength} expected, {br.BaseStream.Position = sectionStartIndex} read");
			}

			section._localNames = functions;
		}

		return section;
	}

	public IList<byte> ToList()
	{
		var bytes = new List<byte>();

		if (ModuleName != null)
		{
			bytes.Add((byte)Subsection.ModuleName);

			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);

			bw.Write(ModuleName);

			bw.Flush();
			byte[] sectionBytes = ms.ToArray();
			bytes.AddRange(((uint)sectionBytes.Length).ToULEB128());
			bytes.AddRange(sectionBytes);
		}

		if (FunctionNames != null)
		{
			bytes.Add((byte)Subsection.FunctionNames);

			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);

			bw.WriteULEB128((uint)FunctionNames.Count);

			foreach (var kvp in FunctionNames)
			{
				bw.WriteULEB128((uint)kvp.Key);
				bw.Write(kvp.Value);
			}

			bw.Flush();
			byte[] sectionBytes = ms.ToArray();
			bytes.AddRange(((uint)sectionBytes.Length).ToULEB128());
			bytes.AddRange(sectionBytes);
		}

		if (LocalNames != null)
		{
			bytes.Add((byte)Subsection.LocalNames);

			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);

			bw.WriteULEB128((uint)LocalNames.Count);

			foreach (var kvp in LocalNames)
			{
				bw.WriteULEB128((uint)kvp.Key);
				bw.WriteULEB128((uint)kvp.Value.Count);

				foreach (var kvp2 in kvp.Value)
				{
					bw.WriteULEB128((uint)kvp2.Key);
					bw.Write(kvp2.Value);
				}
			}

			bw.Flush();
			byte[] sectionBytes = ms.ToArray();
			bytes.AddRange(((uint)sectionBytes.Length).ToULEB128());
			bytes.AddRange(sectionBytes);
		}

		return bytes;
	}

	public void SetFunctionName(int functionIndex, string name)
	{
		_functionNames ??= new Dictionary<int, string>();
		_functionNames[functionIndex] = name;
	}

	public void SetLocalName(int functionIndex, int localIndex, string name)
	{
		_localNames ??= new Dictionary<int, IReadOnlyDictionary<int, string>>();

		if (!_localNames.TryGetValue(functionIndex, out var locals))
			locals = _localNames[functionIndex] = new Dictionary<int, string>();

		if (locals is not IDictionary<int, string> dic)
			throw new Exception($"{nameof(LocalNames)} should contain values of type {nameof(IDictionary<int, string>)}");

		dic[localIndex] = name;
	}

	private enum Subsection : byte
	{
		ModuleName = 0,
		FunctionNames = 1,
		LocalNames = 2,
	}
}
