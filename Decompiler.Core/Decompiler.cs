using System;
using HoLLy.Decompiler.Core.Analysis;
using HoLLy.Decompiler.Core.FrontEnd;

namespace HoLLy.Decompiler.Core;

public class Decompiler
{
	private readonly IDecompilerFrontend _frontend;

	public Decompiler(IDecompilerFrontend frontend)
	{
		_frontend = frontend;
	}

	public void Decompile()
	{
		var ir = _frontend.Convert();

		var astGen = new AstGenerator(ir);
		astGen.CreateCfgDotString();

		throw new NotImplementedException("Full decompiler chain");
	}
}
