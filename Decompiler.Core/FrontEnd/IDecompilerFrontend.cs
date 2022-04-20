using System.Collections.Generic;
using HoLLy.Decompiler.Core.FrontEnd.IntermediateInstructions;

namespace HoLLy.Decompiler.Core.FrontEnd;

public interface IDecompilerFrontend
{
	IList<IntermediateInstruction> Convert();
}
