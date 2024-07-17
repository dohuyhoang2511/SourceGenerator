using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace ExampleSourceGenerator
{
    [Generator]
    public class ExampleSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is ExampleSyntaxReceiver))
            {
                return;
            }

            ExampleSyntaxReceiver exampleSyntaxReceiver = context.SyntaxReceiver as ExampleSyntaxReceiver;
            
            System.Console.WriteLine(System.DateTime.Now.ToString());

            var sourceBuilder = new StringBuilder(
@"using System;

namespace ExampleSourceGenerated
{
    public static class ExampleSourceGenerated
    {
        public static string GetTestText() 
        {
            return ""This is from source generator ");

            sourceBuilder.Append(System.DateTime.Now.ToString());

            string stringTest = "";
            if (exampleSyntaxReceiver.PolymorphicInterfaces.Count > 0)
            {
                foreach (var interfaceDeclarationSyntax in exampleSyntaxReceiver.PolymorphicInterfaces)
                {
                    stringTest += $"{interfaceDeclarationSyntax.GetType().FullName} ";
                }
            }
            else
            {
                stringTest += "Not have interface";
            }
            
            if (exampleSyntaxReceiver.AllStructs.Count > 0)
            {
                foreach (var structDeclarationSyntax in exampleSyntaxReceiver.AllStructs)
                {
                    stringTest += $"{structDeclarationSyntax.GetType().FullName} ";
                }
            }
            else
            {
                stringTest += "Not have struct";
            }
            
            sourceBuilder.Append(stringTest);

            sourceBuilder.Append(
                @""";
        }
    }
}
");

            context.AddSource("exampleSourceGenerator", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ExampleSyntaxReceiver());
        }
    }
}