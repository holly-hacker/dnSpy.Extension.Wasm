namespace HoLLy.Decompiler.Core.Analysis.AST;

public class DoWhileNode : IHighLevelControlFlowNode
{
	public IHighLevelControlFlowNode Head { get; }
	public bool LoopCondition { get; }

	public DoWhileNode(IHighLevelControlFlowNode head, bool loopCondition)
	{
		Head = head;
		LoopCondition = loopCondition;
	}

	public override string ToString() => $"{Head} do {{{Head}}} while ({LoopCondition});";
}
