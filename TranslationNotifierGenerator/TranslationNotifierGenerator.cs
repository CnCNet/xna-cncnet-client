using System;
//// uncomment to debug the generator
//using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace TranslationNotifierGenerator
{
    /// <summary>
    /// Generates a <c>TranslationNotifier</c> class that allows to notify the translation system
    /// about all hardcoded missing translation strings by calling <c>TranslationNotifier.Register()</c>.
    /// </summary>
    /// <remarks>
    /// It is required to make <c>RootNamespace</c> project property visible to the compiler via <c>CompilerVisibleProperty</c>
    /// (already handled in <c>Directory.Build.props</c>). This is required to generate the correct namespace for the generated class.
    /// </remarks>
    [Generator]
    public class TranslationNotifierGenerator : ISourceGenerator
    {
        public const string DescriptorId = "CNCNET0001"; // The DescriptorId is used to suppress warnings. Do not change it.
        public const string DescriptorTitle = "L10N Failure";
        public const string DescriptorCategory = "CNCNET";

        // Change those if you change the method names
        public const string LocalizeMethodContainingNamespace = "ClientCore.Extensions";
        public const string LocalizeMethodName = "L10N";

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
            //// uncomment to debug the generator
            //Debug.WriteLine($"Executing {nameof(TranslationNotifierGenerator)}...");

            var compilation = context.Compilation;
            string assemblyName = compilation.AssemblyName;

            _ = context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.RootNamespace", out string namespaceName);
            if (!namespaceName.Split(new char[] { '.' }).All(name => SyntaxFacts.IsValidIdentifier(name)))
                throw new Exception("The namespace can not contain invalid characters.");

            Dictionary<string, string> translations = new();
            foreach (var tree in compilation.SyntaxTrees)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                // https://stackoverflow.com/questions/43679690/with-roslyn-find-calling-method-from-string-literal-parameter
                var memberAccessSyntaxes = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>();
                foreach (var memberAccessSyntax in memberAccessSyntaxes)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    if (memberAccessSyntax == null
                        || !memberAccessSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                        || memberAccessSyntax.Name.ToString() != LocalizeMethodName)
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
                    object keyExprValue = semanticModel.GetConstantValue(keyNameSyntax.Expression).Value;
                    string keyName = keyExprValue?.ToString();

                    if (!l10nSyntax.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        Warn($"{l10nSyntax.Expression} is of kind {l10nSyntax.Expression.Kind()}. SimpleMemberAccessExpression is expected.", context, l10nSyntax);
                        continue;
                    }

                    var valueSyntax = l10nSyntax.Expression as MemberAccessExpressionSyntax;
                    object valueExprValue = semanticModel.GetConstantValue(valueSyntax.Expression).Value;
                    if (valueExprValue is null)
                    {
                        Warn($"Failed to get the value of key {keyName} as a string.", context, l10nSyntax);
                        continue;
                    }

                    string valueText = valueExprValue?.ToString();

                    if (translations.ContainsKey(keyName))
                    {
                        if (valueText != translations[keyName])
                            Warn($"The value of key {keyName} appears more than once and the values are not the same.", context, l10nSyntax);
                        continue;
                    }

                    translations.Add(keyName, valueText);
                }
            }

            context.CancellationToken.ThrowIfCancellationRequested();
            var sb = new StringBuilder();
            _ = sb.AppendLine(@"
using System.Collections.Generic;");
            _ = sb.AppendLine($"using {LocalizeMethodContainingNamespace};");

            _ = sb.AppendLine($"namespace {namespaceName}.Generated;");
            _ = sb.AppendLine(@"
public class TranslationNotifier
{
    public static void Register()
    {");
            foreach (var kv in translations)
                _ = sb.AppendLine($"        {kv.Value.ToLiteral()}.{LocalizeMethodName}({kv.Key.ToLiteral()});");

            _ = sb.AppendLine(@"    }
}
");

            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"TranslationNotifier.Generated.cs", sb.ToString());
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            //// uncomment to debug the generator
            //if (!Debugger.IsAttached)
            //    Debugger.Launch();

            //Debug.WriteLine($"Initalized {nameof(TranslationNotifierGenerator)}...");
        }
    }
}