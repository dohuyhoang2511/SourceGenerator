using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace AbilitySourceGenerator
{
    internal class AbilitySyntaxReceiver : ISyntaxReceiver
    {
        public List<InterfaceDeclarationSyntax> PolymorphicInterfaces = new List<InterfaceDeclarationSyntax>();
        public List<StructDeclarationSyntax> AllStructs = new List<StructDeclarationSyntax>();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            switch (syntaxNode)
            {
                case InterfaceDeclarationSyntax interfaceStatementSyntax:
                    PolymorphicInterfaces.Add(interfaceStatementSyntax);
                    break;
                case StructDeclarationSyntax structDeclarationSyntax:
                    AllStructs.Add(structDeclarationSyntax);
                    break;
                default:
                    break;
            }
        }
    }
}
