using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace AbilitySourceGenerator
{
    internal class AbilitySyntaxReceiver : ISyntaxReceiver
    {
        public List<InterfaceDeclarationSyntax> polymorphicInterfaces = new List<InterfaceDeclarationSyntax>();
        public List<StructDeclarationSyntax> polymorphicStructs = new List<StructDeclarationSyntax>();
        public Dictionary<string, StructDeclarationSyntax> initializeDataStructs = new Dictionary<string, StructDeclarationSyntax>();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            switch (syntaxNode)
            {
                case InterfaceDeclarationSyntax interfaceStatementSyntax:
                    if (!Utils.HasAttribute(interfaceStatementSyntax, "AbilityGenerateComponent"))
                    {
                        break;
                    }
                    polymorphicInterfaces.Add(interfaceStatementSyntax);
                    break;
                case StructDeclarationSyntax structDeclarationSyntax:
                    if (Utils.HasAttribute(structDeclarationSyntax, "AbilityGenerateStruct"))
                    {
                        polymorphicStructs.Add(structDeclarationSyntax);
                    }
                    else if (Utils.HasAttribute(structDeclarationSyntax, "AbilityGenerateInitializeData", out var argumentString))
                    {
                        argumentString = argumentString.Replace("\"", "");
                        initializeDataStructs.Add(argumentString, structDeclarationSyntax);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
