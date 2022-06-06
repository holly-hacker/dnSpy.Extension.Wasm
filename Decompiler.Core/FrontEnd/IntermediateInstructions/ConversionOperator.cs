namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

public class ConversionOperator : IntermediateInstruction
{
	public ConversionOperator(DataType input, DataType output, ConversionType type)
	{
		Input = input;
		Output = output;
		Type = type;
	}

	public DataType Input { get; }
	public DataType Output { get; }
	public ConversionType Type { get; }

	public override int GetStackPushCount() => 1;
	public override int GetStackPopCount() => 1;
}

public enum ConversionType
{
	/// <summary>
	/// Convert from 1 datatype to another using the default (logical) method. For example, converting i64 to i32 will
	/// truncate the top bits and casting f32 to i32 will drop the decimals. When relevant, it assumes i32 and i64 are
	/// signed.
	/// </summary>
	Convert,
	/// <summary>
	/// Similar to <see cref="Convert"/> but will assume that i32 and i64 are signed.
	/// </summary>
	ConvertSigned,
	/// <summary>
	/// Reinterpret the bits of a value as another datatype.
	/// </summary>
	Reinterpret,
}
