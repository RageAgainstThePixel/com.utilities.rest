// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Utilities.Rest.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ResponseNotDisposedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UTILSREST001";
        private const string Category = "Usage";

        private static readonly LocalizableString Title = "Response must be disposed";
        private static readonly LocalizableString MessageFormat = "Response must be disposed. Use 'using var response = await Rest.{0}(...)' or call response.Dispose().";
        private static readonly LocalizableString Description = "A Response returned from Rest API methods (GetAsync, PostAsync, etc.) must be disposed to release native resources.";

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeLocalDeclaration, SyntaxKind.LocalDeclarationStatement);
        }

        private static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context)
        {
            var localDecl = (LocalDeclarationStatementSyntax)context.Node;

            // Using variable declaration is correct; no diagnostic.
            if (localDecl.UsingKeyword.Kind() == SyntaxKind.UsingKeyword)
            {
                return;
            }

            if (localDecl.Declaration?.Variables.Count != 1)
            {
                return;
            }

            var variable = localDecl.Declaration.Variables[0];
            var initializer = variable.Initializer?.Value;

            // Must be await SomeInvocation()
            if (initializer is not AwaitExpressionSyntax awaitExpr)
            {
                return;
            }

            var invoked = awaitExpr.Expression;

            if (invoked is not InvocationExpressionSyntax invocation)
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var symbolInfo = semanticModel.GetSymbolInfo(invocation, context.CancellationToken);

            if (symbolInfo.Symbol is not IMethodSymbol method)
            {
                return;
            }

            if (!IsRestMethodReturningResponse(method))
            {
                return;
            }

            var methodName = method.Name;
            var diagnostic = Diagnostic.Create(Rule, variable.GetLocation(), methodName);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsRestMethodReturningResponse(IMethodSymbol method)
        {
            if (method.ReturnType is not INamedTypeSymbol returnType)
            {
                return false;
            }

            // Task<T> check: match by metadata name so we don't depend on ToString() format (e.g. assembly-qualified)
            if (returnType.OriginalDefinition?.MetadataName != "Task`1")
            {
                return false;
            }

            if (returnType.TypeArguments.Length != 1)
            {
                return false;
            }

            var responseType = returnType.TypeArguments[0];

            if (responseType is not INamedTypeSymbol responseNamed)
            {
                return false;
            }

            if (responseNamed.MetadataName != "Response")
            {
                return false;
            }

            var responseNs = responseNamed.ContainingNamespace?.ToDisplayString();

            if (responseNs != "Utilities.WebRequestRest")
            {
                return false;
            }

            var name = method.Name;

            if (name != "GetAsync" &&
                name != "PutAsync" &&
                name != "PostAsync" &&
                name != "PatchAsync" &&
                name != "DeleteAsync")
            {
                return false;
            }

            var containing = method.ContainingType;

            if (containing is not { MetadataName: "Rest" })
            {
                return false;
            }

            var restNs = containing.ContainingNamespace?.ToDisplayString();
            return restNs == "Utilities.WebRequestRest";
        }
    }
}
