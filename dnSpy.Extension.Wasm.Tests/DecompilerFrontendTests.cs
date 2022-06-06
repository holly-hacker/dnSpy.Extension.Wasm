using System;
using dnSpy.Extension.Wasm.Decompiling;
using FluentAssertions;
using WebAssembly;
using WebAssembly.Instructions;
using Xunit;

namespace dnSpy.Extension.Wasm.Tests;

public class DecompilerFrontendTests
{
	public static Instruction[][] EmptyFunctionBodies =
	{
		new Instruction[] { new End() },
		new Instruction[] { new NoOperation(), new End() },
		new Instruction[] { new NoOperation(), new NoOperation(), new End() },
	};

	[Theory]
	[MemberData(nameof(EmptyFunctionBodies))]
	public void LiftEmptyFunction(params Instruction[] instruction)
	{
		var frontend = new WasmDecompilerFrontend(
			null!, // TODO: use some interface?
			ArraySegment<Local>.Empty,
			instruction,
			new WebAssemblyType());

		var output = frontend.Convert();
		output.Should().ContainSingle();
	}
}
