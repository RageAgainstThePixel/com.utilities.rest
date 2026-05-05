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
        private static readonly LocalizableString MessageFormat = "Response must be disposed. Use 'using var response = await Rest.{0}(...)' (or dispose explicitly and suppress this diagnostic).";
        private static readonly LocalizableString Description = "A Response returned from Rest API methods (GetAsync, PostAsync, etc.) must be disposed to release native resources. Recognized patterns: 'using var', block-form 'using (...)', and try/finally with Dispose() in finally. Discarding the awaited Response (bare 'await Rest.X(...);' or '_ = await Rest.X(...);') always reports.";

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
            context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.ExpressionStatement);
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

            if (initializer is not AwaitExpressionSyntax awaitExpr)
            {
                return;
            }

            var restMethod = ResolveUnderlyingRestMethod(awaitExpr.Expression, context.SemanticModel, context.CancellationToken);

            if (restMethod == null)
            {
                return;
            }

            var localSymbol = context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken);

            if (localSymbol is ILocalSymbol local && IsDisposedInEnclosingTryFinally(localDecl, local, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, variable.GetLocation(), restMethod.Name);
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context)
        {
            var statement = (ExpressionStatementSyntax)context.Node;
            var expr = statement.Expression;

            AwaitExpressionSyntax? awaitExpr;

            if (expr is AwaitExpressionSyntax bareAwait)
            {
                awaitExpr = bareAwait;
            }
            else if (expr is AssignmentExpressionSyntax assignment &&
                     assignment.Left is IdentifierNameSyntax id &&
                     id.Identifier.ValueText == "_" &&
                     assignment.Right is AwaitExpressionSyntax discardAwait)
            {
                awaitExpr = discardAwait;
            }
            else
            {
                return;
            }

            var restMethod = ResolveUnderlyingRestMethod(awaitExpr.Expression, context.SemanticModel, context.CancellationToken);

            if (restMethod == null)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, awaitExpr.GetLocation(), restMethod.Name);
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Walks past task-wrapping members (<c>ConfigureAwait</c>, <c>WithCancellation</c>, <c>AsTask</c>) to
        /// resolve the underlying <c>Rest.{XAsync}</c> invocation, if any.
        /// </summary>
        private static IMethodSymbol? ResolveUnderlyingRestMethod(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            System.Threading.CancellationToken cancellationToken)
        {
            const int maxDepth = 8;
            var current = expression;

            for (var depth = 0; depth < maxDepth; depth++)
            {
                if (current is not InvocationExpressionSyntax invocation)
                {
                    return null;
                }

                if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
                {
                    return null;
                }

                if (IsRestMethodReturningResponse(method))
                {
                    return method;
                }

                if (!IsTaskPassThrough(method))
                {
                    return null;
                }

                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    current = memberAccess.Expression;
                    continue;
                }

                return null;
            }

            return null;
        }

        /// <summary>
        /// Members that wrap a task without changing ownership semantics, so we should walk past
        /// them to find the underlying invocation that produces the <see cref="System.Threading.Tasks.Task{T}"/>.
        /// </summary>
        private static bool IsTaskPassThrough(IMethodSymbol method)
        {
            var name = method.Name;
            return name == "ConfigureAwait" ||
                name == "WithCancellation" ||
                name == "AsTask" ||
                name == "Unwrap";
        }

        /// <summary>
        /// Returns true if the declaration is inside a try that has a finally block which disposes the given local (by symbol).
        /// </summary>
        private static bool IsDisposedInEnclosingTryFinally(
            LocalDeclarationStatementSyntax declaration,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            System.Threading.CancellationToken cancellationToken)
        {
            for (SyntaxNode? node = declaration.Parent; node != null; node = node.Parent)
            {
                if (node is not TryStatementSyntax tryStatement || tryStatement.Finally == null)
                {
                    continue;
                }

                if (!IsInTryOrCatch(tryStatement, declaration))
                {
                    continue;
                }

                if (FinallyDisposesVariable(tryStatement.Finally, localSymbol, semanticModel, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInTryOrCatch(TryStatementSyntax tryStatement, SyntaxNode declaration)
        {
            if (tryStatement.Block.Contains(declaration))
            {
                return true;
            }

            foreach (var catchClause in tryStatement.Catches)
            {
                if (catchClause.Block.Contains(declaration))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FinallyDisposesVariable(
            FinallyClauseSyntax finallyClause,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            System.Threading.CancellationToken cancellationToken)
        {
            foreach (var node in finallyClause.DescendantNodes())
            {
                if (node is not InvocationExpressionSyntax invocation)
                {
                    continue;
                }

                var receiver = GetReceiver(invocation.Expression);

                if (receiver is null)
                {
                    continue;
                }

                var receiverSymbol = semanticModel.GetSymbolInfo(receiver, cancellationToken).Symbol;

                if (SymbolEqualityComparer.Default.Equals(receiverSymbol, localSymbol))
                {
                    var methodName = (invocation.Expression as MemberAccessExpressionSyntax)?.Name?.Identifier.ValueText ??
                        (invocation.Expression as MemberBindingExpressionSyntax)?.Name?.Identifier.ValueText;
                    if (methodName == "Dispose")
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static ExpressionSyntax? GetReceiver(ExpressionSyntax expression)
        {
            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Expression;
            }

            if (expression is ConditionalAccessExpressionSyntax conditionalAccess)
            {
                return conditionalAccess.Expression;
            }

            return null;
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
