using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AbilitySourceGenerator
{
    public class Utils
    {
        public static string GetNamespace(BaseTypeDeclarationSyntax syntax)
        {
            string empty = string.Empty;
            SyntaxNode parent = syntax.Parent;
            bool loop = true;
            // int count = 0;
            // int maxCount = 100;
            while (loop)
            {
                // count++;

                // if (count < maxCount)
                // {
                //     break;
                // }
                
                switch (parent)
                {
                    case null:
                    case NamespaceDeclarationSyntax _:
                        goto label_3;
                        // loop = false;
                        // break;
                    default:
                        parent = parent.Parent;
                        continue;
                }

                // if (loop == false)
                // {
                //     break;
                // }
            }

            label_3:
            if (parent != null && parent is NamespaceDeclarationSyntax declarationSyntax)
                empty = declarationSyntax.Name.ToString();
            return empty;
        }
        
        public static List<ISymbol> GetAllMemberSymbols(GeneratorExecutionContext context, InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(interfaceDeclarationSyntax.SyntaxTree, false);
            INamedTypeSymbol declaredSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);

            if (declaredSymbol == null)
            {
                return null;
            }

            return declaredSymbol.GetMembers().Concat(declaredSymbol.AllInterfaces.SelectMany(it => it.GetMembers())).Where(IsNotAPropertyMethod).ToList();
        }
        
        private static bool IsNotAPropertyMethod(ISymbol it)
        {
            if (!(it is IMethodSymbol methodSymbol))
                return true;
            return methodSymbol.MethodKind != MethodKind.PropertyGet && methodSymbol.MethodKind != MethodKind.PropertySet;
        }
        
        public static bool ImplementsInterface(BaseTypeDeclarationSyntax typeSyntax, string interfaceName)
        {
            if (typeSyntax.BaseList != null)
            {
                foreach (object type in typeSyntax.BaseList.Types)
                {
                    if (type.ToString() == interfaceName)
                        return true;
                }
            }

            return false;
        }
    }
}