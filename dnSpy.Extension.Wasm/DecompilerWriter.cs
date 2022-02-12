using System.Globalization;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using WebAssembly;

namespace dnSpy.Extension.Wasm;

internal class DecompilerWriter
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

	public DecompilerWriter Text(string text) => Write(text, BoxedTextColor.Text);
	public DecompilerWriter Space() => Text(" ");

	public DecompilerWriter Punctuation(string text) => Write(text, BoxedTextColor.Punctuation);

	public DecompilerWriter Number(int number) => Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);
	public DecompilerWriter Number(long number) => Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);
	public DecompilerWriter Number(float number) => Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);
	public DecompilerWriter Number(double number) => Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);

	public DecompilerWriter Local(string text) => Write(text, BoxedTextColor.Local);

	public DecompilerWriter Keyword(string text) => Write(text, BoxedTextColor.Keyword);

	public DecompilerWriter OpCode(OpCode code) => Write(code.ToInstruction(), BoxedTextColor.AsmMnemonic);

	private DecompilerWriter Write(string text, object color, object? reference = null, DecompilerReferenceFlags flags = DecompilerReferenceFlags.None)
	{
		_output.Write(text, reference, flags, color);
		return this;
	}
}
