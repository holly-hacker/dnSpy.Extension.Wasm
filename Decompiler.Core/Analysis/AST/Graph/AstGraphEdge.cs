using Echo.ControlFlow;
using Echo.Core.Graphing;

namespace HoLLy.Decompiler.Core.Analysis.AST.Graph;

public class AstGraphEdge : IEdge
{

	public AstGraphEdge(AstGraphNode origin, AstGraphNode target, ControlFlowEdgeType edgeType)
	{
		Origin = origin;
		Target = target;
		EdgeType = edgeType;
	}

	INode IEdge.Origin => Origin;
	INode IEdge.Target => Target;

	public AstGraphNode Origin { get; }
	public AstGraphNode Target { get; }
	public ControlFlowEdgeType EdgeType { get; }
}
