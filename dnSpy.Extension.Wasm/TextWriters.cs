using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using dnSpy.Extension.Wasm.Decompilers;
using dnSpy.Extension.Wasm.Decompilers.References;
using WebAssembly;

namespace dnSpy.Extension.Wasm;

internal class DecompilerWriter : ArbitraryTextWriter
{
	private readonly IDecompilerOutput _output;
	private readonly Stack<(TextSpan, CodeBracesRangeFlags)> _braces = new();

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

	public override ArbitraryTextWriter OpenBraceInternal(string text, CodeBracesRangeFlags flags)
	{
		// NOTE: could make flags optional and infer BraceKind from text

		int braceStart = _output.NextPosition;
		this.Punctuation(text);
		int braceEnd = _output.NextPosition;

		_braces.Push((TextSpan.FromBounds(braceStart, braceEnd), flags));
		return this;
	}

	public override ArbitraryTextWriter CloseBraceInternal(string text)
	{
		int braceStart = _output.NextPosition;
		this.Punctuation(text);
		int braceEnd = _output.NextPosition;

		var (startBraceSpan, flags) = _braces.Pop();
		var endBraceSpan = TextSpan.FromBounds(braceStart, braceEnd);
		_output.AddBracePair(startBraceSpan, endBraceSpan, flags);
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

	public override ArbitraryTextWriter OpenBraceInternal(string text, CodeBracesRangeFlags flags) => this.Punctuation(text);
	public override ArbitraryTextWriter CloseBraceInternal(string text) => this.Punctuation(text);

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

	public abstract ArbitraryTextWriter OpenBraceInternal(string text, CodeBracesRangeFlags flags);
	public abstract ArbitraryTextWriter CloseBraceInternal(string text);
}

internal static class TextWriterExtensions
{
	private static T Write<T>(this T writer, string text, object color, object? reference = null,
		DecompilerReferenceFlags flags = DecompilerReferenceFlags.None) where T : ArbitraryTextWriter
	{
		writer.WriteInternal(text, color, reference, flags);
		return writer;
	}

	public static T OpenBrace<T>(this T writer, string text, CodeBracesRangeFlags flags) where T : ArbitraryTextWriter
	{
		writer.OpenBraceInternal(text, flags);
		return writer;
	}

	public static T CloseBrace<T>(this T writer, string text) where T : ArbitraryTextWriter
	{
		writer.CloseBraceInternal(text);
		return writer;
	}

	public static T Text<T>(this T writer, string text) where T : ArbitraryTextWriter => writer.Write(text, BoxedTextColor.Text);

	public static T Space<T>(this T writer) where T : ArbitraryTextWriter => writer.Text(" ");

	public static T Punctuation<T>(this T writer, string text) where T : ArbitraryTextWriter => writer.Write(text, BoxedTextColor.Punctuation);

	public static T Number<T>(this T writer, int number) where T : ArbitraryTextWriter => writer.Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);
	public static T Number<T>(this T writer, long number) where T : ArbitraryTextWriter => writer.Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);
	public static T Number<T>(this T writer, float number) where T : ArbitraryTextWriter => writer.Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);
	public static T Number<T>(this T writer, double number) where T : ArbitraryTextWriter => writer.Write(number.ToString(CultureInfo.InvariantCulture), BoxedTextColor.Number, number);

	public static T Local<T>(this T writer, string text, LocalReference? reference, bool isDefinition) where T : ArbitraryTextWriter
	{
		var flags = DecompilerReferenceFlags.Local;
		if (isDefinition) flags |= DecompilerReferenceFlags.Definition;

		return writer.Write(text, BoxedTextColor.Local, reference, flags);
	}

	public static T Global<T>(this T writer, string text, GlobalReference? reference, bool isDefinition) where T : ArbitraryTextWriter
	{
		return writer.Write(text, BoxedTextColor.StaticField, reference,
			isDefinition ? DecompilerReferenceFlags.Definition : DecompilerReferenceFlags.None);
	}

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

	public static T FunctionDeclaration<T>(this T writer, string name, WebAssemblyType type,
		int? globalFunctionIndex = null, bool isDefinition = false, VariableInfo? vars = null)
		where T : ArbitraryTextWriter
	{
		writer.Keyword("fn").Space().FunctionName(name, globalFunctionIndex, isDefinition).OpenBrace("(", CodeBracesRangeFlags.Parentheses);

		bool firstParameter = true;
		for (var paramIdx = 0; paramIdx < type.Parameters.Count; paramIdx++)
		{
			var parameter = type.Parameters[paramIdx];
			if (!firstParameter)
				writer.Punctuation(", ");
			firstParameter = false;

			if (vars != null)
			{
				var local = vars.Locals[paramIdx];
				writer.Local(local.name, local.reference, true).Punctuation(": ");
			}

			writer.Keyword(parameter.ToWasmType());
		}

		writer.CloseBrace(")");

		if (type.Returns.Any())
		{
			writer.Punctuation(": ");

			if (type.Returns.Count > 1) writer.OpenBrace("(", CodeBracesRangeFlags.Parentheses);

			bool firstReturnParameter = true;
			foreach (var returnParameter in type.Returns)
			{
				if (!firstReturnParameter)
					writer.Punctuation(", ");
				firstReturnParameter = false;

				writer.Keyword(returnParameter.ToWasmType());
			}

			if (type.Returns.Count > 1) writer.CloseBrace(")");
		}

		return writer;
	}

	public static T FunctionSignature<T>(this T writer, WebAssemblyType type)
		where T : ArbitraryTextWriter
	{
		writer.OpenBrace("(", CodeBracesRangeFlags.Parentheses);

		bool firstParameter = true;
		for (var paramIdx = 0; paramIdx < type.Parameters.Count; paramIdx++)
		{
			var parameter = type.Parameters[paramIdx];
			if (!firstParameter)
				writer.Punctuation(", ");
			firstParameter = false;

			writer.Keyword(parameter.ToWasmType());
		}

		writer.CloseBrace(")");

		if (type.Returns.Any())
		{
			writer.Punctuation(": ");

			if (type.Returns.Count > 1) writer.OpenBrace("(", CodeBracesRangeFlags.Parentheses);

			bool firstReturnParameter = true;
			foreach (var returnParameter in type.Returns)
			{
				if (!firstReturnParameter)
					writer.Punctuation(", ");
				firstReturnParameter = false;

				writer.Keyword(returnParameter.ToWasmType());
			}

			if (type.Returns.Count > 1) writer.CloseBrace(")");
		}

		return writer;
	}
}
