using DarkLink.Generators.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            enumInfos.Foreach(enumInfo => GenerateMatcher(enumInfo, context));
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

        private void GenerateMatcher(EnumInfo enumInfo, GeneratorExecutionContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"using System;");
            sb.AppendLine($"namespace {enumInfo.EnumTypeSymbol.ContainingNamespace} {{");
            sb.AppendLine($"    internal static class {enumInfo.EnumTypeSymbol.Name}Matcher {{");

            // Generate Action
            sb.AppendLine($"        public static void Match(");
            sb.Append($"            this {enumInfo.EnumTypeSymbol} thisEnum");
            foreach (var field in enumInfo.Fields)
            {
                sb.AppendLine($",");
                sb.Append($"            Action on{field.Name}");
            }
            sb.AppendLine($") {{");
            sb.AppendLine($"            switch(thisEnum) {{");
            foreach (var field in enumInfo.Fields)
            {
                sb.AppendLine($"                case {field}:");
                sb.AppendLine($"                    on{field.Name}();");
                sb.AppendLine($"                    break;");
            }
            sb.AppendLine($"                default:");
            sb.AppendLine($"                    throw new NotSupportedException();");
            sb.AppendLine($"            }}");
            sb.AppendLine($"        }}");

            // Generate Func
            sb.AppendLine($"        public static T Match<T>(");
            sb.Append($"            this {enumInfo.EnumTypeSymbol} thisEnum");
            foreach (var field in enumInfo.Fields)
            {
                sb.AppendLine($",");
                sb.Append($"            Func<T> on{field.Name}");
            }
            sb.AppendLine($") {{");
            sb.AppendLine($"            switch(thisEnum) {{");
            foreach (var field in enumInfo.Fields)
            {
                sb.AppendLine($"                case {field}:");
                sb.AppendLine($"                    return on{field.Name}();");
            }
            sb.AppendLine($"                default:");
            sb.AppendLine($"                    throw new NotSupportedException();");
            sb.AppendLine($"            }}");
            sb.AppendLine($"        }}");

            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            var sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);
            context.AddSource($"{enumInfo.EnumTypeSymbol}.cs", sourceText);
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