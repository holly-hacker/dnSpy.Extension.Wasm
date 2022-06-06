using System.Collections.Generic;
using System.Linq;
using Echo.ControlFlow;
using HoLLy.Decompiler.Core.Analysis.AST.Graph;

namespace HoLLy.Decompiler.Core.Analysis.AST;

internal interface IRegionMatcher
{
	(IHighLevelControlFlowNode flowNode, IList<AstGraphNode> oldNodes, AstGraphNode? nextNode)? TryMatch(AstGraph graph, AstGraphNode node);
}

internal class SequenceNodeMatcher : IRegionMatcher
{
	public (IHighLevelControlFlowNode flowNode, IList<AstGraphNode> oldNodes, AstGraphNode? nextNode)? TryMatch(AstGraph graph,
		AstGraphNode node)
	{
		if (node.OutDegree != 1)
			return null;

		var list = new List<AstGraphNode>();

		var currentNode = node;
		do
		{
			list.Add(currentNode);

			currentNode = (AstGraphNode?)currentNode.GetSuccessors().SingleOrDefault();
			// only check that second node onwards has in degree of 1
		} while (currentNode?.OutDegree is 1 or 0 && currentNode.InDegree == 1);

		return list.Count > 1
			? (new SequenceNode(list.Select(l => l.ControlFlowNode).ToList()), list, currentNode)
			: null;
	}
}

internal class IfThenNodeMatcher : IRegionMatcher
{
	public (IHighLevelControlFlowNode flowNode, IList<AstGraphNode> oldNodes, AstGraphNode? nextNode)? TryMatch(AstGraph graph,
		AstGraphNode node)
	{
		if (node.OutDegree != 2)
			return null;

		var successors = node.GetOutgoingEdges().Cast<AstGraphEdge>().ToArray();

		var onTrueEdge = successors.FirstOrDefault(e => e.EdgeType == ControlFlowEdgeType.Conditional);
		var onFalseEdge = successors.FirstOrDefault(e => e.EdgeType == ControlFlowEdgeType.FallThrough);

		if (onTrueEdge is null || onFalseEdge is null)
			return null;

		var onTrueNode = onTrueEdge.Target;
		var onFalseNode = onFalseEdge.Target;

		int trueOutDegree = onTrueNode.OutDegree;
		int falseOutDegree = onFalseNode.OutDegree;

		if (trueOutDegree > 1 && falseOutDegree > 1)
			return null;

		if (onTrueNode.InDegree != 1 && onFalseNode.InDegree != 1)
			return null;

		var onTrueNext = onTrueNode.GetSuccessors().Cast<AstGraphNode>().FirstOrDefault();
		var onFalseNext = onFalseNode.GetSuccessors().Cast<AstGraphNode>().FirstOrDefault();

		if (onTrueNext is null && onFalseNext is null)
		{
			// end of the function

			var newNode = new IfThenElseNode(
				node.ControlFlowNode,
				onTrueNode.ControlFlowNode,
				onFalseNode.ControlFlowNode);

			return (newNode, new[] { node, onTrueNode, onFalseNode }, null);
		}

		if (onTrueNext != null && onTrueNext == onFalseNext && onTrueNext.InDegree >= 2 &&
		    trueOutDegree == 1 && falseOutDegree == 1)
		{
			// the successor of the if-true-then and if-false-then blocks are the same

			var newNode = new IfThenElseNode(
				node.ControlFlowNode,
				onTrueNode.ControlFlowNode,
				onFalseNode.ControlFlowNode);

			return (newNode, new[] { node, onTrueNode, onFalseNode }, onTrueNext);
		}

		if (onTrueNext == onFalseNode && onTrueNext.InDegree >= 2 && trueOutDegree == 1)
		{
			// the successor of if-true-then is if-false-then, meaning there is only a if-true-then block

			var newNode = new IfThenNode(node.ControlFlowNode, onTrueNode.ControlFlowNode, true);
			return (newNode, new[] { node, onTrueNode }, onFalseNode);
		}

		if (onFalseNext == onTrueNode && onFalseNext.InDegree >= 2 && falseOutDegree == 1)
		{
			// the successor of if-false-then is if-true-then, meaning there is only a if-false-then block

			var newNode = new IfThenNode(node.ControlFlowNode, onFalseNode.ControlFlowNode, false);
			return (newNode, new[] { node, onFalseNode }, onTrueNode);
		}

		return null;
	}
}

internal class WhileNodeMatcher : IRegionMatcher
{
	public (IHighLevelControlFlowNode flowNode, IList<AstGraphNode> oldNodes, AstGraphNode? nextNode)? TryMatch(AstGraph graph, AstGraphNode node)
	{
		if (node.OutDegree != 2)
			return null;

		// PERF: this should really just write to 2 vars directly
		var outEdges = node.GetOutgoingEdges().Cast<AstGraphEdge>().ToArray();

		var targetFallthrough = outEdges.FirstOrDefault(e => e.EdgeType == ControlFlowEdgeType.FallThrough)?.Target;
		var targetCondition = outEdges.FirstOrDefault(e => e.EdgeType == ControlFlowEdgeType.Conditional)?.Target;

		if (targetFallthrough == null || targetCondition == null)
			return null;

		if (targetFallthrough == targetCondition)
			return null;

		if (targetCondition.OutDegree == 1 && targetCondition.InDegree == 1 && targetCondition.GetSuccessors().Single() == node)
			return (new WhileNode(node.ControlFlowNode, targetCondition.ControlFlowNode, true), new[] { node, targetCondition }, targetFallthrough);

		if (targetFallthrough.OutDegree == 1 && targetFallthrough.InDegree == 1 && targetFallthrough.GetSuccessors().Single() == node)
			return (new WhileNode(node.ControlFlowNode, targetFallthrough.ControlFlowNode, false), new[] { node, targetFallthrough }, targetCondition);

		return null;
	}
}

internal class DoWhileNodeMatcher : IRegionMatcher
{
	public (IHighLevelControlFlowNode flowNode, IList<AstGraphNode> oldNodes, AstGraphNode? nextNode)? TryMatch(AstGraph graph, AstGraphNode node)
	{
		if (node.OutDegree != 2)
			return null;

		// PERF: this should really just write to 2 vars directly
		var outEdges = node.GetOutgoingEdges().Cast<AstGraphEdge>().ToArray();

		var targetFallthrough = outEdges.FirstOrDefault(e => e.EdgeType == ControlFlowEdgeType.FallThrough)?.Target;
		var targetCondition = outEdges.FirstOrDefault(e => e.EdgeType == ControlFlowEdgeType.Conditional)?.Target;

		if (targetFallthrough == targetCondition)
			return null;

		if (node == targetCondition)
			return (new DoWhileNode(node.ControlFlowNode, true), new[] { node }, targetFallthrough);

		if (node == targetFallthrough)
			return (new DoWhileNode(node.ControlFlowNode, true), new[] { node }, targetCondition);

		return null;
	}
}
