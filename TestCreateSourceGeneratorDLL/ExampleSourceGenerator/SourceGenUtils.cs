// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
//
// namespace PolymorphicStructsSourceGenerators
// {
//     public static class SourceGenUtils
//     {
//         public static bool HasAttribute(BaseTypeDeclarationSyntax typeSyntax, string attributeName)
//         {
//             SyntaxList<AttributeListSyntax> attributeLists = ((MemberDeclarationSyntax)typeSyntax).AttributeLists;
//             foreach (AttributeListSyntax attributeList in ((MemberDeclarationSyntax)typeSyntax).AttributeLists)
//             {
//                 foreach (AttributeSyntax attribute in attributeList.Attributes)
//                 {
//                     if (((object)attribute.Name).ToString() == attributeName)
//                         return true;
//                 }
//             }
//
//             return false;
//         }
//
//         public static bool ImplementsInterface(
//             BaseTypeDeclarationSyntax typeSyntax,
//             string interfaceName)
//         {
//             if (typeSyntax.BaseList != null)
//             {
//                 foreach (object type in typeSyntax.BaseList.Types)
//                 {
//                     if (type.ToString() == interfaceName)
//                         return true;
//                 }
//             }
//
//             return false;
//         }
//
//         public static bool ImplementsAnyInterface(ITypeSymbol typeSymbol) => typeSymbol.AllInterfaces.Length > 0;
//
//         public static IEnumerable<MethodDeclarationSyntax> GetAllMethodsOf(
//             TypeDeclarationSyntax t)
//         {
//             return ((IEnumerable)((IEnumerable<MemberDeclarationSyntax>)t.Members).Where<MemberDeclarationSyntax>(
//                     (Func<MemberDeclarationSyntax, bool>)(m =>
//                         CSharpExtensions.IsKind((SyntaxNode)m, (SyntaxKind)8875))))
//                 .OfType<MethodDeclarationSyntax>();
//         }
//
//         public static IEnumerable<PropertyDeclarationSyntax> GetAllPropertiesOf(
//             TypeDeclarationSyntax t)
//         {
//             return ((IEnumerable)((IEnumerable<MemberDeclarationSyntax>)t.Members).Where<MemberDeclarationSyntax>(
//                     (Func<MemberDeclarationSyntax, bool>)(m =>
//                         CSharpExtensions.IsKind((SyntaxNode)m, (SyntaxKind)8892))))
//                 .OfType<PropertyDeclarationSyntax>();
//         }
//
//         public static IEnumerable<FieldDeclarationSyntax> GetAllFieldsOf(
//             TypeDeclarationSyntax t)
//         {
//             return ((IEnumerable)((IEnumerable<MemberDeclarationSyntax>)t.Members).Where<MemberDeclarationSyntax>(
//                     (Func<MemberDeclarationSyntax, bool>)(m =>
//                         CSharpExtensions.IsKind((SyntaxNode)m, (SyntaxKind)8873))))
//                 .OfType<FieldDeclarationSyntax>();
//         }
//
//         public static string GetNamespace(BaseTypeDeclarationSyntax syntax)
//         {
//             string empty = string.Empty;
//             SyntaxNode parent = ((SyntaxNode)syntax).Parent;
//             while (true)
//             {
//                 switch (parent)
//                 {
//                     case null:
//                     case NamespaceDeclarationSyntax _:
//                         goto label_3;
//                     default:
//                         parent = parent.Parent;
//                         continue;
//                 }
//             }
//
//             label_3:
//             if (parent != null && parent is NamespaceDeclarationSyntax declarationSyntax)
//                 empty = ((object)declarationSyntax.Name).ToString();
//             return empty;
//         }
//
//         public static List<ISymbol> GetAllMemberSymbols(
//             GeneratorExecutionContext context,
//             InterfaceDeclarationSyntax polymorphicInterface)
//         {
//             INamedTypeSymbol declaredSymbol = CSharpExtensions
//                     .GetDeclaredSymbol(((GeneratorExecutionContext) ref context).Compilation
//                     .GetSemanticModel(((SyntaxNode)polymorphicInterface).SyntaxTree, false),
//   (BaseTypeDeclarationSyntax)polymorphicInterface, ((GeneratorExecutionContext) ref context)
//                 .CancellationToken);
//             return declaredSymbol == null
//                 ? (List<ISymbol>)null
//                 : ((IEnumerable<ISymbol>)((INamespaceOrTypeSymbol)declaredSymbol).GetMembers()).Concat<ISymbol>(
//                     declaredSymbol != null
//                         ? ((IEnumerable<INamedTypeSymbol>)((ITypeSymbol)declaredSymbol).AllInterfaces)
//                         .SelectMany<INamedTypeSymbol, ISymbol>((Func<INamedTypeSymbol, IEnumerable<ISymbol>>)(it =>
//                             (IEnumerable<ISymbol>)((INamespaceOrTypeSymbol)it).GetMembers()))
//                         : (IEnumerable<ISymbol>)null)
//                 .Where<ISymbol>(new Func<ISymbol, bool>(SourceGenUtils.IsNotAPropertyMethod)).ToList<ISymbol>();
//         }
//
//         private static bool IsNotAPropertyMethod(ISymbol it)
//         {
//             if (!(it is IMethodSymbol imethodSymbol))
//                 return true;
//             return imethodSymbol.MethodKind != 11 && imethodSymbol.MethodKind != 12;
//         }
//     }
// }