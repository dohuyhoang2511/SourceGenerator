using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace AbilitySourceGenerator
{
    internal class AbilitySyntaxReceiver : ISyntaxReceiver
    {
        public List<InterfaceDeclarationSyntax> polymorphicInterfaces = new List<InterfaceDeclarationSyntax>();
        public List<StructDeclarationSyntax> allStructs = new List<StructDeclarationSyntax>();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            switch (syntaxNode)
            {
                case InterfaceDeclarationSyntax interfaceStatementSyntax:
                    if (!Utils.HasAttribute(interfaceStatementSyntax, "AbilitySource"))
                    {
                        break;
                    }
                    polymorphicInterfaces.Add(interfaceStatementSyntax);
                    break;
                case StructDeclarationSyntax structDeclarationSyntax:
                    allStructs.Add(structDeclarationSyntax);
                    break;
                default:
                    break;
            }
        }
    }
}
