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
        public IFieldSymbol? fieldSymbol;
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

            if (syntaxReceiver.initializeDataStructs.Count > 0)
            {
                foreach (var pair in syntaxReceiver.initializeDataStructs)
                {
                    var structDeclarationSyntax = pair.Value;
                    string targetStructPointer = pair.Key;
                    List<StructFieldData> allFields = GetAllFields(context, structDeclarationSyntax);

                    GenerateStructPolymorphismFromInitializeData(context, targetStructPointer, structDeclarationSyntax, allFields);
                    GenerateStructInitializeData(context, targetStructPointer, structDeclarationSyntax, allFields);
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
        
        private void GenerateStructPolymorphismFromInitializeData(GeneratorExecutionContext context, string targetStructPointer, StructDeclarationSyntax structDeclarationSyntax, List<StructFieldData> allFields)
        {
            SyntaxToken identifier = structDeclarationSyntax.Identifier;
            var scriptName = identifier.Text.Substring(0, identifier.Text.Length - 14);
            HeaderData headerData = GetHeader(structDeclarationSyntax, scriptName);
            
            SourceText sourceText = BuildPartialPropertyStruct(headerData, targetStructPointer, allFields);
            
            context.AddSource(scriptName, sourceText);
        }

        private List<StructFieldData> GetAllFields(GeneratorExecutionContext context, StructDeclarationSyntax structDeclarationSyntax)
        {
            List<StructFieldData> result = new List<StructFieldData>();
            
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(structDeclarationSyntax.SyntaxTree);
            INamedTypeSymbol namedTypeSymbol = semanticModel.GetDeclaredSymbol(structDeclarationSyntax);

            List<IFieldSymbol> fieldSymbolList = namedTypeSymbol?.GetMembers().Where(symbol => symbol.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToList();

            if (fieldSymbolList != null && fieldSymbolList.Count > 0)
            {
                foreach (var fieldSymbol in fieldSymbolList)
                {
                    result.Add(new StructFieldData()
                    {
                        fieldSymbol = fieldSymbol,
                        typeName = fieldSymbol.Type.Name,
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

            for (int idx = 0; idx < allFields.Count; idx++)
            {
                var structFieldData = allFields[idx];
                int numberParameter = idx / 4;
                int indexInParameter = idx % 4;

                if (structFieldData.fieldSymbol != null)
                {
                    fileWriter.WriteLine($"// TypeKind: {structFieldData.fieldSymbol.Type.TypeKind}");
                    var nameParameter = GetNameField(structFieldData.fieldSymbol, structFieldData.typeName, numberParameter, indexInParameter);
                    
                    fileWriter.WriteLine($"public {structFieldData.typeName} {structFieldData.fieldName}");
                    fileWriter.BeginScope();
                    
                    if (structFieldData.fieldSymbol.Type.TypeKind == TypeKind.Enum)
                    {
                        fileWriter.WriteLine($"get => ({structFieldData.typeName}){structPointerFieldName}->{nameParameter};");
                        fileWriter.WriteLine($"set => {structPointerFieldName}->{nameParameter} = (int)value;");
                    }
                    else if (structFieldData.fieldSymbol.Type.TypeKind == TypeKind.Struct)
                    {
                        fileWriter.WriteLine($"get => {structPointerFieldName}->{nameParameter};");
                        fileWriter.WriteLine($"set => {structPointerFieldName}->{nameParameter} = value;");
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
        
        #endregion

        #region Generate Sync Data From Initialize Data
        
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
            
            fileWriter.WriteLine($"public unsafe partial struct {headerData.scriptName}");
            fileWriter.BeginScope();
            
            GenerateMethodInitializeData(fileWriter, targetStructPointer, allFields);
            
            fileWriter.EndScope();
            
            if (useNameSpace)
            {
                fileWriter.EndScope();
            }
            
            return SourceText.From(fileWriter.FileContents, Encoding.UTF8);
        }

        private void GenerateMethodInitializeData(FileWriter fileWriter, string targetStructPointer, List<StructFieldData> allFields)
        {
            fileWriter.WriteLine($"public {targetStructPointer} InitializeData()");
            fileWriter.BeginScope();
            
            fileWriter.WriteLine($"var data = new {targetStructPointer}();");

            for (int idx = 0; idx < allFields.Count; idx++)
            {
                var structFieldData = allFields[idx];
                int numberParameter = idx / 4;
                int indexInParameter = idx % 4;

                if (structFieldData.fieldSymbol != null)
                {
                    var nameParameter = GetNameField(structFieldData.fieldSymbol, structFieldData.typeName, numberParameter, indexInParameter);
                    if (structFieldData.fieldSymbol.Type.TypeKind == TypeKind.Enum)
                    {
                        fileWriter.WriteLine($"data.{nameParameter} = (int){structFieldData.fieldName};");
                    }
                    else if (structFieldData.fieldSymbol.Type.TypeKind == TypeKind.Struct)
                    {
                        fileWriter.WriteLine($"data.{nameParameter} = {structFieldData.fieldName};");
                    }
                }
            }
            
            fileWriter.WriteLine($"return data;");
            fileWriter.EndScope();
        }
        
        #endregion

        private Dictionary<string, List<string>> BuildMergedNameFields(List<StructFieldData> allFields)
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

            foreach (var structFieldData in allFields)
            {
                if (structFieldData.fieldSymbol == null)
                {
                    continue;
                }

                string typeName = "typeName_Null";
                if (structFieldData.fieldSymbol.Type.TypeKind == TypeKind.Enum)
                {
                    typeName = "int32";
                }
                else if (structFieldData.fieldSymbol.Type.TypeKind == TypeKind.Struct)
                {
                    typeName = structFieldData.typeName;
                }
                
                if (!result.ContainsKey(typeName))
                {
                    result.Add(typeName, new List<string>());
                }
                    
                result[typeName].Add(structFieldData.fieldName);
            }

            return result;
        }
        
        private string GetNameField(IFieldSymbol fieldSymbol, string typeName, int numberParameter, int indexInParameter)
        {
            string nameParameter = "";
            if (fieldSymbol.Type.TypeKind == TypeKind.Enum)
            {
                nameParameter = "int32";
                return $"{nameParameter}Parameter_{numberParameter}[{indexInParameter}]";
            }
            else if (fieldSymbol.Type.TypeKind == TypeKind.Struct)
            {
                nameParameter = $"{typeName.Substring(0, 1).ToLower()}{typeName.Substring(1)}";
                return $"{nameParameter}Parameter_{numberParameter}[{indexInParameter}]";
            }

            return "Get name parameter fail !!!";
        }
        
        private void GenerateStructComponentFromInterface(GeneratorExecutionContext context, AbilitySyntaxReceiver syntaxReceiver, InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            var identifier = interfaceDeclarationSyntax.Identifier;
            var scriptName = identifier.Text.Substring(1);
            HeaderData interfaceHeaderData = GetHeader(interfaceDeclarationSyntax, scriptName);
            List<ISymbol> allMemberSymbols = Utils.GetAllMemberSymbols(context, interfaceDeclarationSyntax);
            
            List<StructData> structDataList = BuildStructsData(context, syntaxReceiver, interfaceHeaderData, interfaceDeclarationSyntax);
            
            if (structDataList == null || structDataList.Count == 0)
            {
                return;
            }
            
            List<MergedFieldData> mergedFieldData = BuildMergedFields(structDataList);
            SourceText mergedStruct = GenerateMergedStruct(interfaceHeaderData, allMemberSymbols, mergedFieldData, structDataList);

            context.AddSource(scriptName, mergedStruct);
            
            // foreach (var structData in structDataList)
            // {
            //     GeneratePartialStruct(context, headerData.usingDirectives, headerData.scriptName, structData, mergedFieldData);  
            // }
            
            // DebugInEditorUnity(context, $"headerData.scriptName: {headerData.scriptName} \n headerData.interfaceName: {headerData.interfaceName}");
        }

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

        private List<StructData> BuildStructsDataFromInitializeData(GeneratorExecutionContext context, AbilitySyntaxReceiver syntaxReceiver, HeaderData headerData, InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            List<StructData> structDataList = new List<StructData>();
            foreach (var structDeclarationSyntax in syntaxReceiver.initializeDataStructs)
            {
                SyntaxToken identifier1 = interfaceDeclarationSyntax.Identifier;
                string interfaceName = identifier1.Text;
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

                        SemanticModel semanticModel = context.Compilation.GetSemanticModel(interfaceDeclarationSyntax.SyntaxTree);
                        INamedTypeSymbol namedTypeSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);

                        List<IFieldSymbol> fieldSymbolList = namedTypeSymbol?.GetMembers().Where(it => it.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToList();
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
            bool useNameSpace = !string.IsNullOrEmpty(headerData.nameSpace) ? true : false;
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