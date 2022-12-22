using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace LiteralTranslationGenerator
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        public const string DescriptorId = "CNCNET0001"; // The DescriptorId is used to suppress warnings. Do not change it.
        public const string DescriptorTitle = "L10N Failure";
        public const string DescriptorCategory = "CNCNET";

        private readonly IReadOnlyDictionary<string, string> AssemblyToNamespace = new Dictionary<string, string>()
        {
            {"ClientCore","ClientCore"},
            {"ClientGUI","ClientGUI"},
            {"DTAConfig","DTAConfig"},
            {"clientdx","DTAClient"},
            {"clientogl","DTAClient"},
            {"clientxna","DTAClient"},
        };

        private void Warn(string text, GeneratorExecutionContext context, SyntaxNode node)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(DescriptorId, DescriptorTitle, text,
                        DescriptorCategory, DiagnosticSeverity.Warning, true),
                    node.GetLocation()));
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;
            string assemblyName = compilation.AssemblyName;
            if (!AssemblyToNamespace.ContainsKey(assemblyName))
                throw new Exception("Unrecognized assembly name.");

            string namespaceName = AssemblyToNamespace[assemblyName];
            if (!namespaceName.Split(new char[] { '.' }).All(name => SyntaxFacts.IsValidIdentifier(name)))
                throw new Exception("The assembly name is considered as the namespace name. Can not contain invalid characters.");

            Dictionary<string, string> translations = new();
            foreach (var tree in compilation.SyntaxTrees)
            {
                // https://stackoverflow.com/questions/43679690/with-roslyn-find-calling-method-from-string-literal-parameter
                var memberAccessSyntaxes = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>();
                foreach (var memberAccessSyntax in memberAccessSyntaxes)
                {
                    if (memberAccessSyntax == null
                        || !memberAccessSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                        || memberAccessSyntax.Name.ToString() != "L10N")
                    {
                        continue;
                    }

                    var l10nSyntax = memberAccessSyntax.Parent as InvocationExpressionSyntax;
                    if (l10nSyntax is null
                        || l10nSyntax.ArgumentList.Arguments.Count == 0)
                    {
                        continue;
                    }

                    var keyNameSyntax = l10nSyntax.ArgumentList.Arguments[0];
                    if (!keyNameSyntax.Expression.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        Warn($"{keyNameSyntax.Expression} is of kind {keyNameSyntax.Expression.Kind()}. StringLiteralExpression is expected.", context, l10nSyntax);
                        continue;
                    }

                    // https://stackoverflow.com/questions/35670115/how-to-use-roslyn-to-get-compile-time-constant-value
                    var semanticModel = compilation.GetSemanticModel(keyNameSyntax.SyntaxTree);
                    object keyValue = semanticModel.GetConstantValue(keyNameSyntax.Expression).Value;
                    string keyName = keyValue?.ToString();

                    if (!l10nSyntax.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        Warn($"{l10nSyntax.Expression} is of kind {l10nSyntax.Expression.Kind()}. SimpleMemberAccessExpression is expected.", context, l10nSyntax);
                        continue;
                    }

                    var valueSyntax = l10nSyntax.Expression as MemberAccessExpressionSyntax;
                    object valueValue = semanticModel.GetConstantValue(valueSyntax.Expression).Value;
                    if (valueValue is null)
                    {
                        Warn($"Failed to get the value of key {keyName} as a string.", context, l10nSyntax);
                        continue;
                    }

                    string valueText = semanticModel.GetConstantValue(valueSyntax.Expression).Value?.ToString();

                    if (translations.ContainsKey(keyName))
                    {
                        if (valueText != translations[keyName])
                            Warn($"The value of key {keyName} appears more than once and the values are not the same.", context, l10nSyntax);
                        continue;
                    }

                    translations.Add(keyName, valueText);
                }
            }

            var sb = new StringBuilder();
            _ = sb.AppendLine(@"
using System.Collections.Generic;
using ClientCore.Extensions;
");
            _ = sb.AppendLine($"namespace {namespaceName}.Generated;");
            _ = sb.AppendLine(@"
public class TranslationNotifier
{
    public static void Register()
    {
");
            foreach (var kv in translations)
                _ = sb.AppendLine($"{kv.Value.ToLiteral()}.L10N({kv.Key.ToLiteral()});");

            _ = sb.AppendLine(@"
    }
}
");

            context.AddSource($"TranslationNotifier.Generated.cs", sb.ToString());
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }
}