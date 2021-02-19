using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DarkLink.AutoNotify
{
    [Generator]
    public class Generator : ISourceGenerator, ISyntaxReceiver
    {
        private readonly List<AttributeSyntax> attributeNodeCandidates = new();

        public void Execute(GeneratorExecutionContext context)
        {
            var attributeSource = GetAttributeSource();
            context.AddSource("Attribute.cs", attributeSource);

            if (attributeNodeCandidates.Any())
            {
                var compilation = AddAttributeCompilation(attributeSource, context.Compilation);
                var attributeSymbol = compilation.GetTypeByMetadataName("DarkLink.AutoNotify.AutoNotifyAttribute");

                var classInfoDictionary = new Dictionary<INamedTypeSymbol, ClassInfo>(SymbolEqualityComparer.Default);
                foreach (var attributeNode in attributeNodeCandidates)
                {
                    var semanticModel = compilation.GetSemanticModel(attributeNode.SyntaxTree);
                    var currentAttributeSymbol = semanticModel.GetSymbol<IMethodSymbol>(attributeNode, context.CancellationToken)?.ContainingType;

                    if (!SymbolEqualityComparer.Default.Equals(attributeSymbol, currentAttributeSymbol))
                        continue;

                    if (attributeNode is not
                        {
                            Parent:
                            {
                                Parent: FieldDeclarationSyntax
                                {
                                    Parent: ClassDeclarationSyntax classDeclarationSyntax,
                                } fieldDeclarationSyntax,
                            },
                        })
                    {
                        context.ReportDiagnostic(Diagnostic.Create("DL.AN01", "Generation", "Attribute is not applied to field of class.", DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 0, false, location: attributeNode.GetLocation()));
                        continue;
                    }

                    if (!classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                    {
                        context.ReportDiagnostic(Diagnostic.Create("DL.AN02", "Generation", "Class is not marked as partial.", DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 0, false, location: attributeNode.GetLocation()));
                        continue;
                    }

                    if (fieldDeclarationSyntax.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                    {
                        context.ReportDiagnostic(Diagnostic.Create("DL.AN03", "Generation", "Field is marked as readonly.", DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 0, false, location: attributeNode.GetLocation()));
                        continue;
                    }

                    var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax, context.CancellationToken) as INamedTypeSymbol;

                    if (!classInfoDictionary.TryGetValue(classSymbol, out var classInfo))
                    {
                        classInfo = new ClassInfo(classSymbol);
                        classInfoDictionary[classSymbol] = classInfo;
                    }

                    fieldDeclarationSyntax.Declaration.Variables
                        .Select(variable => semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol)
                        .Foreach(classInfo.FieldSymbols.Add);
                }

                foreach (var classInfo in classInfoDictionary.Values)
                {
                    var sb = new StringBuilder();

                    sb.AppendLine("using System.ComponentModel;");
                    sb.AppendLine($"namespace {classInfo.TypeSymbol.ContainingNamespace} {{");

                    sb.AppendLine($"    partial class {classInfo.TypeSymbol.Name} : INotifyPropertyChanged {{");
                    sb.AppendLine("        public event PropertyChangedEventHandler PropertyChanged;");

                    foreach (var fieldSymbol in classInfo.FieldSymbols)
                    {
                        var propName = fieldSymbol.Name.Capitalize();
                        sb.AppendLine($"        public {fieldSymbol.Type} {propName} {{");
                        sb.AppendLine($"            get => this.{fieldSymbol.Name};");
                        sb.AppendLine("            set {");
                        sb.AppendLine($"                if(this.{fieldSymbol.Name} == value)");
                        sb.AppendLine("                    return;");
                        sb.AppendLine($"                this.{fieldSymbol.Name} = value;");
                        sb.AppendLine($"                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(\"{propName}\"));");
                        sb.AppendLine("            }");
                        sb.AppendLine("        }");
                    }

                    sb.AppendLine("    }");

                    sb.AppendLine("}");

                    var source = SourceText.From(sb.ToString(), Encoding.UTF8);
                    context.AddSource($"{classInfo.TypeSymbol}.cs", source);
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            attributeNodeCandidates.Clear();
            context.RegisterForSyntaxNotifications(() => this);
        }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is AttributeSyntax attributeSyntax && attributeSyntax.Name.ToString().Contains("AutoNotify"))
                attributeNodeCandidates.Add(attributeSyntax);
        }

        private Compilation AddAttributeCompilation(SourceText attributeSource, Compilation compilation)
        {
            var options = (compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
            return compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(attributeSource, options));
        }

        private SourceText GetAttributeSource()
        {
            using var stream = typeof(Generator).Assembly.GetManifestResourceStream("DarkLink.AutoNotify.AutoNotifyAttribute.cs");
            return SourceText.From(stream, Encoding.UTF8, canBeEmbedded: true);
        }
    }

    internal class ClassInfo
    {
        public ClassInfo(INamedTypeSymbol typeSymbol)
        {
            TypeSymbol = typeSymbol;
        }

        public IList<IFieldSymbol> FieldSymbols { get; } = new List<IFieldSymbol>();

        public INamedTypeSymbol TypeSymbol { get; }
    }
}