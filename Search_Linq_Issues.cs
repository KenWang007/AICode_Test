public override void VisitInvocationExpression(InvocationExpressionSyntax node)
{
    if (node.Expression is MemberAccessExpressionSyntax memberAccessExpression)
    {
        if (memberAccessExpression.Expression is InvocationExpressionSyntax invocationExpression)
        {
            AnalyzeInvocationExpression(invocationExpression);
        }
        else if (memberAccessExpression.Expression is IdentifierNameSyntax identifierName)
        {
            if (IsLinqMethod(identifierName.Identifier.Text))
            {
                AnalyzeQuery(node);
            }
        }
        else if (memberAccessExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression2)
        {
            AnalyzeMemberAccessExpression(memberAccessExpression2);
        }
    }
    else if (node.Expression is IdentifierNameSyntax identifierName)
    {
        if (IsLinqMethod(identifierName.Identifier.Text))
        {
            AnalyzeQuery(node);
        }
    }

    base.VisitInvocationExpression(node);
}

private void AnalyzeInvocationExpression(InvocationExpressionSyntax invocationExpression)
{
    if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression)
    {
        AnalyzeMemberAccessExpression(memberAccessExpression);
    }
    else if (invocationExpression.Expression is IdentifierNameSyntax identifierName)
    {
        if (IsLinqMethod(identifierName.Identifier.Text))
        {
            AnalyzeQuery(invocationExpression);
        }
    }
}

private void AnalyzeMemberAccessExpression(MemberAccessExpressionSyntax memberAccessExpression)
{
    if (memberAccessExpression.Name is IdentifierNameSyntax identifierName && IsLinqMethod(identifierName.Identifier.Text))
    {
        AnalyzeQuery(memberAccessExpression);
    }
}

private bool IsLinqMethod(string methodName)
{
    return methodName == "Where" || methodName == "Select" || methodName == "OrderBy" || methodName == "Join";
}


private void AnalyzeQuery(InvocationExpressionSyntax node)
{
    var semanticModel = _compilation.GetSemanticModel(node.SyntaxTree);
    var symbolInfo = semanticModel.GetSymbolInfo(node);

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
    {
        var declaringType = methodSymbol.ContainingType;

        if (declaringType != null && declaringType.ToString().StartsWith("System.Linq.Queryable"))
        {
            var argument = node.ArgumentList.Arguments.FirstOrDefault();
            if (argument is LambdaExpressionSyntax lambdaExpression)
            {
                var lambdaReturnType = semanticModel.GetTypeInfo(lambdaExpression.Body).ConvertedType;

                if (lambdaReturnType != null && !CanBeEvaluatedOnClient(lambdaReturnType))
                {
                    Console.WriteLine($"LINQ query cannot be evaluated on client-side. Expression: {node}");
                }
            }
        }
    }
}

