// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using System.Collections.Generic;
//
// namespace PolymorphicStructsSourceGenerators
// {
//     internal class PolymorphicStructSyntaxReceiver : ISyntaxReceiver
//     {
//         public List<InterfaceDeclarationSyntax> PolymorphicInterfaces = new List<InterfaceDeclarationSyntax>();
//         public List<StructDeclarationSyntax> AllStructs = new List<StructDeclarationSyntax>();
//
//         public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
//         {
//             switch (syntaxNode)
//             {
//                 case InterfaceDeclarationSyntax typeSyntax:
//                     if (!SourceGenUtils.HasAttribute((BaseTypeDeclarationSyntax)typeSyntax, "PolymorphicStruct"))
//                         break;
//                     this.PolymorphicInterfaces.Add(typeSyntax);
//                     break;
//                 case StructDeclarationSyntax declarationSyntax:
//                     this.AllStructs.Add(declarationSyntax);
//                     break;
//             }
//         }
//     }
// }