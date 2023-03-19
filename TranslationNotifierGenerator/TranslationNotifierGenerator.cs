using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        // Change those if you change the method names
        public const string LocalizeMethodContainingNamespace = "ClientCore.Extensions";
        public const string LocalizeMethodName = "L10N";

        public void Execute(GeneratorExecutionContext context)
        {
            // uncomment to debug the generator
            //Debug.WriteLine($"Executing {nameof(TranslationNotifierGenerator)}...");

            var compilation = context.Compilation;

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

                    if (memberAccessSyntax.Parent is not InvocationExpressionSyntax l10nSyntax
                        || l10nSyntax.ArgumentList.Arguments.Count == 0
                        || !l10nSyntax.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        continue;
                    }

                    // https://stackoverflow.com/questions/35670115/how-to-use-roslyn-to-get-compile-time-constant-value
                    var semanticModel = compilation.GetSemanticModel(l10nSyntax.SyntaxTree);

                    // Get the key and the value.
                    var keyNameSyntax = l10nSyntax.ArgumentList.Arguments[0];
                    string keyName = semanticModel.GetConstantValue(keyNameSyntax.Expression).Value?.ToString();
                    Debug.Assert(keyName is null == !keyNameSyntax.Expression.IsKind(SyntaxKind.StringLiteralExpression));
                    bool keyNameIsPotentiallyForIni = keyName is null || keyName.StartsWith("INI:");

                    var valueTextSyntax = l10nSyntax.Expression as MemberAccessExpressionSyntax;
                    string valueText = semanticModel.GetConstantValue(valueTextSyntax.Expression).Value?.ToString();

                    if (keyNameIsPotentiallyForIni)
                    {
                        if (valueText is not null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                                "CNCNET0001", "Literal INI translation value",
                                "The value of an INI translation should not be a compile-time string.",
                                "CNCNET", DiagnosticSeverity.Warning, isEnabledByDefault: true), l10nSyntax.GetLocation()));
                        }

                        continue;
                    }

                    if (keyName is not null && valueText is null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                            "CNCNET0002", "Non-literal translation value",
                            $"Failed to get the value of key {keyName} as a compile-time string.",
                            "CNCNET", DiagnosticSeverity.Warning, isEnabledByDefault: true), l10nSyntax.GetLocation()));
                        continue;
                    }

                    // Check for duplicates.
                    if (translations.ContainsKey(keyName))
                    {
                        if (valueText != translations[keyName])
                        {
                            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                                "CNCNET0003", "Conflict translation items",
                                $"Key {keyName} is defined more than once and the values are not the same.",
                                "CNCNET", DiagnosticSeverity.Warning, isEnabledByDefault: true), l10nSyntax.GetLocation()));
                        }

                        continue;
                    }

                    // Avoid trimmable strings
                    if (valueText.Trim() != valueText)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                            "CNCNET0004", "Trimmable translation value",
                            $"The value of key {keyName} should not have leading or trailing whitespace.",
                            "CNCNET", DiagnosticSeverity.Warning, isEnabledByDefault: true), l10nSyntax.GetLocation()));
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
            // uncomment to debug the generator
            //if (!Debugger.IsAttached)
            //    Debugger.Launch();

            //Debug.WriteLine($"Initalized {nameof(TranslationNotifierGenerator)}...");
        }
    }
}