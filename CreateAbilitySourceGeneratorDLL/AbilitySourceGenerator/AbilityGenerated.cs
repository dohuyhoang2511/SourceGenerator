using System.Collections;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#nullable enable
namespace AbilitySourceGenerator
{
    public class HeaderData
    {
        public string scriptName = "";
        public string interfaceName = "";
        public string nameSpace = "";
        public List<string> usingDirectives = new List<string>();
    }

    public class StructData
    {
        public string nameSpace = "";
        public string structName = "";
        public List<StructFieldData> fields = new List<StructFieldData>();
    }

    public class StructFieldData
    {
        public string typeName = "";
        public string fieldName = "";
        public string mergedFieldName = "";
    }

    public class MergedFieldData
    {
        public string typeName = "";
        public string fieldName = "";
        public Dictionary<string, string> fieldNameForStructName = new Dictionary<string, string>();
    }

    [Generator]
    public class AbilityGenerated : ISourceGenerator
    {
#nullable disable
        // private const string typeEnumName = "UnitAbilityPolymorphismType";
        // private const string typeEnumVarName = "currentUnitAbilityPolymorphismType";

        /// <summary>
        /// Called before generation occurs. A generator can use the <paramref name="context"/>
        /// to register callbacks required to perform generation.
        /// </summary>
        /// <param name="context">The <see cref="GeneratorInitializationContext"/> to register callbacks on</param>
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new AbilitySyntaxReceiver());
        }
    
        /// <summary>
        /// Called to perform source generation. A generator can use the <paramref name="context"/>
        /// to add source files via the <see cref="GeneratorExecutionContext.AddSource(string, SourceText)"/> 
        /// method.
        /// </summary>
        /// <param name="context">The <see cref="GeneratorExecutionContext"/> to add source to</param>
        /// <remarks>
        /// This call represents the main generation step. It is called after a <see cref="Compilation"/> is 
        /// created that contains the user written code. 
        /// 
        /// A generator can use the <see cref="GeneratorExecutionContext.Compilation"/> property to
        /// discover information about the users compilation and make decisions on what source to 
        /// provide. 
        /// </remarks>
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not AbilitySyntaxReceiver syntaxReceiver)
            {
                return;
            }

            if (syntaxReceiver.polymorphicInterfaces.Count == 0)
            {
                return;
            }

            foreach (var interfaceDeclarationSyntax in syntaxReceiver.polymorphicInterfaces)
            {
                GenerateStructFromInterface(context, syntaxReceiver, interfaceDeclarationSyntax);
            }
        }

        private void DebugInEditorUnity(GeneratorExecutionContext context, string str)
        {
            DiagnosticDescriptor descriptor = new DiagnosticDescriptor("Debug", "", $"\n{str}", "",
                DiagnosticSeverity.Error, true);
            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, DiagnosticSeverity.Error));
        }
        
        private void GenerateStructFromInterface(GeneratorExecutionContext context, AbilitySyntaxReceiver syntaxReceiver, InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            HeaderData headerData = GetHeader(interfaceDeclarationSyntax);
            List<ISymbol> allMemberSymbols = Utils.GetAllMemberSymbols(context, interfaceDeclarationSyntax);
            List<StructData> structDataList = BuildStructsData(context, syntaxReceiver, headerData, interfaceDeclarationSyntax);
        
            if (structDataList == null || structDataList.Count == 0)
            {
                return;
            }
            
            List<MergedFieldData> mergedFieldData = BuildMergedFields(structDataList);
            SourceText mergedStruct = GenerateMergedStruct(headerData, allMemberSymbols, mergedFieldData, structDataList);

            context.AddSource(headerData.scriptName, mergedStruct);

            // DebugInEditorUnity(context, $"headerData.scriptName: {headerData.scriptName} \n headerData.interfaceName: {headerData.interfaceName}");
            
            // foreach (var structData in structDataList)
            // {
            //     GeneratePartialStruct(context, headerData.usingDirectives, headerData.scriptName, structData, mergedFieldData);  
            // }
        
            return;
        }

        private static HeaderData GetHeader(InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            var identifier = interfaceDeclarationSyntax.Identifier;
            var str = identifier.Text.Substring(1);
            var newUsing = Utils.GetNamespace(interfaceDeclarationSyntax);
            var allUsing = new List<string>();
            TryAddUsing(allUsing, "System");
            if (!string.IsNullOrEmpty(newUsing))
            {
                TryAddUsing(allUsing, newUsing);
            }

            foreach (var usingDirectiveSyntax in interfaceDeclarationSyntax.SyntaxTree.GetCompilationUnitRoot().Usings)
            {
                TryAddUsing(allUsing, usingDirectiveSyntax.Name.ToString());
            }

            return new HeaderData()
            {
                interfaceName = interfaceDeclarationSyntax.Identifier.ToString(),
                scriptName = str,
                nameSpace = newUsing,
                usingDirectives = allUsing,
            };
        }

        private static void TryAddUsing(List<string> allUsing, string newUsing)
        {
            if (allUsing.Contains(newUsing))
            {
                return;
            }
            
            allUsing.Add(newUsing);
        }
        
        private List<StructData> BuildStructsData(GeneratorExecutionContext context, AbilitySyntaxReceiver syntaxReceiver, HeaderData headerData, InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            List<StructData> structDataList = new List<StructData>();
            foreach (StructDeclarationSyntax allStruct in syntaxReceiver.allStructs)
            {
                StructDeclarationSyntax typeSyntax = allStruct;
                SyntaxToken identifier1 = interfaceDeclarationSyntax.Identifier;
                string text = identifier1.Text;
                if (Utils.ImplementsInterface(typeSyntax, text))
                {
                    SyntaxToken identifier2 = allStruct.Identifier;
                    if (!identifier2.Text.Equals(headerData.scriptName))
                    {
                        StructData structData = new StructData
                        {
                            nameSpace = Utils.GetNamespace(allStruct),
                            structName = allStruct.Identifier.ToString()
                        };
                        foreach (var usingDirectiveSyntax in interfaceDeclarationSyntax.SyntaxTree.GetCompilationUnitRoot().Usings)
                        {
                            TryAddUsing(headerData.usingDirectives, usingDirectiveSyntax.Name.ToString());
                        }

                        SemanticModel semanticModel = context.Compilation.GetSemanticModel(interfaceDeclarationSyntax.SyntaxTree, false);
                        INamedTypeSymbol declaredSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);

                        List<IFieldSymbol> fieldSymbolList = declaredSymbol?.GetMembers().Where(it => it.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToList();
                        if (fieldSymbolList != null)
                        {
                            foreach (var fieldSymbol in fieldSymbolList)
                            {
                                structData.fields.Add(new StructFieldData()
                                {
                                    typeName = fieldSymbol.Type.Name,
                                    fieldName = MapFieldNameToProperty(fieldSymbol)
                                });
                            }
                        }

                        structDataList.Add(structData);
                    }
                }
            }

            return structDataList;
        }

        private static string MapFieldNameToProperty(IFieldSymbol fieldSymbol) =>
            fieldSymbol.AssociatedSymbol is IPropertySymbol associatedSymbol ? associatedSymbol.Name : fieldSymbol.Name;
        
        private List<MergedFieldData> BuildMergedFields(List<StructData> structDataList)
        {
            List<MergedFieldData> mergedFieldDataList = new List<MergedFieldData>();
            List<int> intList = new List<int>();
            foreach (var structData in structDataList)
            {
                intList.Clear();
                foreach (var field in structData.fields)
                {
                    int index2 = -1;
                    for (int index3 = 0; index3 < mergedFieldDataList.Count; ++index3)
                    {
                        if (!intList.Contains(index3) &&
                            string.Equals(field.typeName, mergedFieldDataList[index3].typeName))
                        {
                            index2 = index3;
                            break;
                        }
                    }

                    if (index2 < 0)
                    {
                        int count = mergedFieldDataList.Count;
                        MergedFieldData mergedFieldData = new MergedFieldData
                        {
                            typeName = field.typeName,
                            fieldName = $"{field.typeName}_{mergedFieldDataList.Count}"
                        };
                        mergedFieldData.fieldNameForStructName.Add(structData.structName, field.fieldName);
                        mergedFieldDataList.Add(mergedFieldData);
                        field.mergedFieldName = mergedFieldData.fieldName;
                        intList.Add(count);
                    }
                    else
                    {
                        MergedFieldData mergedFieldData = mergedFieldDataList[index2];
                        mergedFieldData.fieldNameForStructName.Add(structData.structName, field.fieldName);
                        field.mergedFieldName = mergedFieldData.fieldName;
                        intList.Add(index2);
                    }
                }
            }

            return mergedFieldDataList;
        }
        
        private SourceText GenerateMergedStruct(HeaderData headerData, List<ISymbol> allMemberSymbols, List<MergedFieldData> mergedFieldData,
            List<StructData> structDataList)
        {
            FileWriter fileWriter = new FileWriter();
            GenerateUsingDirectives(fileWriter, headerData);
            fileWriter.WriteLine("");
            int num = !string.IsNullOrEmpty(headerData.nameSpace) ? 1 : 0;
            if (num != 0)
            {
                fileWriter.WriteLine($"namespace {headerData.nameSpace}");
                fileWriter.BeginScope();
            }
            
            GenerateTypeEnum(fileWriter, structDataList);
            fileWriter.WriteLine("");
            GenerateStructHeader(fileWriter, headerData);
            fileWriter.BeginScope();
            GenerateFields(fileWriter, mergedFieldData);
            fileWriter.WriteLine("");
            GenerateMethods(fileWriter, headerData, allMemberSymbols, structDataList);
            fileWriter.WriteLine("");
            fileWriter.EndScope();
            
            if (num != 0)
            {
                fileWriter.EndScope();
            }
            return SourceText.From(fileWriter.FileContents, Encoding.UTF8);
        }
        
        private static void GenerateUsingDirectives(FileWriter mergedStructWriter, HeaderData headerData)
        {
            foreach (string usingDirective in headerData.usingDirectives)
            {
                mergedStructWriter.WriteLine($"using {usingDirective};");
            }
        }
        
        private void GenerateStructHeader(FileWriter structWriter, HeaderData headerData)
        {
            structWriter.WriteLine("[Serializable]");
            structWriter.WriteLine($"public partial struct {headerData.scriptName} : {headerData.interfaceName}");
        }
        
        private void GenerateTypeEnum(FileWriter structWriter, List<StructData> structDataList)
        {
            structWriter.WriteLine("public enum UnitAbilityPolymorphismType");
            structWriter.BeginScope();
            foreach (var structData in structDataList)
            {
                structWriter.WriteLine($"{structData.structName},");
            }
            structWriter.EndScope();
        }
        
        private void GenerateFields(FileWriter structWriter, List<MergedFieldData> mergedFields)
        {
            structWriter.WriteLine("public UnitAbilityPolymorphismType currentUnitAbilityPolymorphismType;");
            foreach (var mergedField in mergedFields)
            {
                structWriter.WriteLine($"public {mergedField.typeName} {mergedField.fieldName};");
            }
        }
        
        private void GenerateMethods(FileWriter structWriter, HeaderData headerData, List<ISymbol> allMemberSymbols, List<StructData> structDataList)
        {
            if (allMemberSymbols == null || allMemberSymbols.Count == 0)
            {
                return;
            }
            
            foreach (var memberSymbol in allMemberSymbols)
            {
                if (memberSymbol is IMethodSymbol methodSymbol)
                {
                    string str1 = methodSymbol.ReturnsVoid ? "void" : methodSymbol.ReturnType.ToDisplayString();
                    string str2 = string.Join(", ", methodSymbol.Parameters.Select(parameterSymbol => $"{MapRefKind(parameterSymbol.RefKind)}{parameterSymbol.Type} {parameterSymbol.Name}"));
                    string str3 = string.Join(", ", methodSymbol.Parameters.Select(parameterSymbol => $"{MapRefKind(parameterSymbol.RefKind)}{parameterSymbol.Name}"));
                    structWriter.WriteLine($"public {str1} {methodSymbol.Name}({str2})");
                    GenerateMethodBody(structWriter, headerData, methodSymbol, structDataList, $"{methodSymbol.Name}({str3})");
                }
                else if (memberSymbol is IPropertySymbol propertySymbol)
                {
                    structWriter.WriteLine($"public {propertySymbol.Type} {propertySymbol.Name}");
                    structWriter.BeginScope();
                    if (propertySymbol.GetMethod != null)
                    {
                        GeneratePropertyGetMethod(structWriter, headerData, propertySymbol.GetMethod, structDataList, propertySymbol.Name);
                    }
                    if (propertySymbol.SetMethod != null)
                    {
                        GeneratePropertySetMethod(structWriter, headerData, propertySymbol.SetMethod, structDataList, propertySymbol.Name);
                    }
                    structWriter.EndScope();
                }
            }
        }
        
        private static void GeneratePropertyGetMethod(FileWriter structWriter, HeaderData headerData, IMethodSymbol methodSymbol,
            List<StructData> structDataList, string propertyName)
        {
            structWriter.WriteLine("get");
            GenerateMethodBody(structWriter, headerData, methodSymbol, structDataList, propertyName ?? "");
        }
        
        private static void GeneratePropertySetMethod(FileWriter structWriter, HeaderData headerData, IMethodSymbol methodSymbol, List<StructData> structDataList,
            string propertyName)
        {
            structWriter.WriteLine("set");
            GenerateMethodBody(structWriter, headerData, methodSymbol, structDataList,  $"{propertyName} = value");
        }
        
        private static void GenerateMethodBody(FileWriter structWriter, HeaderData headerData, IMethodSymbol methodSymbol, List<StructData> structDataList, string callClause)
        {
            structWriter.BeginScope();
            structWriter.WriteLine("switch(currentUnitAbilityPolymorphismType)");
            structWriter.BeginScope();
            bool returnsVoid = methodSymbol.ReturnsVoid;
            foreach (var structData in structDataList)
            {
                structWriter.WriteLine($"case UnitAbilityPolymorphismType.{structData.structName}:");
                structWriter.BeginScope();
                // string str = $"instance_{structData.structName}";
                // structWriter.WriteLine($"{structData.structName} {str} = new {structData.structName}(this);");
                // structWriter.WriteLine((returnsVoid ? "" : "var r = ") + $"{str}.{callClause};");
                // structWriter.WriteLine($"{str}.To{headerData.scriptName}(ref this);");
                structWriter.WriteLine(returnsVoid ? "break;" : "return r;");
                structWriter.EndScope();
            }

            structWriter.WriteLine("default:");
            structWriter.BeginScope();
            foreach (var parameter in methodSymbol.Parameters)
            {
                if (parameter.RefKind == RefKind.Out)
                {
                    structWriter.WriteLine($"{parameter.Name} = default;");
                }
            }

            structWriter.WriteLine("return" + (returnsVoid ? "" : " default") + ";");
            structWriter.EndScope();
            
            structWriter.EndScope();
            structWriter.EndScope();
        }

        private static string MapRefKind(RefKind argRefKind)
        {
            return argRefKind switch
            {
                RefKind.None => "",
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                _ => throw new ArgumentOutOfRangeException(nameof(argRefKind), argRefKind, null)
            };
        }
        
        private static void GeneratePartialStruct(GeneratorExecutionContext context, List<string> allUsings, string mergedStructName,
            StructData structData, List<MergedFieldData> mergedFields)
        {
            FileWriter fileWriter = new FileWriter();
            foreach (var allUsing in allUsings)
            {
                fileWriter.WriteLine($"using {allUsing};");
            }
            fileWriter.WriteLine("");
            if (!string.IsNullOrEmpty(structData.nameSpace))
            {
                fileWriter.WriteLine($"namespace {structData.nameSpace}");
                fileWriter.BeginScope();
            }

            fileWriter.WriteLine($"public partial struct {structData.structName}");
            fileWriter.BeginScope();
            
            // fileWriter.WriteLine($"public {structData.structName}({mergedStructName} s)");
            // fileWriter.BeginScope();
            // foreach (var field in structData.fields)
            // {
            //     fileWriter.WriteLine($"{field.fieldName} = s.{field.mergedFieldName};");
            // }
            // fileWriter.EndScope();
            // fileWriter.WriteLine("");
            
            // fileWriter.WriteLine($"public {mergedStructName} To{mergedStructName}()");
            // fileWriter.BeginScope();
            // fileWriter.WriteLine($"return new {mergedStructName}");
            // fileWriter.BeginScope();
            // fileWriter.WriteLine($"currentUnitAbilityPolymorphismType = {mergedStructName}.UnitAbilityPolymorphismType.{structData.structName},");
            // foreach (var mergedField in mergedFields)
            // {
            //     if (mergedField.fieldNameForStructName.ContainsKey(structData.structName))
            //     {
            //         fileWriter.WriteLine($"{mergedField.fieldName} = {mergedField.fieldNameForStructName[structData.structName]},");
            //     }
            // }
            //
            // fileWriter.EndScope(";");
            // fileWriter.EndScope();
            // fileWriter.WriteLine("");
            
            // fileWriter.WriteLine($"public void To{mergedStructName}(ref {mergedStructName} s)");
            // fileWriter.BeginScope();
            // fileWriter.WriteLine($"s.currentUnitAbilityPolymorphismType = {mergedStructName}.UnitAbilityPolymorphismType.{structData.structName};");
            // foreach (var mergedField in mergedFields)
            // {
            //     if (mergedField.fieldNameForStructName.ContainsKey(structData.structName))
            //     {
            //         fileWriter.WriteLine($"s.{mergedField.fieldName} = {mergedField.fieldNameForStructName[structData.structName]};");
            //     }
            // }
            //
            // fileWriter.EndScope();
            
            fileWriter.EndScope();
            
            if (!string.IsNullOrEmpty(structData.nameSpace))
            {
                fileWriter.EndScope();
            }
            
            context.AddSource(structData.structName, SourceText.From(fileWriter.FileContents, Encoding.UTF8));
        }
    }
}