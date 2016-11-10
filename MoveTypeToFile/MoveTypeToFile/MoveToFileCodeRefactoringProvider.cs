using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeRefactoring
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MoveToFileCodeRefactoringProvider)), Shared]
    internal class MoveToFileCodeRefactoringProvider : CodeRefactoringProvider
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

            var typeDeclParentAsNamespace = typeDecl.Parent as NamespaceDeclarationSyntax;
            if (typeDeclParentAsNamespace != null)
            {
                if (typeDeclParentAsNamespace.Members.Count(m => m.GetType() == typeof(ClassDeclarationSyntax)) <= 1)
                {
                    return;
                }
            }
            else
            {
                return;
            }
            
            var action = CodeAction.Create($"Move to {typeDecl.Identifier}.cs", c => MoveToFileAsync(context.Document, typeDecl, c));

            // Register this code action.
            context.RegisterRefactoring(action);
        }

        private async Task<Solution> MoveToFileAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            // I need to clean this up.
            var identifierToken = typeDecl.Identifier;
            var newName = $"{identifierToken}.cs";

            var cu = typeDecl.SyntaxTree.GetCompilationUnitRoot();
            var usings = cu.Usings;
            
            var nodesToRemove = typeDecl.Parent.ChildNodes().Where(n => n != typeDecl && n.GetType() == typeDecl.GetType());
            var newRoot = typeDecl.Parent.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepDirectives) as NamespaceDeclarationSyntax;

            // create new file for type
            var newCu = SyntaxFactory.CompilationUnit();
            newCu = newCu.WithUsings(usings);
            newCu = newCu.AddMembers(newRoot);
            var newFile = document.Project.AddDocument(newName, newCu);

            // remove type from original document
            var originalMembers = new List<SyntaxNode> { typeDecl.Parent.RemoveNode(typeDecl, SyntaxRemoveOptions.KeepDirectives) };
            cu = cu.WithMembers(SyntaxFactory.List(originalMembers));
            var d = document.WithSyntaxRoot(cu);
            
            var newSolution = newFile.Project.Solution;

            newSolution = newSolution.WithDocumentText(document.Id, await d.GetTextAsync());

            return await Task.FromResult(newSolution);
        }
    }
}