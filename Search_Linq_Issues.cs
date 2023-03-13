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



////version two:



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

            if (argument?.Expression is LambdaExpressionSyntax lambdaExpression)
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

private bool CanBeEvaluatedOnClient(ITypeSymbol type)
{
    if (type == null)
    {
        return true;
    }

    if (type.TypeKind == TypeKind.Enum || type.SpecialType == SpecialType.System_String || type.SpecialType == SpecialType.System_Boolean ||
        type.SpecialType == SpecialType.System_Byte || type.SpecialType == SpecialType.System_Char || type.SpecialType == SpecialType.System_DateTime ||
        type.SpecialType == SpecialType.System_Decimal || type.SpecialType == SpecialType.System_Double || type.SpecialType == SpecialType.System_Int16 ||
        type.SpecialType == SpecialType.System_Int32 || type.SpecialType == SpecialType.System_Int64 || type.SpecialType == SpecialType.System_Single ||
        type.SpecialType == SpecialType.System_UInt16 || type.SpecialType == SpecialType.System_UInt32 || type.SpecialType == SpecialType.System_UInt64)
    {
        return true;
    }

    if (type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsAnonymousType)
    {
        return namedTypeSymbol.GetMembers().All(member => CanBeEvaluatedOnClient(member.GetTypeOrReturnType()));
    }

    if (type is IArrayTypeSymbol arrayTypeSymbol)
    {
        return CanBeEvaluatedOnClient(arrayTypeSymbol.ElementType);
    }

    return false;
}

