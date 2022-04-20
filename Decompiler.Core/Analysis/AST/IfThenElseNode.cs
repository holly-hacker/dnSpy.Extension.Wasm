namespace HoLLy.Decompiler.Core.Analysis.AST;

public class IfThenElseNode : IHighLevelControlFlowNode
{
	public IfThenElseNode(IHighLevelControlFlowNode head, IHighLevelControlFlowNode onTrue, IHighLevelControlFlowNode onFalse)
	{
		Head = head;
		OnTrue = onTrue;
		OnFalse = onFalse;
	}

	public IHighLevelControlFlowNode Head { get; }
	public IHighLevelControlFlowNode OnTrue { get; }
	public IHighLevelControlFlowNode OnFalse { get; }

	public override string ToString() => Head + " if() {" + OnTrue + "} else {" + OnFalse + "}";
}
