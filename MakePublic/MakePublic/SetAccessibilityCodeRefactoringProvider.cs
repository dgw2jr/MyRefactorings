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
using Microsoft.CodeAnalysis.Editing;

namespace SetAccessibility
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(SetAccessibilityCodeRefactoringProvider)), Shared]
    internal class SetAccessibilityCodeRefactoringProvider : CodeRefactoringProvider
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

            var model = await context.Document.GetSemanticModelAsync();
            var symbol = model.GetDeclaredSymbol(typeDecl);

            var actions = AccessibilityModifiers
                .Where(m => symbol.DeclaredAccessibility != m.Value)
                .Select(kvp => CodeAction.Create($"Make {kvp.Key}", c => SetAccessibilityAsync(context.Document, typeDecl, kvp.Value, c)));
            
            foreach (var action in actions)
            {
                context.RegisterRefactoring(action);
            }
        }

        private async Task<Solution> SetAccessibilityAsync(Document document, TypeDeclarationSyntax typeDecl, Accessibility modifier, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            editor.SetAccessibility(typeDecl, modifier);

            var newSolution = editor.GetChangedDocument().Project.Solution;

            return await Task.FromResult(newSolution);
        }
        
        private Dictionary<string, Accessibility> AccessibilityModifiers => new Dictionary<string, Accessibility>
            {
                { "private", Accessibility.Private },
                { "internal", Accessibility.Internal },
                { "protected", Accessibility.Protected },
                { "public", Accessibility.Public }
            };
    }
}