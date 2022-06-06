using System.Collections.Generic;
using System.Linq;
using Echo.Core.Graphing;

namespace HoLLy.Decompiler.Core.Analysis.AST.Graph;

public class AstGraphNode : INode
{
	private readonly AstGraph _parent;

	public AstGraphNode(AstGraph parent, IHighLevelControlFlowNode controlFlowNode)
	{
		_parent = parent;
		ControlFlowNode = controlFlowNode;
	}

	public IHighLevelControlFlowNode ControlFlowNode { get; }

	public int InDegree => GetIncomingEdges().Count();
	public int OutDegree => GetOutgoingEdges().Count();

	public IEnumerable<IEdge> GetIncomingEdges() => _parent.GetEdges().Where(e => e.Target == this);
	public IEnumerable<IEdge> GetOutgoingEdges() => _parent.GetEdges().Where(e => e.Origin == this);

	public IEnumerable<INode> GetPredecessors() => GetIncomingEdges().Select(n => n.Origin).Distinct();
	public IEnumerable<INode> GetSuccessors() => GetOutgoingEdges().Select(n => n.Target).Distinct();

	public bool HasPredecessor(INode node) => GetPredecessors().Any();
	public bool HasSuccessor(INode node) => GetSuccessors().Any();

	public override string? ToString() => ControlFlowNode.ToString();
}
