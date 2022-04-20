using System.Collections.Generic;
using System.Linq;

namespace HoLLy.Decompiler.Core.Analysis.AST;

public class SequenceNode : IHighLevelControlFlowNode
{
	private readonly List<IHighLevelControlFlowNode> _list;

	public SequenceNode(List<IHighLevelControlFlowNode> list)
	{
		_list = list;
	}

	public IHighLevelControlFlowNode Head => _list.First();
	public IReadOnlyList<IHighLevelControlFlowNode> Nodes => _list;

	public override string ToString()
	{
		return string.Join(" ", _list.Select(l => l.ToString()));
	}
}
