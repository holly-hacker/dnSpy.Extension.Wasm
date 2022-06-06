namespace HoLLy.Decompiler.Core.Analysis.AST;

public class WhileNode : IHighLevelControlFlowNode
{
	public IHighLevelControlFlowNode Head { get; }
	public IHighLevelControlFlowNode LoopBody { get; }
	public bool LoopCondition { get; }

	public WhileNode(IHighLevelControlFlowNode head, IHighLevelControlFlowNode loopBody, bool loopCondition)
	{
		Head = head;
		LoopBody = loopBody;
		LoopCondition = loopCondition;
	}

	public override string ToString() => $"{Head} while ({LoopCondition}) {{{LoopBody}}}";
}
