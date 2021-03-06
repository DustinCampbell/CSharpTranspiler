﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpTranspiler.Agnostic.Syntax.Expressions;
using Microsoft.CodeAnalysis;

namespace CSharpTranspiler.Agnostic.Syntax.Statements
{
	public class ReturnStatement : Statement
	{
		public Expression expression;

		public ReturnStatement(ReturnStatementSyntax statement, SemanticModel semanticModel)
		{
			expression = Expression.CreateExpression(null, statement.Expression, semanticModel);
		}
	}
}
