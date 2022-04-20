using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Echo.ControlFlow;
using Echo.Core.Graphing;

namespace HoLLy.Decompiler.Core.Analysis.AST.Graph;

public class AstGraph : IGraph
{
	private readonly IList<AstGraphNode> _nodes = new List<AstGraphNode>();
	private readonly IList<AstGraphEdge> _edges = new List<AstGraphEdge>();

	IEnumerable<INode> ISubGraph.GetNodes() => _nodes;
	IEnumerable<IEdge> IGraph.GetEdges() => _edges;

	public IEnumerable<AstGraphNode> GetNodes() => _nodes;
	public IEnumerable<AstGraphEdge> GetEdges() => _edges;

	public IEnumerable<ISubGraph> GetSubGraphs() => Enumerable.Empty<ISubGraph>();

	public AstGraphNode AddNode(IHighLevelControlFlowNode node)
	{
		var newNode = new AstGraphNode(this, node);
		AddNode(newNode);
		return newNode;
	}

	public void AddNode(AstGraphNode node)
	{
		_nodes.Add(node);
	}

	public AstGraphEdge AddEdge(AstGraphNode node1, AstGraphNode node2, ControlFlowEdgeType edgeType)
	{
		var edge = new AstGraphEdge(node1, node2, edgeType);
		_edges.Add(edge);
		return edge;
	}

	public void ReplaceNode(IList<AstGraphNode> originalNodes, AstGraphNode newNode, AstGraphNode? nextNode)
	{
		AddNode(newNode);

		for (var i = _edges.Count - 1; i >= 0; i--)
		{
			var edge = _edges[i];

			var originInternal = originalNodes.Contains(edge.Origin);
			var targetInternal = originalNodes.Contains(edge.Target);

			if (originInternal && targetInternal)
			{
				// remove internal edges
				_edges.Remove(edge);
			}
			else if (originInternal)
			{
				if (edge.Target == nextNode)
				{
					// we will replace this later
					_edges.Remove(edge);
				}

				// if target is not the next node, it's probably a goto.
			}
			else if (targetInternal)
			{
				// target should be the head of the new node
				Debug.Assert(edge.Target.ControlFlowNode == newNode.ControlFlowNode.Head);
				_edges.Remove(edge);
				AddEdge(edge.Origin, newNode, edge.EdgeType);
			}
		}

		foreach (var astGraphNode in originalNodes)
		{
			_nodes.Remove(astGraphNode);
		}

		if (nextNode != null)
			AddEdge(newNode, nextNode, ControlFlowEdgeType.FallThrough);
	}
}
