using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AbilitySourceGenerator
{
    public static class Utils
    {
        public static bool HasAttribute(BaseTypeDeclarationSyntax typeSyntax, string attributeName)
        {
            foreach (var attributeList in typeSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (attribute.Name.ToString() == attributeName)
                        return true;
                }
            }

            return false;
        }
        
        public static string GetNamespace(BaseTypeDeclarationSyntax syntax)
        {
            string empty = string.Empty;
            SyntaxNode? parent = syntax.Parent;
            while (true)
            {
                switch (parent)
                {
                    case null:
                    case NamespaceDeclarationSyntax _:
                        goto label_3;
                    default:
                        parent = parent.Parent;
                        continue;
                }
            }

            label_3:
            if (parent != null && parent is NamespaceDeclarationSyntax declarationSyntax)
                empty = declarationSyntax.Name.ToString();
            return empty;
        }
        
        public static List<ISymbol> GetAllMemberSymbols(GeneratorExecutionContext context, InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(interfaceDeclarationSyntax.SyntaxTree);
            INamedTypeSymbol? declaredSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);

            if (declaredSymbol == null)
            {
                return new List<ISymbol>();
            }

            return declaredSymbol.GetMembers().Concat(declaredSymbol.AllInterfaces.SelectMany(it => it.GetMembers())).Where(IsNotAPropertyMethod).ToList();
        }
        
        private static bool IsNotAPropertyMethod(ISymbol it)
        {
            if (it is not IMethodSymbol methodSymbol)
                return true;
            return methodSymbol.MethodKind != MethodKind.PropertyGet && methodSymbol.MethodKind != MethodKind.PropertySet;
        }
        
        public static bool ImplementsInterface(BaseTypeDeclarationSyntax typeSyntax, string interfaceName)
        {
            if (typeSyntax.BaseList != null)
            {
                foreach (var type in typeSyntax.BaseList.Types)
                {
                    if (type.ToString() == interfaceName)
                        return true;
                }
            }

            return false;
        }
    }
}