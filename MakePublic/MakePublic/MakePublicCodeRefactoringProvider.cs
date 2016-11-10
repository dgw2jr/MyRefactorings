using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MakePublic
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MakePublicCodeRefactoringProvider)), Shared]
    internal class MakePublicCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            
            var node = root.FindNode(context.Span);

            // Only offer a refactoring if the selected node is a type declaration node.
            var typeDecl = node as TypeDeclarationSyntax;
            if (typeDecl == null)
            {
                return;
            }

            if(typeDecl.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                return;
            }
            
            var action = CodeAction.Create("Make public", c => MakePublicAsync(context.Document, typeDecl, c));
            
            context.RegisterRefactoring(action);
        }

        private async Task<Solution> MakePublicAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var cu = typeDecl.SyntaxTree.GetCompilationUnitRoot();
            var ns = typeDecl.Parent as NamespaceDeclarationSyntax;
           
            var cls = typeDecl as ClassDeclarationSyntax;
            cls = cls.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            var newNs = ns.ReplaceNode(typeDecl, cls);
            cu = cu.ReplaceNode(ns, newNs);

            var newSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, cu);

            return await Task.FromResult(newSolution);
        }
    }
}