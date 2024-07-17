using System.Collections;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#nullable enable
namespace AbilitySourceGenerator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
#nullable disable
        private const string typeEnumName = "TypeId";
        private const string typeEnumVarName = "CurrentTypeId";

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
            if (context.SyntaxReceiver is not AbilitySyntaxReceiver)
            {
                return;
            }

            AbilitySyntaxReceiver syntaxReceiver = context.SyntaxReceiver as AbilitySyntaxReceiver;

            if (syntaxReceiver.PolymorphicInterfaces == null || syntaxReceiver.PolymorphicInterfaces.Count == 0)
            {
                return;
            }
            
            foreach (var interfaceDeclarationSyntax in syntaxReceiver.PolymorphicInterfaces)
            {
                GenerateScript(context, syntaxReceiver, interfaceDeclarationSyntax);
            }
        }

        private static void GenerateScript(GeneratorExecutionContext context, AbilitySyntaxReceiver syntaxReceiver, InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            ScriptGenerateData data = GetHeader(context, interfaceDeclarationSyntax);
            List<ISymbol> allMemberSymbols = Utils.GetAllMemberSymbols(context, interfaceDeclarationSyntax);
            List<StructData> structDataList = BuildStructsData(context, syntaxReceiver, data, interfaceDeclarationSyntax);

            if (structDataList == null || structDataList.Count == 0)
            {
                return;
            }

            List<MergedFieldData> mergedFieldData = BuildMergedFields(structDataList);
            SourceText mergedStruct = GenerateMergedStruct(data, allMemberSymbols, mergedFieldData, structDataList);
            context.AddSource(data.mergedStructName, mergedStruct);
            
            foreach (var structData in structDataList)
            {
                GeneratePartialStruct(context, data.usingDirectives, data.mergedStructName, structData, mergedFieldData);
            }
        }

        private static ScriptGenerateData GetHeader(GeneratorExecutionContext context, InterfaceDeclarationSyntax interfaceDeclarationSyntax)
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

            return new ScriptGenerateData()
            {
                interfaceName = interfaceDeclarationSyntax.Identifier.ToString(),
                mergedStructName = str,
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
        
        private static List<StructData> BuildStructsData(GeneratorExecutionContext context, AbilitySyntaxReceiver syntaxReceiver, ScriptGenerateData scriptGenerateData, InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            List<StructData> structDataList = new List<StructData>();
            foreach (StructDeclarationSyntax allStruct in syntaxReceiver.AllStructs)
            {
                StructDeclarationSyntax typeSyntax = allStruct;
                SyntaxToken identifier1 = interfaceDeclarationSyntax.Identifier;
                string text = identifier1.Text;
                if (Utils.ImplementsInterface(typeSyntax, text))
                {
                    SyntaxToken identifier2 = allStruct.Identifier;
                    if (!identifier2.Text.Equals(scriptGenerateData.mergedStructName))
                    {
                        StructData structData = new StructData();
                        structData.Namespace = Utils.GetNamespace(allStruct);
                        structData.StructName = allStruct.Identifier.ToString();
                        foreach (UsingDirectiveSyntax usingDirectiveSyntax in interfaceDeclarationSyntax.SyntaxTree.GetCompilationUnitRoot().Usings)
                        {
                            TryAddUsing(scriptGenerateData.usingDirectives, usingDirectiveSyntax.Name.ToString());
                        }

                        SemanticModel semanticModel = context.Compilation.GetSemanticModel(interfaceDeclarationSyntax.SyntaxTree, false);
                        INamedTypeSymbol declaredSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);

                        List<IFieldSymbol> fieldSymbolList = declaredSymbol != null
                            ? ImmutableArrayExtensions.Where(declaredSymbol.GetMembers(),
                                it => it.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToList()
                            : null;
                        if (fieldSymbolList != null)
                        {
                            foreach (IFieldSymbol fieldSymbol in fieldSymbolList)
                                structData.Fields.Add(new StructFieldData()
                                {
                                    TypeName = fieldSymbol.Type.Name,
                                    FieldName = MapFieldNameToProperty(fieldSymbol)
                                });
                        }

                        structDataList.Add(structData);
                    }
                }
            }

            return structDataList;
        }

        private static string MapFieldNameToProperty(IFieldSymbol fieldSymbol) =>
            fieldSymbol.AssociatedSymbol is IPropertySymbol associatedSymbol ? associatedSymbol.Name : fieldSymbol.Name;
        
        private static List<MergedFieldData> BuildMergedFields(List<StructData> structDataList)
        {
            List<MergedFieldData> mergedFieldDataList = new List<MergedFieldData>();
            List<int> intList = new List<int>();
            foreach (StructData structData in structDataList)
            {
                intList.Clear();
                for (int index1 = 0; index1 < structData.Fields.Count; ++index1)
                {
                    StructFieldData field = structData.Fields[index1];
                    int index2 = -1;
                    for (int index3 = 0; index3 < mergedFieldDataList.Count; ++index3)
                    {
                        if (!intList.Contains(index3) &&
                            string.Equals(field.TypeName, mergedFieldDataList[index3].TypeName))
                        {
                            index2 = index3;
                            break;
                        }
                    }

                    if (index2 < 0)
                    {
                        int count = mergedFieldDataList.Count;
                        MergedFieldData mergedFieldData = new MergedFieldData();
                        mergedFieldData.TypeName = field.TypeName;
                        mergedFieldData.FieldName = string.Format("{0}_{1}", field.TypeName, mergedFieldDataList.Count);
                        mergedFieldData.FieldNameForStructName.Add(structData.StructName, field.FieldName);
                        mergedFieldDataList.Add(mergedFieldData);
                        field.MergedFieldName = mergedFieldData.FieldName;
                        intList.Add(count);
                    }
                    else
                    {
                        MergedFieldData mergedFieldData = mergedFieldDataList[index2];
                        mergedFieldData.FieldNameForStructName.Add(structData.StructName, field.FieldName);
                        field.MergedFieldName = mergedFieldData.FieldName;
                        intList.Add(index2);
                    }
                }
            }

            return mergedFieldDataList;
        }
        
        private static SourceText GenerateMergedStruct(ScriptGenerateData scriptGenerateData, List<ISymbol> allMemberSymbols, List<MergedFieldData> mergedFieldData,
            List<StructData> structDataList)
        {
            FileWriter fileWriter = new FileWriter();
            GenerateUsingDirectives(fileWriter, scriptGenerateData);
            fileWriter.WriteLine("");
            int num = !string.IsNullOrEmpty(scriptGenerateData.nameSpace) ? 1 : 0;
            if (num != 0)
            {
                fileWriter.WriteLine("namespace " + scriptGenerateData.nameSpace);
                fileWriter.BeginScope();
            }

            GenerateStructHeader(fileWriter, scriptGenerateData);
            fileWriter.BeginScope();
            GenerateTypeEnum(fileWriter, structDataList);
            fileWriter.WriteLine("");
            GenerateFields(fileWriter, mergedFieldData);
            fileWriter.WriteLine("");
            GenerateMethods(fileWriter, scriptGenerateData, allMemberSymbols, structDataList);
            fileWriter.WriteLine("");
            fileWriter.EndScope();
            if (num != 0)
                fileWriter.EndScope();
            return SourceText.From(fileWriter.FileContents, Encoding.UTF8, SourceHashAlgorithm.Sha1);
        }
        
        private static void GenerateUsingDirectives(FileWriter mergedStructWriter, ScriptGenerateData scriptGenerateData)
        {
            foreach (string usingDirective in scriptGenerateData.usingDirectives)
            {
                mergedStructWriter.WriteLine("using " + usingDirective + ";");
            }
        }
        
        private static void GenerateStructHeader(FileWriter structWriter, ScriptGenerateData scriptGenerateData)
        {
            structWriter.WriteLine("[Serializable]");
            structWriter.WriteLine("public partial struct " + scriptGenerateData.mergedStructName + " : " + scriptGenerateData.interfaceName);
        }
        
        private static void GenerateTypeEnum(FileWriter structWriter, List<StructData> structDataList)
        {
            structWriter.WriteLine("public enum TypeId");
            structWriter.BeginScope();
            foreach (StructData structData in structDataList)
            {
                structWriter.WriteLine(structData.StructName + ",");
            }
            structWriter.EndScope();
        }
        
        private static void GenerateFields(FileWriter structWriter, List<MergedFieldData> mergedFields)
        {
            structWriter.WriteLine("public TypeId CurrentTypeId;");
            for (int index = 0; index < mergedFields.Count; ++index)
            {
                MergedFieldData mergedField = mergedFields[index];
                structWriter.WriteLine("public " + mergedField.TypeName + " " + mergedField.FieldName + ";");
            }
        }
        
        private static void GenerateMethods(FileWriter structWriter, ScriptGenerateData scriptGenerateData, List<ISymbol> allMemberSymbols, List<StructData> structDataList)
        {
            foreach (ISymbol allMemberSymbol in allMemberSymbols)
            {
                if (allMemberSymbol is IMethodSymbol methodSymbol)
                {
                    string str1 = methodSymbol.ReturnsVoid
                        ? "void"
                        : methodSymbol.ReturnType.ToDisplayString(null);
                    string str2 = string.Join(", ",
                        ImmutableArrayExtensions.Select(methodSymbol.Parameters, it => string.Format("{0}{1} {2}", MapRefKind(it.RefKind), it.Type, it.Name)));
                    string str3 = string.Join(", ",
                        ImmutableArrayExtensions.Select(methodSymbol.Parameters, it => MapRefKind(it.RefKind) + it.Name));
                    structWriter.WriteLine("public " + str1 + " " + methodSymbol.Name + "(" + str2 + ")");
                    GenerateMethodBody(structWriter, scriptGenerateData, methodSymbol, structDataList,
                        methodSymbol.Name + "(" + str3 + ")");
                }
                else if (allMemberSymbol is IPropertySymbol propertySymbol)
                {
                    structWriter.WriteLine(string.Format("public {0} {1}", propertySymbol.Type, propertySymbol.Name));
                    structWriter.BeginScope();
                    if (propertySymbol.GetMethod != null)
                        GeneratePropertyGetMethod(structWriter, scriptGenerateData, propertySymbol.GetMethod,
                            structDataList, propertySymbol.Name);
                    if (propertySymbol.SetMethod != null)
                        GeneratePropertySetMethod(structWriter, scriptGenerateData, propertySymbol.SetMethod,
                            structDataList, propertySymbol.Name);
                    structWriter.EndScope();
                }
            }
        }
        
        private static void GeneratePropertyGetMethod(FileWriter structWriter, ScriptGenerateData scriptGenerateData, IMethodSymbol methodSymbol,
            List<StructData> structDataList, string propertyName)
        {
            structWriter.WriteLine("get");
            GenerateMethodBody(structWriter, scriptGenerateData, methodSymbol, structDataList, propertyName ?? "");
        }
        
        private static void GeneratePropertySetMethod(FileWriter structWriter, ScriptGenerateData scriptGenerateData, IMethodSymbol methodSymbol, List<StructData> structDataList,
            string propertyName)
        {
            structWriter.WriteLine("set");
            GenerateMethodBody(structWriter, scriptGenerateData, methodSymbol, structDataList, propertyName + " = value");
        }
        
        private static void GenerateMethodBody(FileWriter structWriter, ScriptGenerateData scriptGenerateData, IMethodSymbol methodSymbol, List<StructData> structDataList, string callClause)
        {
            structWriter.BeginScope();
            structWriter.WriteLine("switch(CurrentTypeId)");
            structWriter.BeginScope();
            bool returnsVoid = methodSymbol.ReturnsVoid;
            foreach (StructData structData in structDataList)
            {
                structWriter.WriteLine("case TypeId." + structData.StructName + ":");
                structWriter.BeginScope();
                string str = "instance_" + structData.StructName;
                structWriter.WriteLine(
                    structData.StructName + " " + str + " = new " + structData.StructName + "(this);");
                structWriter.WriteLine((returnsVoid ? "" : "var r = ") + str + "." + callClause + ";");
                structWriter.WriteLine(str + ".To" + scriptGenerateData.mergedStructName + "(ref this);");
                structWriter.WriteLine(returnsVoid ? "break;" : "return r;");
                structWriter.EndScope();
            }

            structWriter.WriteLine("default:");
            structWriter.BeginScope();
            foreach (IParameterSymbol parameter in methodSymbol.Parameters)
            {
                if (parameter.RefKind == RefKind.Out)
                {
                    structWriter.WriteLine(parameter.Name + " = default;");
                }
            }

            structWriter.WriteLine("return" + (returnsVoid ? "" : " default") + ";");
            structWriter.EndScope();
            structWriter.EndScope();
            structWriter.EndScope();
        }

        private static string MapRefKind(RefKind argRefKind)
        {
            switch (argRefKind)
            {
                case RefKind.None:
                    return "";
                case RefKind.Ref:
                    return "ref ";
                case RefKind.Out:
                    return "out ";
                case RefKind.In:
                    return "in ";
                default:
                    throw new ArgumentOutOfRangeException(nameof(argRefKind), (object)argRefKind, (string)null);
            }
        }
        
        private static void GeneratePartialStruct(GeneratorExecutionContext context, List<string> allUsings, string mergedStructName,
            StructData structData, List<MergedFieldData> mergedFields)
        {
            FileWriter fileWriter = new FileWriter();
            foreach (string allUsing in allUsings)
            {
                fileWriter.WriteLine("using " + allUsing + ";");
            }
            fileWriter.WriteLine("");
            if (!string.IsNullOrEmpty(structData.Namespace))
            {
                fileWriter.WriteLine("namespace " + structData.Namespace);
                fileWriter.BeginScope();
            }

            fileWriter.WriteLine("public partial struct " + structData.StructName);
            fileWriter.BeginScope();
            fileWriter.WriteLine("public " + structData.StructName + "(" + mergedStructName + " s)");
            fileWriter.BeginScope();
            foreach (StructFieldData field in structData.Fields)
            {
                fileWriter.WriteLine(field.FieldName + " = s." + field.MergedFieldName + ";");
            }
            fileWriter.EndScope();
            fileWriter.WriteLine("");
            fileWriter.WriteLine("public " + mergedStructName + " To" + mergedStructName + "()");
            fileWriter.BeginScope();
            fileWriter.WriteLine("return new " + mergedStructName);
            fileWriter.BeginScope();
            fileWriter.WriteLine("CurrentTypeId = " + mergedStructName + ".TypeId." + structData.StructName + ",");
            foreach (MergedFieldData mergedField in mergedFields)
            {
                if (mergedField.FieldNameForStructName.ContainsKey(structData.StructName))
                {
                    fileWriter.WriteLine(mergedField.FieldName + " = " + mergedField.FieldNameForStructName[structData.StructName] + ",");
                }
            }
            
            fileWriter.EndScope(";");
            fileWriter.EndScope();
            fileWriter.WriteLine("");
            fileWriter.WriteLine("public void To" + mergedStructName + "(ref " + mergedStructName + " s)");
            fileWriter.BeginScope();
            fileWriter.WriteLine("s.CurrentTypeId = " + mergedStructName + ".TypeId." + structData.StructName + ";");
            foreach (MergedFieldData mergedField in mergedFields)
            {
                if (mergedField.FieldNameForStructName.ContainsKey(structData.StructName))
                {
                    fileWriter.WriteLine("s." + mergedField.FieldName + " = " + mergedField.FieldNameForStructName[structData.StructName] + ";");
                }
            }

            fileWriter.EndScope();
            fileWriter.EndScope();
            if (!string.IsNullOrEmpty(structData.Namespace))
            {
                fileWriter.EndScope();
            }
            
            context.AddSource(structData.StructName, SourceText.From(fileWriter.FileContents, Encoding.UTF8, SourceHashAlgorithm.Sha1));
        }
        
        public class ScriptGenerateData
        {
            public string mergedStructName;
            public string interfaceName;
            public string nameSpace;
            public List<string> usingDirectives;
        }

        public class StructData
        {
            public string Namespace = "";
            public string StructName = "";
            public List<StructFieldData> Fields = new List<StructFieldData>();
        }

        public class StructFieldData
        {
            public string TypeName = "";
            public string FieldName = "";
            public string MergedFieldName = "";
        }

        public class MergedFieldData
        {
            public string TypeName = "";
            public string FieldName = "";
            public Dictionary<string, string> FieldNameForStructName = new Dictionary<string, string>();
        }
    }
}