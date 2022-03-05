using System.Globalization;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using dnSpy.Extension.Wasm.Decompilers.References;
using WebAssembly;

namespace dnSpy.Extension.Wasm;

internal class DecompilerWriter : ArbitraryTextWriter
{
	private readonly IDecompilerOutput _output;

	public DecompilerWriter(IDecompilerOutput output)
	{
		_output = output;
	}

	public DecompilerWriter Indent()
	{
		_output.IncreaseIndent();
		return this;
	}

	public DecompilerWriter DeIndent()
	{
		_output.DecreaseIndent();
		return this;
	}

	public DecompilerWriter EndLine()
	{
		_output.WriteLine();
		return this;
	}

	protected override ArbitraryTextWriter Write(string text, object color, object? reference = null, DecompilerReferenceFlags flags = DecompilerReferenceFlags.None)
	{
		_output.Write(text, reference, flags, color);
		return this;
	}
}

internal class TextColorWriter : ArbitraryTextWriter
{
	private readonly ITextColorWriter _output;

	public TextColorWriter(ITextColorWriter output)
	{
		_output = output;
	}

	protected override ArbitraryTextWriter Write(string text, object color, object? reference = null,
		DecompilerReferenceFlags flags = DecompilerReferenceFlags.None)
	{
		_output.Write(color, text);
		return this;
	}
}

internal abstract class ArbitraryTextWriter
{
	protected abstract ArbitraryTextWriter Write(string text, object color, object? reference = null,
		DecompilerReferenceFlags flags = DecompilerReferenceFlags.None);

	public ArbitraryTextWriter Text(string text) => Write(text, BoxedTextColor.Text);
	public ArbitraryTextWriter Space() => Text(" ");

	public ArbitraryTextWriter Punctuation(string text) => Write(text, BoxedTextColor.Punctuation);

	public ArbitraryTextWriter Number(int number) => Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);
	public ArbitraryTextWriter Number(long number) => Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);
	public ArbitraryTextWriter Number(float number) => Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);
	public ArbitraryTextWriter Number(double number) => Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);

	public ArbitraryTextWriter Local(string text) => Write(text, BoxedTextColor.Local);

	public ArbitraryTextWriter Keyword(string text) => Write(text, BoxedTextColor.Keyword);

	public ArbitraryTextWriter OpCode(OpCode code) => Write(code.ToInstruction(), BoxedTextColor.AsmMnemonic);

	public ArbitraryTextWriter FunctionName(string name, int? globalIndex = null, bool isDefinition = false)
	{
		if (globalIndex.HasValue)
		{
			var reference = new FunctionReference(globalIndex.Value);
			var flags = isDefinition ? DecompilerReferenceFlags.Definition : DecompilerReferenceFlags.None;
			Write(name, BoxedTextColor.StaticMethod, reference, flags);
		}
		else
		{
			Write(name, BoxedTextColor.StaticMethod);
		}

		return this;
	}

	public ArbitraryTextWriter FunctionDeclaration(string name, WebAssemblyType type, int? globalFunctionIndex = null, bool isDefinition = false)
	{
		Keyword("fn").Space().FunctionName(name, globalFunctionIndex, isDefinition).Punctuation("(");

		bool firstParameter = true;
		foreach (var parameter in type.Parameters)
		{
			if (!firstParameter)
				Punctuation(", ");
			firstParameter = false;

			Keyword(parameter.ToWasmType());
		}

		Punctuation(")");

		if (type.Returns.Any())
		{
			Punctuation(": ");

			if (type.Returns.Count > 1) Punctuation("(");

			bool firstReturnParameter = true;
			foreach (var returnParameter in type.Returns)
			{
				if (!firstReturnParameter)
					Punctuation(", ");
				firstReturnParameter = false;

				Keyword(returnParameter.ToWasmType());
			}

			if (type.Returns.Count > 1) Punctuation(")");
		}

		return this;
	}
}
