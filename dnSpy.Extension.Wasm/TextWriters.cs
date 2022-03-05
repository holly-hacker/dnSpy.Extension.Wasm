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

	public override ArbitraryTextWriter WriteInternal(string text, object color, object? reference = null, DecompilerReferenceFlags flags = DecompilerReferenceFlags.None)
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

	public override ArbitraryTextWriter WriteInternal(string text, object color, object? reference = null,
		DecompilerReferenceFlags flags = DecompilerReferenceFlags.None)
	{
		_output.Write(color, text);
		return this;
	}
}

internal abstract class ArbitraryTextWriter
{
	public abstract ArbitraryTextWriter WriteInternal(string text, object color, object? reference = null,
		DecompilerReferenceFlags flags = DecompilerReferenceFlags.None);
}

internal static class TextWriterExtensions
{
	private static T Write<T>(this T writer, string text, object color, object? reference = null,
		DecompilerReferenceFlags flags = DecompilerReferenceFlags.None) where T : ArbitraryTextWriter
	{
		writer.WriteInternal(text, color, reference, flags);
		return writer;
	}

	public static T Text<T>(this T writer, string text) where T : ArbitraryTextWriter => writer.Write(text, BoxedTextColor.Text);

	public static T Space<T>(this T writer) where T : ArbitraryTextWriter => writer.Text(" ");

	public static T Punctuation<T>(this T writer, string text) where T : ArbitraryTextWriter => writer.Write(text, BoxedTextColor.Punctuation);

	public static T Number<T>(this T writer, int number) where T : ArbitraryTextWriter => writer.Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);
	public static T Number<T>(this T writer, long number) where T : ArbitraryTextWriter => writer.Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);
	public static T Number<T>(this T writer, float number) where T : ArbitraryTextWriter => writer.Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);
	public static T Number<T>(this T writer, double number) where T : ArbitraryTextWriter => writer.Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);

	public static T Local<T>(this T writer, string text) where T : ArbitraryTextWriter => writer.Write(text, BoxedTextColor.Local);

	public static T Keyword<T>(this T writer, string text) where T : ArbitraryTextWriter => writer.Write(text, BoxedTextColor.Keyword);

	public static T OpCode<T>(this T writer, OpCode code) where T : ArbitraryTextWriter => writer.Write(code.ToInstruction(), BoxedTextColor.AsmMnemonic);

	public static T FunctionName<T>(this T writer, string name, int? globalIndex = null, bool isDefinition = false)
		where T : ArbitraryTextWriter
	{
		if (globalIndex.HasValue)
		{
			var reference = new FunctionReference(globalIndex.Value);
			var flags = isDefinition ? DecompilerReferenceFlags.Definition : DecompilerReferenceFlags.None;
			writer.Write(name, BoxedTextColor.StaticMethod, reference, flags);
		}
		else
		{
			writer.Write(name, BoxedTextColor.StaticMethod);
		}

		return writer;
	}

	public static T FunctionDeclaration<T>(this T writer, string name, WebAssemblyType type, int? globalFunctionIndex = null, bool isDefinition = false)
		where T : ArbitraryTextWriter
	{
		writer.Keyword("fn").Space().FunctionName(name, globalFunctionIndex, isDefinition).Punctuation("(");

		bool firstParameter = true;
		foreach (var parameter in type.Parameters)
		{
			if (!firstParameter)
				writer.Punctuation(", ");
			firstParameter = false;

			writer.Keyword(parameter.ToWasmType());
		}

		writer.Punctuation(")");

		if (type.Returns.Any())
		{
			writer.Punctuation(": ");

			if (type.Returns.Count > 1) writer.Punctuation("(");

			bool firstReturnParameter = true;
			foreach (var returnParameter in type.Returns)
			{
				if (!firstReturnParameter)
					writer.Punctuation(", ");
				firstReturnParameter = false;

				writer.Keyword(returnParameter.ToWasmType());
			}

			if (type.Returns.Count > 1) writer.Punctuation(")");
		}

		return writer;
	}
}
