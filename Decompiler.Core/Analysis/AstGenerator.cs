using System.Collections.Generic;
using System.IO;
using System.Linq;
using Echo.ControlFlow;
using Echo.ControlFlow.Construction;
using Echo.ControlFlow.Construction.Static;
using HoLLy.Decompiler.Core.Analysis.AST;
using HoLLy.Decompiler.Core.Analysis.AST.Graph;
using HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

namespace HoLLy.Decompiler.Core.Analysis;

public class AstGenerator
{
	private readonly IList<IntermediateInstruction> _instructions;

	private static readonly IRegionMatcher[] RegionMatchers =
	{
		new SequenceNodeMatcher(),
		new IfThenNodeMatcher(),
		new WhileNodeMatcher(),
		new DoWhileNodeMatcher(),
	};

	public AstGenerator(IList<IntermediateInstruction> instructions)
	{
		_instructions = instructions;
	}

	public AstGraph CreateControlFlowAst()
	{
		var cfg = CreateCfg();

		// create AST graph
		var astGraph = CreateAstGraph(cfg);
		ReduceGraph(astGraph);

		return astGraph;
	}

	public string CreateCfgDotString()
	{
		var cfg = CreateCfg();

		using var sw = new StringWriter();
		cfg.ToDotGraph(sw);
		sw.Flush();
		return sw.ToString();
	}

	private ControlFlowGraph<IntermediateInstruction> CreateCfg()
	{
		var arch = new IntermediateInstructionArchitecture(_instructions);
		var successorResolver = new IntermediateInstructionSuccessorResolver(arch);
		var builder = new StaticFlowGraphBuilder<IntermediateInstruction>(arch, successorResolver);

		var cfg = builder.ConstructFlowGraph(0L);

		return cfg;
	}

	private static AstGraph CreateAstGraph(ControlFlowGraph<IntermediateInstruction> cfg)
	{
		var dic = new Dictionary<ControlFlowNode<IntermediateInstruction>, AstGraphNode>();

		var astGraph = new AstGraph();
		foreach (var cfgNode in cfg.Nodes)
		{
			var cfgNodeContents = cfgNode.Contents.Instructions;
			var graphNode = astGraph.AddNode(new IntermediateInstructionListNode(cfgNodeContents));
			dic.Add(cfgNode, graphNode);
		}

		foreach (var cfgEdge in cfg.Nodes.SelectMany(cfgNode => cfgNode.GetIncomingEdges()))
		{
			astGraph.AddEdge(dic[cfgEdge.Origin], dic[cfgEdge.Target], cfgEdge.Type);
		}

		return astGraph;
	}

	private static void ReduceGraph(AstGraph graph)
	{
		bool anyMatcherReplaced;
		do
		{
			anyMatcherReplaced = false;
			foreach (var matcher in RegionMatchers)
			{
				bool thisMatcherReplaced;
				do
				{
					thisMatcherReplaced = false;

					foreach (var node in graph.GetNodes())
					{
						var match = matcher.TryMatch(graph, node);
						if (!match.HasValue)
							continue;

						var (flowNode, oldNodes, nextNode) = match.Value;
						graph.ReplaceNode(oldNodes, new AstGraphNode(graph, flowNode), nextNode);
						thisMatcherReplaced = true;
						anyMatcherReplaced = true;
						break;
					}
				} while (thisMatcherReplaced);
			}
		} while (anyMatcherReplaced);
	}
}
