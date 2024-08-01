using Microsoft.CodeAnalysis.CSharp;

namespace TranslationNotifierGenerator
{
    public static class StringExtensions
    {
        public static string ToLiteral(this string input)
        {
            // https://stackoverflow.com/a/55798623
            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(input)).ToFullString();
        }
    }
}