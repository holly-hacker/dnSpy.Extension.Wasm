namespace HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

public class CallFunction : IntermediateInstruction
{
	public CallFunction(DataType[] inputParameters, DataType[] returnParameters)
	{
		InputParameters = inputParameters;
		ReturnParameters = returnParameters;
	}

	// TODO: some function descriptor that can provide name and reference/tag object

	public DataType[] InputParameters { get; }
	public DataType[] ReturnParameters { get; }

	public override int GetStackPushCount() => ReturnParameters.Length;
	public override int GetStackPopCount() => InputParameters.Length;
}
