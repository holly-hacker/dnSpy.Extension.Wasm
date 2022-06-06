namespace HoLLy.Decompiler.Core.Analysis.AST;

public class IfThenNode : IHighLevelControlFlowNode
{
	public IHighLevelControlFlowNode Head { get; }
	public IHighLevelControlFlowNode OnCondition { get; }

	/// <summary>
	/// Whether the condition is required to be <c>true</c> or <c>false</c> for <see cref="OnCondition"/> to be
	/// executed.
	/// </summary>
	public bool LoopCondition { get; }

	public IfThenNode(IHighLevelControlFlowNode head, IHighLevelControlFlowNode onCondition, bool loopCondition)
	{
		Head = head;
		OnCondition = onCondition;
		LoopCondition = loopCondition;
	}

	public override string ToString() => Head + " if (" + LoopCondition + ") {" + OnCondition + "}";
}
