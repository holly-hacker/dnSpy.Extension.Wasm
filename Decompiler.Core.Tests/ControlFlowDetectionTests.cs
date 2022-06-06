using System;
using System.Linq;
using FluentAssertions;
using HoLLy.Decompiler.Core.Analysis;
using HoLLy.Decompiler.Core.Analysis.AST;
using HoLLy.Decompiler.Core.FrontEnd;
using HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;
using Xunit;

namespace Decompiler.Core.Tests;

public class ControlFlowDetectionTests
{
	// TODO: test `load 1; jump +2; trap; end;`

	[Fact]
	public void DetectIfThen()
	{
		var instructions = new IntermediateInstruction[]
		{
			new LoadConstant(new I32Value(1)),
			new Jump(true),
			new CallFunction(Array.Empty<DataType>(), Array.Empty<DataType>()),
			new EndOfFunction(),
		};
		((Jump)instructions[1]).SetTarget(instructions[3]);

		var astGen = new AstGenerator(instructions);
		var astGraph = astGen.CreateControlFlowAst();

		var nodes = astGraph.GetNodes().ToArray();
		nodes.Should().HaveCount(1).And.ContainSingle(n => n.ControlFlowNode is SequenceNode);
		var sequenceNode = nodes.Single();
		var sequence = (SequenceNode)sequenceNode.ControlFlowNode;

		sequence.Nodes.Should().HaveCount(2);
		sequence.Nodes[0].Should().BeOfType<IfThenNode>();
		sequence.Nodes[1].Should().BeOfType<IntermediateInstructionListNode>();

		var ifThenNode = (IfThenNode)sequence.Nodes[0];

		ifThenNode.LoopCondition.Should().BeFalse("call is skipped if jump executes");
		ifThenNode.OnCondition.Should().BeOfType<IntermediateInstructionListNode>()
			.Which.Instructions.Should().ContainSingle(i => i is CallFunction);
	}

	[Fact]
	public void DetectIfThenElse()
	{
		var instructions = new IntermediateInstruction[]
		{
			new LoadConstant(new I32Value(1)),
			new Jump(true),
			new CallFunction(Array.Empty<DataType>(), Array.Empty<DataType>()),
			new Jump(false),
			new CallFunction(Array.Empty<DataType>(), Array.Empty<DataType>()),
			new EndOfFunction(),
		};
		((Jump)instructions[1]).SetTarget(instructions[4]);
		((Jump)instructions[3]).SetTarget(instructions[5]);

		var astGen = new AstGenerator(instructions);
		var astGraph = astGen.CreateControlFlowAst();

		var nodes = astGraph.GetNodes().ToArray();
		nodes.Should().HaveCount(1).And.ContainSingle(n => n.ControlFlowNode is SequenceNode);
		var sequenceNode = nodes.Single();
		var sequence = (SequenceNode)sequenceNode.ControlFlowNode;

		sequence.Nodes.Should().HaveCount(2);
		sequence.Nodes[0].Should().BeOfType<IfThenElseNode>();
		sequence.Nodes[1].Should().BeOfType<IntermediateInstructionListNode>();

		var ifThenNode = (IfThenElseNode)sequence.Nodes[0];

		ifThenNode.OnTrue.Should().BeOfType<IntermediateInstructionListNode>()
			.Which.Instructions.Should().ContainSingle(i => i is CallFunction);
		ifThenNode.OnFalse.Should().BeOfType<IntermediateInstructionListNode>()
			.Which.Instructions.Should().ContainSingle(i => i is CallFunction);

		ifThenNode.OnFalse.Head.Should().NotBe(ifThenNode.OnTrue.Head);
	}

	[Fact]
	public void DetectNestedIfThen()
	{
		var instructions = new IntermediateInstruction[]
		{
			new LoadConstant(new I32Value(0)),
			new Jump(true),
			new LoadConstant(new I32Value(0)),
			new Jump(true),
			new CallFunction(Array.Empty<DataType>(), Array.Empty<DataType>()),
			new EndOfFunction(),
		};
		((Jump)instructions[1]).SetTarget(instructions[5]);
		((Jump)instructions[3]).SetTarget(instructions[5]);

		var astGen = new AstGenerator(instructions);
		var astGraph = astGen.CreateControlFlowAst();

		var nodes = astGraph.GetNodes().ToArray();
		nodes.Should().HaveCount(1).And.ContainSingle(n => n.ControlFlowNode is SequenceNode);
		var sequenceNode = nodes.Single();
		var sequence = (SequenceNode)sequenceNode.ControlFlowNode;

		sequence.Nodes.Should().HaveCount(2);
		sequence.Nodes[0].Should().BeOfType<IfThenNode>();
		sequence.Nodes[1].Should().BeOfType<IntermediateInstructionListNode>();

		var ifThenNode1 = (IfThenNode)sequence.Nodes[0];

		ifThenNode1.OnCondition.Should().BeOfType<IfThenNode>();

		var ifThenNode2 = (IfThenNode)ifThenNode1.OnCondition;
		ifThenNode2.OnCondition.Should().BeOfType<IntermediateInstructionListNode>();
	}

	[Fact]
	public void DetectWhileLoop()
	{
		var instructions = new IntermediateInstruction[]
		{
			new LoadConstant(new I32Value(0)),
			new Jump(true),
			new CallFunction(Array.Empty<DataType>(), Array.Empty<DataType>()),
			new Jump(false),
			new EndOfFunction(),
		};
		((Jump)instructions[1]).SetTarget(instructions[4]);
		((Jump)instructions[3]).SetTarget(instructions[0]);

		var astGen = new AstGenerator(instructions);
		var astGraph = astGen.CreateControlFlowAst();

		var nodes = astGraph.GetNodes().ToArray();
		nodes.Should().HaveCount(1).And.ContainSingle(n => n.ControlFlowNode is SequenceNode);
		var sequenceNode = nodes.Single();
		var sequence = (SequenceNode)sequenceNode.ControlFlowNode;

		sequence.Nodes.Should().HaveCount(2);
		sequence.Nodes[0].Should().BeOfType<WhileNode>();
		sequence.Nodes[1].Should().BeOfType<IntermediateInstructionListNode>();

		var whileNode = (WhileNode)sequence.Nodes[0];

		whileNode.LoopBody.Should().BeOfType<IntermediateInstructionListNode>()
			.Which.Instructions.Should().ContainSingle(i => i is CallFunction);
	}

	[Fact]
	public void DetectDoWhileLoop()
	{
		var instructions = new IntermediateInstruction[]
		{
			new CallFunction(Array.Empty<DataType>(), Array.Empty<DataType>()),
			new LoadConstant(new I32Value(0)),
			new Jump(true),
			new EndOfFunction(),
		};
		((Jump)instructions[2]).SetTarget(instructions[0]);

		var astGen = new AstGenerator(instructions);
		var astGraph = astGen.CreateControlFlowAst();

		var nodes = astGraph.GetNodes().ToArray();
		nodes.Should().HaveCount(1).And.ContainSingle(n => n.ControlFlowNode is SequenceNode);
		var sequenceNode = nodes.Single();
		var sequence = (SequenceNode)sequenceNode.ControlFlowNode;

		sequence.Nodes.Should().HaveCount(2);
		sequence.Nodes[0].Should().BeOfType<DoWhileNode>();
		sequence.Nodes[1].Should().BeOfType<IntermediateInstructionListNode>();

		var whileNode = (DoWhileNode)sequence.Nodes[0];

		whileNode.Head.Should().BeOfType<IntermediateInstructionListNode>()
			.Which.Instructions.Should().ContainSingle(i => i is CallFunction);
	}
}
