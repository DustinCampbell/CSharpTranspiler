﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpTranspiler.Agnostic.Types
{
	public class StructType : LogicalType
	{
		public StructType(StructDeclarationSyntax declaration, SemanticModel semanticModel)
		: base(declaration, semanticModel)
		{

		}
	}
}
