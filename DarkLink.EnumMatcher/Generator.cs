using DarkLink.Generators.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkLink.EnumMatcher
{
    [Generator]
    public class Generator : ISourceGenerator, ISyntaxReceiver
    {
        private record EnumInfo(INamedTypeSymbol EnumTypeSymbol, IReadOnlyList<IFieldSymbol> Fields, bool IsFlags);

        private readonly List<MemberAccessExpressionSyntax> matchAccessNodes = new();

        public void Execute(GeneratorExecutionContext context)
        {
            if (!matchAccessNodes.Any())
                return;

            var enumTypeSymbols = CollectEnumTypes(context.Compilation);
            var enumInfos = enumTypeSymbols.Select(MapEnumType);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            matchAccessNodes.Clear();
            context.RegisterForSyntaxNotifications(() => this);

            // Initialize generator
        }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Collect relevant syntax nodes
            if (syntaxNode is MemberAccessExpressionSyntax { Name: { Identifier: { Text: "Match" } } } memberAccessExpressionSyntax)
                matchAccessNodes.Add(memberAccessExpressionSyntax);
        }

        private IEnumerable<INamedTypeSymbol> CollectEnumTypes(Compilation compilation)
        {
            var enumTypeSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var matchAccessNode in matchAccessNodes)
            {
                var semanticModel = compilation.GetSemanticModel(matchAccessNode.SyntaxTree);

                var enumTypeSymbol = semanticModel.GetTypeInfo(matchAccessNode.Expression).Type as INamedTypeSymbol;
                enumTypeSymbols.Add(enumTypeSymbol);
            }

            return enumTypeSymbols;
        }

        private void GenerateMatcher(EnumInfo enumInfo)
        {
            throw new NotImplementedException();
        }

        private EnumInfo MapEnumType(INamedTypeSymbol enumTypeSymbol)
        {
            var fields = enumTypeSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(o => o.IsStatic && o.IsConst);
            return new EnumInfo(enumTypeSymbol, fields.ToList(), false);
        }
    }
}