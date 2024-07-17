using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ExampleSourceGenerator
{
    public class ExampleSyntaxReceiver : ISyntaxReceiver
    {
        public List<InterfaceDeclarationSyntax> PolymorphicInterfaces = new List<InterfaceDeclarationSyntax>();
        public List<StructDeclarationSyntax> AllStructs = new List<StructDeclarationSyntax>();
        
        /// <summary>
        /// Called for each <see cref="SyntaxNode"/> in the compilation
        /// </summary>
        /// <param name="syntaxNode">The current <see cref="SyntaxNode"/> being visited</param>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            switch (syntaxNode)
            {
                case InterfaceDeclarationSyntax interfaceDeclarationSyntax:
                    this.PolymorphicInterfaces.Add(interfaceDeclarationSyntax);
                    break;
                case StructDeclarationSyntax structDeclarationSyntax:
                    this.AllStructs.Add(structDeclarationSyntax);
                    break;
                default:
                    break;
            }
        }
    }
}