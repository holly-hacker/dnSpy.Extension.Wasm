using System.Globalization;

namespace HoLLy.Decompiler.Core.FrontEnd;

public interface IConstantValue
{
	public DataType DataType { get; }
}

public class I32Value : IConstantValue
{
	public I32Value(int val)
	{
		Value = val;
	}

	public DataType DataType => DataType.I32;
	public int Value { get; set; }

	public override string ToString() => Value.ToString();
}

public class I64Value : IConstantValue
{
	public I64Value(long val)
	{
		Value = val;
	}

	public DataType DataType => DataType.I64;
	public long Value { get; set; }

	public override string ToString() => Value.ToString();
}

public class F32Value : IConstantValue
{
	public F32Value(float val)
	{
		Value = val;
	}

	public DataType DataType => DataType.F32;
	public float Value { get; set; }

	public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public class F64Value : IConstantValue
{
	public F64Value(double val)
	{
		Value = val;
	}

	public DataType DataType => DataType.F64;
	public double Value { get; set; }

	public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
