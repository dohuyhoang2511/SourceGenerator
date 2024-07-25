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
        public Dictionary<string, List<StructFieldData>> fieldsByTypeNameDict =
            new Dictionary<string, List<StructFieldData>>();
    }

    public class StructFieldData
    {
        public IFieldSymbol? fieldSymbol;
        public string typeName = "";
        public string fieldName = "";
    }

    public class MergedFieldData
    {
        public string typeName = "";
        public string fieldName = "";
    }

    [Generator]
#pragma warning disable RS1036
    public class AbilityGenerated : ISourceGenerator
#pragma warning restore RS1036
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

            if (syntaxReceiver.initializeDataStructs.Count > 0)
            {
                foreach (var pair in syntaxReceiver.initializeDataStructs)
                {
                    string targetStructPointerName = pair.Key;
                    List<StructDeclarationSyntax> structDeclarationSyntaxList = pair.Value;

                    foreach (var structDeclarationSyntax in structDeclarationSyntaxList)
                    {
                        List<StructFieldData> allFields = GetAllFieldsOfStruct(context, structDeclarationSyntax);

                        GenerateStructPolymorphismFromInitializeData(context, targetStructPointerName, structDeclarationSyntax, allFields);
                        GenerateStructInitializeData(context, targetStructPointerName, structDeclarationSyntax, allFields);
                    }
                }
            }
            
            if (syntaxReceiver.polymorphicInterfaces.Count > 0)
            {
                foreach (var interfaceDeclarationSyntax in syntaxReceiver.polymorphicInterfaces)
                {
                    GenerateStructComponentFromInterface(context, syntaxReceiver, interfaceDeclarationSyntax);
                }
            }
        }

        #region Generate Struct Polymorphism From Initialize Data

        private string GetStructPolymorphismNameFromStructInitializeData(string initializeStructName)
        {
            return initializeStructName.Substring(0, initializeStructName.Length - 14);
        }
        
        private void GenerateStructPolymorphismFromInitializeData(GeneratorExecutionContext context, string targetStructPointer, StructDeclarationSyntax structDeclarationSyntax, List<StructFieldData> allFields)
        {
            SyntaxToken identifier = structDeclarationSyntax.Identifier;
            var scriptName = GetStructPolymorphismNameFromStructInitializeData(identifier.Text);
            HeaderData headerData = GetHeader(structDeclarationSyntax, scriptName);
            
            SourceText sourceText = BuildPartialPropertyStruct(headerData, targetStructPointer, allFields);
            
            context.AddSource(scriptName, sourceText);
        }

        private List<StructFieldData> GetAllFieldsOfStruct(GeneratorExecutionContext context, StructDeclarationSyntax structDeclarationSyntax)
        {
            List<StructFieldData> result = new List<StructFieldData>();
            
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(structDeclarationSyntax.SyntaxTree);
            INamedTypeSymbol namedTypeSymbol = semanticModel.GetDeclaredSymbol(structDeclarationSyntax);

            List<IFieldSymbol> fieldSymbolList = namedTypeSymbol?.GetMembers().Where(symbol => symbol.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToList();

            if (fieldSymbolList != null)
            {
                foreach (var fieldSymbol in fieldSymbolList)
                {
                    result.Add(new StructFieldData()
                    {
                        fieldSymbol = fieldSymbol,
                        typeName = ConvertFieldTypeName(fieldSymbol.Type.TypeKind, fieldSymbol.Type.Name),
                        fieldName = MapFieldNameToProperty(fieldSymbol),
                    });
                }
            }

            return result;
        }
        
        private SourceText BuildPartialPropertyStruct(HeaderData headerData, string targetStructPointer, List<StructFieldData> allFields)
        {
            FileWriter fileWriter = new FileWriter();

            GenerateUsingDirectives(fileWriter, headerData);
            fileWriter.WriteLine("");
            
            bool useNameSpace = !string.IsNullOrEmpty(headerData.nameSpace);
            if (useNameSpace)
            {
                fileWriter.WriteLine($"namespace {headerData.nameSpace}");
                fileWriter.BeginScope();
            }
            
            fileWriter.WriteLine($"// Script Generated");
            fileWriter.WriteLine("[Serializable]");
            fileWriter.WriteLine($"public unsafe partial struct {headerData.scriptName}");
            fileWriter.BeginScope();

            string structPointerFieldName = $"{targetStructPointer.Substring(0, 1).ToLower()}{targetStructPointer.Substring(1)}";
            GenerateConstructorPointer(fileWriter, headerData, targetStructPointer, structPointerFieldName);
            fileWriter.WriteLine("");
            
            GenerateGetSetProperty(fileWriter, allFields, structPointerFieldName);
            
            fileWriter.EndScope();
            
            if (useNameSpace)
            {
                fileWriter.EndScope();
            }
            
            return SourceText.From(fileWriter.FileContents, Encoding.UTF8);
        }

        private void GenerateConstructorPointer(FileWriter fileWriter, HeaderData headerData, string targetStructPointer, string structPointerFieldName)
        {
            fileWriter.WriteLine($"public {targetStructPointer}* {structPointerFieldName};");
            fileWriter.WriteLine("");
            fileWriter.WriteLine($"public {headerData.scriptName}({targetStructPointer}* {structPointerFieldName})");
            fileWriter.BeginScope();
            fileWriter.WriteLine($"this.{structPointerFieldName} = {structPointerFieldName};");
            fileWriter.EndScope();
        }
        
        private void GenerateGetSetProperty(FileWriter fileWriter, List<StructFieldData> allFields, string structPointerFieldName)
        {
            if (allFields == null || allFields.Count == 0)
            {
                return;
            }

            var mergedFields = BuildMergedFieldsFromStructInitialized(allFields);
            if (mergedFields.Count > 0)
            {
                foreach (var mergedFieldPairData in mergedFields)
                {
                    var structFieldDataList = mergedFieldPairData.Value;
                    
                    for (int idx = 0; idx < structFieldDataList.Count; idx++)
                    {
                        var structFieldData = structFieldDataList[idx];
                        int numberParameter = idx / 4;
                        int indexInParameter = idx % 4;

                        if (structFieldData.fieldSymbol != null)
                        {
                            var typeKind = structFieldData.fieldSymbol.Type.TypeKind;
                            var typeName = typeKind == TypeKind.Enum ? structFieldData.fieldSymbol.Type.Name : structFieldData.typeName;
                            var fieldName = GetFieldNameByRule(typeKind, typeName, numberParameter, indexInParameter);
                    
                            // fileWriter.WriteLine($"// TypeKind: {structFieldData.fieldSymbol.Type.TypeKind}");
                            fileWriter.WriteLine($"public {typeName} {structFieldData.fieldName}");
                            fileWriter.BeginScope();
                    
                            if (typeKind == TypeKind.Enum)
                            {
                                fileWriter.WriteLine($"get => ({typeName}){structPointerFieldName}->{fieldName};");
                                fileWriter.WriteLine($"set => {structPointerFieldName}->{fieldName} = ({ParseFromEnumType()})value;");
                            }
                            else if (typeKind == TypeKind.Struct)
                            {
                                fileWriter.WriteLine($"get => {structPointerFieldName}->{fieldName};");
                                fileWriter.WriteLine($"set => {structPointerFieldName}->{fieldName} = value;");
                            }
                            else
                            {
                                fileWriter.WriteLine($"// Missing unknown field type: {structFieldData.typeName} - {structFieldData.fieldName}");
                                fileWriter.WriteLine("");
                            }
                    
                            fileWriter.EndScope();
                            fileWriter.WriteLine("");
                        }
                    }
                }
            }
        }
        
        private Dictionary<string, List<StructFieldData>> BuildMergedFieldsFromStructInitialized(List<StructFieldData> allFields)
        {
            Dictionary<string, List<StructFieldData>> result = new Dictionary<string, List<StructFieldData>>();

            foreach (var structFieldData in allFields)
            {
                if (structFieldData.fieldSymbol == null)
                {
                    continue;
                }

                string typeName = structFieldData.typeName;
                
                if (!result.ContainsKey(typeName))
                {
                    result.Add(typeName, new List<StructFieldData>());
                }
                    
                result[typeName].Add(structFieldData);
            }

            return result;
        }
        
        #endregion

        #region Generate Initialize Data From Initialize Data
        
        private void GenerateStructInitializeData(GeneratorExecutionContext context, string targetStructPointer, StructDeclarationSyntax structDeclarationSyntax, List<StructFieldData> allFields)
        {
            SyntaxToken identifier = structDeclarationSyntax.Identifier;
            var scriptName = identifier.Text;
            HeaderData headerData = GetHeader(structDeclarationSyntax, scriptName);

            SourceText sourceText = BuildPartialInitializeDataStruct(headerData, targetStructPointer, allFields);
            
            context.AddSource(scriptName, sourceText);
        }

        private SourceText BuildPartialInitializeDataStruct(HeaderData headerData, string targetStructPointer, List<StructFieldData> allFields)
        {
            FileWriter fileWriter = new FileWriter();

            GenerateUsingDirectives(fileWriter, headerData);
            fileWriter.WriteLine("");
            
            bool useNameSpace = !string.IsNullOrEmpty(headerData.nameSpace);
            if (useNameSpace)
            {
                fileWriter.WriteLine($"namespace {headerData.nameSpace}");
                fileWriter.BeginScope();
            }
            
            fileWriter.WriteLine($"// Script Generated");
            fileWriter.WriteLine($"public unsafe partial struct {headerData.scriptName}");
            fileWriter.BeginScope();
            
            GenerateMethodInitializeData(fileWriter, targetStructPointer, allFields, headerData);
            
            fileWriter.EndScope();
            
            if (useNameSpace)
            {
                fileWriter.EndScope();
            }
            
            return SourceText.From(fileWriter.FileContents, Encoding.UTF8);
        }

        private void GenerateMethodInitializeData(FileWriter fileWriter, string targetStructPointer, List<StructFieldData> allFields, HeaderData headerData)
        {
            fileWriter.WriteLine($"public {targetStructPointer} InitializeData()");
            fileWriter.BeginScope();
            
            fileWriter.WriteLine($"var data = new {targetStructPointer}();");
            fileWriter.WriteLine($"data.currentUnitAbilityPolymorphismType = UnitAbilityPolymorphismType.{GetStructPolymorphismNameFromStructInitializeData(headerData.scriptName)};");
            var mergedFields = BuildMergedFieldsFromStructInitialized(allFields);
            if (mergedFields.Count > 0)
            {
                foreach (var mergedFieldPairData in mergedFields)
                {
                    var structFieldDataList = mergedFieldPairData.Value;

                    for (int idx = 0; idx < structFieldDataList.Count; idx++)
                    {
                        var structFieldData = structFieldDataList[idx];
                        int numberParameter = idx / 4;
                        int indexInParameter = idx % 4;
                        
                        if (structFieldData.fieldSymbol != null)
                        {
                            var typeKind = structFieldData.fieldSymbol.Type.TypeKind;
                            var fieldName = GetFieldNameByRule(structFieldData.fieldSymbol.Type.TypeKind, structFieldData.typeName, numberParameter, indexInParameter);
                            
                            if (typeKind == TypeKind.Enum)
                            {
                                fileWriter.WriteLine($"data.{fieldName} = ({ParseFromEnumType()}){structFieldData.fieldName};");
                            }
                            else if (typeKind == TypeKind.Struct)
                            {
                                fileWriter.WriteLine($"data.{fieldName} = {structFieldData.fieldName};");
                            }
                        }
                    }
                }
            }
            
            fileWriter.WriteLine($"return data;");
            fileWriter.EndScope();
        }
        
        #endregion

        private string ParseFromEnumType()
        {
            return "int";
        }
        
        private string GetFieldNameByRule(TypeKind typeKind, string typeName, int fieldNumber, int indexInField)
        {
            string fieldName = "";
            if (typeKind == TypeKind.Enum)
            {
                fieldName = "int";
                return $"{fieldName}Parameter_{fieldNumber}[{indexInField}]";
            }
            else if (typeKind == TypeKind.Struct)
            {
                fieldName = $"{typeName.Substring(0, 1).ToLower()}{typeName.Substring(1)}";
                return $"{fieldName}Parameter_{fieldNumber}[{indexInField}]";
            }

            return "Get name parameter fail !!!";
        }

        private string CreateMergedFieldNameByRule(IFieldSymbol fieldSymbol, string typeName, int fieldNumber)
        {
            string fieldName = "";
            if (fieldSymbol.Type.TypeKind == TypeKind.Enum)
            {
                fieldName = "int";
                return $"{fieldName}Parameter_{fieldNumber}";
            }
            else if (fieldSymbol.Type.TypeKind == TypeKind.Struct)
            {
                fieldName = $"{typeName.Substring(0, 1).ToLower()}{typeName.Substring(1)}";
                return $"{fieldName}Parameter_{fieldNumber}";
            }

            return "Get name parameter fail !!!";
        }

        private string CreateMergedTypeNameByRule(string typeName)
        {
            return $"{typeName}4";
        }

        private string ConvertFieldTypeName(TypeKind typeKind, string typeName)
        {
            if (typeKind == TypeKind.Enum)
            {
                return "int";
            }
            
            switch (typeName)
            {
                case "Single":
                    return "float";
                case "Double":
                    return "float";
                case "Int32":
                    return "int";
                case "int32":
                    return "int";
                case "Boolean":
                    return "bool";
                default:
                    break;
            }

            return $"{typeName}NotConvert";
        }
        
        #region Generate Struct Component From Interface
        
        private void GenerateStructComponentFromInterface(GeneratorExecutionContext context, AbilitySyntaxReceiver syntaxReceiver, InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            var identifier = interfaceDeclarationSyntax.Identifier;
            var scriptName = identifier.Text.Substring(1);
            HeaderData interfaceHeaderData = GetHeader(interfaceDeclarationSyntax, scriptName);
            List<ISymbol> allMemberSymbols = Utils.GetAllMemberSymbols(context, interfaceDeclarationSyntax);
            
            List<StructData> structDataListToBuildMergedField = BuildStructsDataToBuildMergedField(context, syntaxReceiver, interfaceHeaderData, interfaceDeclarationSyntax);
            
            List<StructData> structDataList = BuildStructsData(context, syntaxReceiver, interfaceHeaderData, interfaceDeclarationSyntax);
            
            if (structDataList == null || structDataList.Count == 0 || structDataListToBuildMergedField == null || structDataListToBuildMergedField.Count == 0)
            {
                return;
            }
            
            List<MergedFieldData> mergedFieldData = BuildMergedFieldsByRuleName(structDataListToBuildMergedField);
            SourceText mergedStruct = GenerateMergedStruct(interfaceHeaderData, allMemberSymbols, mergedFieldData, structDataList);

            context.AddSource(scriptName, mergedStruct);
            
            // foreach (var structData in structDataList)
            // {
            //     GeneratePartialStruct(context, headerData.usingDirectives, headerData.scriptName, structData, mergedFieldData);  
            // }
            
            // DebugInEditorUnity(context, $"headerData.scriptName: {headerData.scriptName} \n headerData.interfaceName: {headerData.interfaceName}");
        }

        private List<StructData> BuildStructsDataToBuildMergedField(GeneratorExecutionContext context, AbilitySyntaxReceiver syntaxReceiver, HeaderData headerData, InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            List<StructData> structDataList = new List<StructData>();

            foreach (var initializeDataStructPair in syntaxReceiver.initializeDataStructs)
            {
                var componentNameToGenerate = initializeDataStructPair.Key;
                SyntaxToken interfaceIdentifier = interfaceDeclarationSyntax.Identifier;
                var interfaceName = interfaceIdentifier.Text;
                if (componentNameToGenerate != interfaceName.Substring(1))
                {
                    continue;
                }

                foreach (var structDeclarationSyntax in initializeDataStructPair.Value)
                {
                    SyntaxToken structIdentifier = structDeclarationSyntax.Identifier;
                    if (structIdentifier.Text.Equals(headerData.scriptName))
                    {
                        continue;
                    }
                    
                    StructData structData = new StructData
                    {
                        nameSpace = Utils.GetNamespace(structDeclarationSyntax),
                        structName = structDeclarationSyntax.Identifier.ToString()
                    };
                    foreach (var usingDirectiveSyntax in interfaceDeclarationSyntax.SyntaxTree.GetCompilationUnitRoot().Usings)
                    {
                        TryAddUsing(headerData.usingDirectives, usingDirectiveSyntax.Name.ToString());
                    }

                    SemanticModel semanticModel = context.Compilation.GetSemanticModel(structDeclarationSyntax.SyntaxTree);
                    INamedTypeSymbol namedTypeSymbol = semanticModel.GetDeclaredSymbol(structDeclarationSyntax);

                    List<IFieldSymbol> fieldSymbolList = namedTypeSymbol?.GetMembers().Where(it => it.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToList();
                    if (fieldSymbolList != null)
                    {
                        foreach (var fieldSymbol in fieldSymbolList)
                        {
                            string typeName = ConvertFieldTypeName(fieldSymbol.Type.TypeKind, fieldSymbol.Type.Name);
                            
                            if (!structData.fieldsByTypeNameDict.ContainsKey(typeName))
                            {
                                structData.fieldsByTypeNameDict.Add(typeName, new List<StructFieldData>());
                            }
                            
                            structData.fieldsByTypeNameDict[typeName].Add(new StructFieldData()
                            {
                                fieldSymbol = fieldSymbol,
                                typeName = typeName,
                                fieldName = MapFieldNameToProperty(fieldSymbol)
                            });
                        }
                    }
                    
                    structDataList.Add(structData);
                }
            }
            
            return structDataList;
        }
        
        private List<StructData> BuildStructsData(GeneratorExecutionContext context, AbilitySyntaxReceiver syntaxReceiver, HeaderData headerData, InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            List<StructData> structDataList = new List<StructData>();
            foreach (StructDeclarationSyntax structDeclarationSyntax in syntaxReceiver.polymorphicStructs)
            {
                StructDeclarationSyntax typeSyntax = structDeclarationSyntax;
                SyntaxToken identifier1 = interfaceDeclarationSyntax.Identifier;
                string interfaceName = identifier1.Text;
                if (Utils.ImplementsInterface(typeSyntax, interfaceName))
                {
                    SyntaxToken identifier2 = structDeclarationSyntax.Identifier;
                    if (!identifier2.Text.Equals(headerData.scriptName))
                    {
                        StructData structData = new StructData
                        {
                            nameSpace = Utils.GetNamespace(structDeclarationSyntax),
                            structName = structDeclarationSyntax.Identifier.ToString()
                        };
                        foreach (var usingDirectiveSyntax in interfaceDeclarationSyntax.SyntaxTree.GetCompilationUnitRoot().Usings)
                        {
                            TryAddUsing(headerData.usingDirectives, usingDirectiveSyntax.Name.ToString());
                        }
        
                        // SemanticModel semanticModel = context.Compilation.GetSemanticModel(interfaceDeclarationSyntax.SyntaxTree);
                        // INamedTypeSymbol namedTypeSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);
        
                        // List<IFieldSymbol> fieldSymbolList = namedTypeSymbol?.GetMembers().Where(it => it.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToList();
                        // if (fieldSymbolList != null)
                        // {
                        //     foreach (var fieldSymbol in fieldSymbolList)
                        //     {
                        //         structData.fields.Add(new StructFieldData()
                        //         {
                        //             fieldSymbol = fieldSymbol,
                        //             typeName = fieldSymbol.Type.Name,
                        //             fieldName = MapFieldNameToProperty(fieldSymbol)
                        //         });
                        //     }
                        // }
        
                        structDataList.Add(structData);
                    }
                }
            }
        
            return structDataList;
        }

        private List<MergedFieldData> BuildMergedFieldsByRuleName(List<StructData> structComponentDataList)
        {
            List<MergedFieldData> mergedFieldDataList = new List<MergedFieldData>();
            
            Dictionary<string, List<MergedFieldData>> mergedFieldDataByTypeNameDict = new Dictionary<string, List<MergedFieldData>>();
            
            foreach (var structData in structComponentDataList)
            {
                foreach (var fieldByTypeNamePair in structData.fieldsByTypeNameDict)
                {
                    string typeName = fieldByTypeNamePair.Key;
                    List<StructFieldData> fieldDataByTypeList = fieldByTypeNamePair.Value;
                    var numberField = Math.Ceiling(fieldDataByTypeList.Count / 4f);
                    
                    StructFieldData fieldData = fieldDataByTypeList[0];
                    if (fieldData.fieldSymbol == null)
                    {
                        fieldData = fieldDataByTypeList.Find(x => x.fieldSymbol != null);
                        if (fieldData == null)
                        {
                            continue;
                        }
                    }
                    
                    if (!mergedFieldDataByTypeNameDict.ContainsKey(typeName))
                    {
                        mergedFieldDataByTypeNameDict.Add(typeName, new List<MergedFieldData>());
                        
                        for (int idx = 0; idx < numberField; idx++)
                        {
                            string fieldName = CreateMergedFieldNameByRule(fieldData.fieldSymbol, typeName, idx);
                            mergedFieldDataByTypeNameDict[typeName].Add(new MergedFieldData()
                            {
                                typeName = CreateMergedTypeNameByRule(typeName),
                                fieldName = fieldName,
                            });
                        }
                    }
                    else
                    {
                        int numberMergedField = mergedFieldDataByTypeNameDict[typeName].Count;
                        if (numberMergedField < numberField)
                        {
                            for (int idx = numberMergedField; idx < numberField; idx++)
                            {
                                string fieldName = CreateMergedFieldNameByRule(fieldData.fieldSymbol, typeName, idx);
                                mergedFieldDataByTypeNameDict[typeName].Add(new MergedFieldData()
                                {
                                    typeName = CreateMergedTypeNameByRule(typeName),
                                    fieldName = fieldName,
                                });
                            }
                        }
                    }
                }
            }

            if (mergedFieldDataByTypeNameDict.Count > 0)
            {
                foreach (var pair in mergedFieldDataByTypeNameDict)
                {
                    mergedFieldDataList.AddRange(pair.Value);
                }
            }
            
            return mergedFieldDataList;
        }
        
        // private List<MergedFieldData> BuildMergedFields(List<StructData> structComponentDataList)
        // {
        //     List<MergedFieldData> mergedFieldDataList = new List<MergedFieldData>();
        //     List<int> intList = new List<int>();
        //     foreach (var structData in structComponentDataList)
        //     {
        //         intList.Clear();
        //         foreach (var field in structData.fields)
        //         {
        //             int index2 = -1;
        //             for (int index3 = 0; index3 < mergedFieldDataList.Count; ++index3)
        //             {
        //                 if (!intList.Contains(index3) &&
        //                     string.Equals(field.typeName, mergedFieldDataList[index3].typeName))
        //                 {
        //                     index2 = index3;
        //                     break;
        //                 }
        //             }
        //
        //             if (index2 < 0)
        //             {
        //                 int count = mergedFieldDataList.Count;
        //                 MergedFieldData mergedFieldData = new MergedFieldData
        //                 {
        //                     typeName = field.typeName,
        //                     fieldName = $"{field.typeName}_{mergedFieldDataList.Count}"
        //                 };
        //                 mergedFieldData.fieldNameForStructName.Add(structData.structName, field.fieldName);
        //                 mergedFieldDataList.Add(mergedFieldData);
        //                 field.mergedFieldName = mergedFieldData.fieldName;
        //                 intList.Add(count);
        //             }
        //             else
        //             {
        //                 MergedFieldData mergedFieldData = mergedFieldDataList[index2];
        //                 mergedFieldData.fieldNameForStructName.Add(structData.structName, field.fieldName);
        //                 field.mergedFieldName = mergedFieldData.fieldName;
        //                 intList.Add(index2);
        //             }
        //         }
        //     }
        //
        //     return mergedFieldDataList;
        // }
        
        private SourceText GenerateMergedStruct(HeaderData headerData, List<ISymbol> allMemberSymbols, List<MergedFieldData> mergedFieldData,
            List<StructData> structDataList)
        {
            FileWriter fileWriter = new FileWriter();
            GenerateUsingDirectives(fileWriter, headerData);
            fileWriter.WriteLine("");
            bool useNameSpace = !string.IsNullOrEmpty(headerData.nameSpace);
            if (useNameSpace)
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
            
            if (useNameSpace)
            {
                fileWriter.EndScope();
            }
            
            return SourceText.From(fileWriter.FileContents, Encoding.UTF8);
        }
        
        private void GenerateStructHeader(FileWriter structWriter, HeaderData headerData)
        {
            structWriter.WriteLine($"// Script Generated");
            structWriter.WriteLine("[Serializable]");
            structWriter.WriteLine($"public unsafe partial struct {headerData.scriptName} : {headerData.interfaceName}");
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
            
            if (mergedFields == null || mergedFields.Count == 0)
            {
                structWriter.WriteLine("");
                structWriter.WriteLine($"// Merged Fields Null !!!");
                structWriter.WriteLine("");
            }

            if (mergedFields != null)
            {
                foreach (var mergedField in mergedFields)
                {
                    structWriter.WriteLine($"public {mergedField.typeName} {mergedField.fieldName};");
                }
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
                structWriter.WriteLine("");
            }
        }
        
        #endregion

        private static HeaderData GetHeader(TypeDeclarationSyntax typeDeclarationSyntax, string scriptName)
        {
            var newUsing = Utils.GetNamespace(typeDeclarationSyntax);
            var allUsing = new List<string>();
            TryAddUsing(allUsing, "System");
            if (!string.IsNullOrEmpty(newUsing))
            {
                TryAddUsing(allUsing, newUsing);
            }

            foreach (var usingDirectiveSyntax in typeDeclarationSyntax.SyntaxTree.GetCompilationUnitRoot().Usings)
            {
                TryAddUsing(allUsing, usingDirectiveSyntax.Name.ToString());
            }

            return new HeaderData()
            {
                interfaceName = typeDeclarationSyntax.Identifier.ToString(),
                scriptName = scriptName,
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

        private static string MapFieldNameToProperty(IFieldSymbol fieldSymbol) =>
            fieldSymbol.AssociatedSymbol is IPropertySymbol associatedSymbol ? associatedSymbol.Name : fieldSymbol.Name;
        
        private static void GenerateUsingDirectives(FileWriter mergedStructWriter, HeaderData headerData)
        {
            foreach (string usingDirective in headerData.usingDirectives)
            {
                mergedStructWriter.WriteLine($"using {usingDirective};");
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
            structWriter.WriteLine($"fixed({headerData.scriptName}* ptr = &this)");
            structWriter.BeginScope();
            structWriter.WriteLine("switch(currentUnitAbilityPolymorphismType)");
            structWriter.BeginScope();
            bool returnsVoid = methodSymbol.ReturnsVoid;
            foreach (var structData in structDataList)
            {
                structWriter.WriteLine($"case UnitAbilityPolymorphismType.{structData.structName}:");
                structWriter.BeginScope();
                string str = $"instance_{structData.structName}";
                structWriter.WriteLine($"{structData.structName} {str} = new {structData.structName}(ptr);");
                structWriter.WriteLine((returnsVoid ? "" : "var r = ") + $"{str}.{callClause};");
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
        
        private void DebugInEditorUnity(GeneratorExecutionContext context, string str)
        {
            DiagnosticDescriptor descriptor = new DiagnosticDescriptor("Debug", "", $"\n{str}", "",
                DiagnosticSeverity.Error, true);
            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, DiagnosticSeverity.Error));
        }
    }
}