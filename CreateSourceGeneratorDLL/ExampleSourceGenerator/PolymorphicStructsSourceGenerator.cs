// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Microsoft.CodeAnalysis.Text;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
//
//
// #nullable enable
// namespace PolymorphicStructsSourceGenerators
// {
//     [Generator]
//     public class PolymorphicStructsSourceGenerator : ISourceGenerator
//     {
//         private const
// #nullable disable
//             string typeEnumName = "TypeId";
//
//         private const string typeEnumVarName = "CurrentTypeId";
//
//         public void Initialize(GeneratorInitializationContext context) =>
//             ((GeneratorInitializationContext) ref context)
//         .RegisterForSyntaxNotifications(PolymorphicStructsSourceGenerator.\u003C\u003Ec.\u003C\u003E9__5_0 ?? (
//         PolymorphicStructsSourceGenerator.\u003C\u003Ec.\u003C\u003E9__5_0 = new
//             SyntaxReceiverCreator((object) PolymorphicStructsSourceGenerator.\u003C\u003Ec.\u003C\u003E9, __methodptr(
//             \u003CInitialize\u003Eb__5_0))));
//
//         public void Execute(GeneratorExecutionContext context)
//         {
//             Console.WriteLine(
//                     "PolymorphicStructs sourceGenerator execute  on assembly " +
//                     ((GeneratorExecutionContext) ref context)
//                 .Compilation.AssemblyName);
//             try
//             {
//                 PolymorphicStructSyntaxReceiver syntaxReceiver =
//                     (PolymorphicStructSyntaxReceiver)((GeneratorExecutionContext) ref context ).SyntaxReceiver;
//                 foreach (InterfaceDeclarationSyntax polymorphicInterface in
//                          (IEnumerable<InterfaceDeclarationSyntax>)syntaxReceiver.PolymorphicInterfaces)
//                     this.GenerateInterfacesCode(context, syntaxReceiver, polymorphicInterface);
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine("SourceGenerators ERROR: " + ex.Message);
//                 DiagnosticDescriptor diagnosticDescriptor = new DiagnosticDescriptor("PolymorphicStructsError",
//                     "PolymorphicStructsError", "Generation failed with " + ex.Message, "PolymorphicStructsError",
//                     (DiagnosticSeverity)3, true, (string)null, (string)null, Array.Empty<string>());
//                 ((GeneratorExecutionContext) ref context).ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor,
//                     Location.None, new object[1]
//                     {
//                         (object)(DiagnosticSeverity)3
//                     }));
//             }
//         }
//
//         private void GenerateInterfacesCode(
//             GeneratorExecutionContext context,
//             PolymorphicStructSyntaxReceiver systemReceiver,
//             InterfaceDeclarationSyntax polymorphicInterface)
//         {
//             PolymorphicStructsSourceGenerator.StructDef structDef =
//                 PolymorphicStructsSourceGenerator.CollectStructHeaderDef(context, polymorphicInterface);
//             List<ISymbol> allMemberSymbols = SourceGenUtils.GetAllMemberSymbols(context, polymorphicInterface);
//             List<PolymorphicStructsSourceGenerator.IndividialStructData> individialStructDataList =
//                 PolymorphicStructsSourceGenerator.BuildIndividualStructsData(context, systemReceiver, structDef,
//                     polymorphicInterface);
//             if (individialStructDataList.Count == 0)
//                 return;
//             List<PolymorphicStructsSourceGenerator.MergedFieldData> mergedFieldDataList =
//                 PolymorphicStructsSourceGenerator.BuildMergedFields(individialStructDataList);
//             SourceText mergedStruct = this.GenerateMergedStruct(structDef, allMemberSymbols, mergedFieldDataList,
//                 individialStructDataList);
//             Console.WriteLine("Generating PolyInterface " + structDef.MergedStructName);
//             ((GeneratorExecutionContext) ref context).AddSource(structDef.MergedStructName, mergedStruct);
//             foreach (PolymorphicStructsSourceGenerator.IndividialStructData structData in individialStructDataList)
//                 PolymorphicStructsSourceGenerator.GeneratePartialIndividualStruct(context, structDef.UsingDirectives,
//                     structDef.MergedStructName, structData, mergedFieldDataList);
//         }
//
//         private SourceText GenerateMergedStruct(
//             PolymorphicStructsSourceGenerator.StructDef structDef,
//             List<ISymbol> allMemberSymbols,
//             List<PolymorphicStructsSourceGenerator.MergedFieldData> mergedFieldDatas,
//             List<PolymorphicStructsSourceGenerator.IndividialStructData> individialStructDatas)
//         {
//             FileWriter fileWriter = new FileWriter();
//             PolymorphicStructsSourceGenerator.GenerateUsingDirectives(fileWriter, structDef);
//             fileWriter.WriteLine("");
//             int num = !string.IsNullOrEmpty(structDef.Namespace) ? 1 : 0;
//             if (num != 0)
//             {
//                 fileWriter.WriteLine("namespace " + structDef.Namespace);
//                 fileWriter.BeginScope();
//             }
//
//             this.GenerateStructHeader(fileWriter, structDef);
//             fileWriter.BeginScope();
//             this.GenerateTypeEnum(fileWriter, individialStructDatas);
//             fileWriter.WriteLine("");
//             this.GenerateFields(fileWriter, mergedFieldDatas);
//             fileWriter.WriteLine("");
//             this.GenerateMethods(fileWriter, structDef, allMemberSymbols, individialStructDatas);
//             fileWriter.WriteLine("");
//             fileWriter.EndScope();
//             if (num != 0)
//                 fileWriter.EndScope();
//             return SourceText.From(fileWriter.FileContents, Encoding.UTF8, (SourceHashAlgorithm)1);
//         }
//
//         private void GenerateMethods(
//             FileWriter structWriter,
//             PolymorphicStructsSourceGenerator.StructDef structDef,
//             List<ISymbol> allMemberSymbols,
//             List<PolymorphicStructsSourceGenerator.IndividialStructData> individialStructDatas)
//         {
//             foreach (ISymbol allMemberSymbol in allMemberSymbols)
//             {
//                 if (allMemberSymbol is IMethodSymbol methodSymbol)
//                 {
//                     string str1 = methodSymbol.ReturnsVoid
//                         ? "void"
//                         : ((ISymbol)methodSymbol.ReturnType).ToDisplayString((SymbolDisplayFormat)null);
//                     string str2 = string.Join(", ",
//                         ImmutableArrayExtensions.Select<IParameterSymbol, string>(methodSymbol.Parameters,
//                             (Func<IParameterSymbol, string>)(it => string.Format("{0}{1} {2}",
//                                 (object)this.MapRefKind(it.RefKind), (object)it.Type, (object)((ISymbol)it).Name))));
//                     string str3 = string.Join(", ",
//                         ImmutableArrayExtensions.Select<IParameterSymbol, string>(methodSymbol.Parameters,
//                             (Func<IParameterSymbol, string>)(it => this.MapRefKind(it.RefKind) + ((ISymbol)it).Name)));
//                     structWriter.WriteLine("public " + str1 + " " + ((ISymbol)methodSymbol).Name + "(" + str2 + ")");
//                     this.GenerateMethodBody(structWriter, structDef, methodSymbol, individialStructDatas,
//                         ((ISymbol)methodSymbol).Name + "(" + str3 + ")");
//                 }
//                 else if (allMemberSymbol is IPropertySymbol ipropertySymbol)
//                 {
//                     structWriter.WriteLine(string.Format("public {0} {1}", (object)ipropertySymbol.Type,
//                         (object)((ISymbol)ipropertySymbol).Name));
//                     structWriter.BeginScope();
//                     if (ipropertySymbol.GetMethod != null)
//                         this.GeneratePropertyGetMethod(structWriter, structDef, ipropertySymbol.GetMethod,
//                             individialStructDatas, ((ISymbol)ipropertySymbol).Name);
//                     if (ipropertySymbol.SetMethod != null)
//                         this.GeneratePropertySetMethod(structWriter, structDef, ipropertySymbol.SetMethod,
//                             individialStructDatas, ((ISymbol)ipropertySymbol).Name);
//                     structWriter.EndScope();
//                 }
//             }
//         }
//
//         private void GeneratePropertyGetMethod(
//             FileWriter structWriter,
//             PolymorphicStructsSourceGenerator.StructDef structDef,
//             IMethodSymbol methodSymbol,
//             List<PolymorphicStructsSourceGenerator.IndividialStructData> structDatas,
//             string propertyName)
//         {
//             structWriter.WriteLine("get");
//             this.GenerateMethodBody(structWriter, structDef, methodSymbol, structDatas, propertyName ?? "");
//         }
//
//         private void GeneratePropertySetMethod(
//             FileWriter structWriter,
//             PolymorphicStructsSourceGenerator.StructDef structDef,
//             IMethodSymbol methodSymbol,
//             List<PolymorphicStructsSourceGenerator.IndividialStructData> structDatas,
//             string propertyName)
//         {
//             structWriter.WriteLine("set");
//             this.GenerateMethodBody(structWriter, structDef, methodSymbol, structDatas, propertyName + " = value");
//         }
//
//         private void GenerateMethodBody(
//             FileWriter structWriter,
//             PolymorphicStructsSourceGenerator.StructDef structDef,
//             IMethodSymbol methodSymbol,
//             List<PolymorphicStructsSourceGenerator.IndividialStructData> structDatas,
//             string callClause)
//         {
//             structWriter.BeginScope();
//             structWriter.WriteLine("switch(CurrentTypeId)");
//             structWriter.BeginScope();
//             bool returnsVoid = methodSymbol.ReturnsVoid;
//             foreach (PolymorphicStructsSourceGenerator.IndividialStructData structData in structDatas)
//             {
//                 structWriter.WriteLine("case TypeId." + structData.StructName + ":");
//                 structWriter.BeginScope();
//                 string str = "instance_" + structData.StructName;
//                 structWriter.WriteLine(
//                     structData.StructName + " " + str + " = new " + structData.StructName + "(this);");
//                 structWriter.WriteLine((returnsVoid ? "" : "var r = ") + str + "." + callClause + ";");
//                 structWriter.WriteLine(str + ".To" + structDef.MergedStructName + "(ref this);");
//                 structWriter.WriteLine(returnsVoid ? "break;" : "return r;");
//                 structWriter.EndScope();
//             }
//
//             structWriter.WriteLine("default:");
//             structWriter.BeginScope();
//             foreach (IParameterSymbol parameter in methodSymbol.Parameters)
//             {
//                 if (parameter.RefKind == 2)
//                     structWriter.WriteLine(((ISymbol)parameter).Name + " = default;");
//             }
//
//             structWriter.WriteLine("return" + (returnsVoid ? "" : " default") + ";");
//             structWriter.EndScope();
//             structWriter.EndScope();
//             structWriter.EndScope();
//         }
//
//         private string MapRefKind(RefKind argRefKind)
//         {
//             switch ((int)argRefKind)
//             {
//                 case 0:
//                     return "";
//                 case 1:
//                     return "ref ";
//                 case 2:
//                     return "out ";
//                 case 3:
//                     return "in ";
//                 default:
//                     throw new ArgumentOutOfRangeException(nameof(argRefKind), (object)argRefKind, (string)null);
//             }
//         }
//
//         private void GenerateFields(
//             FileWriter structWriter,
//             List<PolymorphicStructsSourceGenerator.MergedFieldData> mergedFields)
//         {
//             structWriter.WriteLine("public TypeId CurrentTypeId;");
//             for (int index = 0; index < mergedFields.Count; ++index)
//             {
//                 PolymorphicStructsSourceGenerator.MergedFieldData mergedField = mergedFields[index];
//                 structWriter.WriteLine("public " + mergedField.TypeName + " " + mergedField.FieldName + ";");
//             }
//         }
//
//         private void GenerateTypeEnum(
//             FileWriter structWriter,
//             List<PolymorphicStructsSourceGenerator.IndividialStructData> structDatas)
//         {
//             structWriter.WriteLine("public enum TypeId");
//             structWriter.BeginScope();
//             foreach (PolymorphicStructsSourceGenerator.IndividialStructData structData in structDatas)
//                 structWriter.WriteLine(structData.StructName + ",");
//             structWriter.EndScope();
//         }
//
//         private void GenerateStructHeader(
//             FileWriter structWriter,
//             PolymorphicStructsSourceGenerator.StructDef structDef)
//         {
//             structWriter.WriteLine("[Serializable]");
//             structWriter.WriteLine("public partial struct " + structDef.MergedStructName + " : " +
//                                    structDef.InterfaceName);
//         }
//
//         private static void GenerateUsingDirectives(
//             FileWriter mergedStructWriter,
//             PolymorphicStructsSourceGenerator.StructDef structDef)
//         {
//             foreach (string usingDirective in structDef.UsingDirectives)
//                 mergedStructWriter.WriteLine("using " + usingDirective + ";");
//         }
//
//         private static PolymorphicStructsSourceGenerator.StructDef CollectStructHeaderDef(
//             GeneratorExecutionContext context,
//             InterfaceDeclarationSyntax polymorphicInterface)
//         {
//             SyntaxToken identifier = ((BaseTypeDeclarationSyntax)polymorphicInterface).Identifier;
//             string str = identifier.Text.Substring(1);
//             string newUsing = SourceGenUtils.GetNamespace((BaseTypeDeclarationSyntax)polymorphicInterface);
//             List<string> allUsings = new List<string>();
//             PolymorphicStructsSourceGenerator.TryAddUniqueUsing(allUsings, "System");
//             if (!string.IsNullOrEmpty(newUsing))
//                 PolymorphicStructsSourceGenerator.TryAddUniqueUsing(allUsings, newUsing);
//             foreach (UsingDirectiveSyntax usingDirectiveSyntax in CSharpExtensions.GetCompilationUnitRoot(((SyntaxNode)polymorphicInterface).SyntaxTree, ((GeneratorExecutionContext) ref context).CancellationToken).Usings)
//             PolymorphicStructsSourceGenerator.TryAddUniqueUsing(allUsings,
//                 ((object)usingDirectiveSyntax.Name).ToString());
//             return new PolymorphicStructsSourceGenerator.StructDef()
//             {
//                 InterfaceName = ((BaseTypeDeclarationSyntax)polymorphicInterface).Identifier.ToString(),
//                 MergedStructName = str,
//                 Namespace = newUsing,
//                 UsingDirectives = allUsings
//             };
//         }
//
//         private static List<PolymorphicStructsSourceGenerator.MergedFieldData> BuildMergedFields(
//             List<PolymorphicStructsSourceGenerator.IndividialStructData> structDatas)
//         {
//             List<PolymorphicStructsSourceGenerator.MergedFieldData> mergedFieldDataList =
//                 new List<PolymorphicStructsSourceGenerator.MergedFieldData>();
//             List<int> intList = new List<int>();
//             foreach (PolymorphicStructsSourceGenerator.IndividialStructData structData in structDatas)
//             {
//                 intList.Clear();
//                 for (int index1 = 0; index1 < structData.Fields.Count; ++index1)
//                 {
//                     PolymorphicStructsSourceGenerator.StructFieldData field = structData.Fields[index1];
//                     int index2 = -1;
//                     for (int index3 = 0; index3 < mergedFieldDataList.Count; ++index3)
//                     {
//                         if (!intList.Contains(index3) &&
//                             string.Equals(field.TypeName, mergedFieldDataList[index3].TypeName))
//                         {
//                             index2 = index3;
//                             break;
//                         }
//                     }
//
//                     if (index2 < 0)
//                     {
//                         int count = mergedFieldDataList.Count;
//                         PolymorphicStructsSourceGenerator.MergedFieldData mergedFieldData =
//                             new PolymorphicStructsSourceGenerator.MergedFieldData();
//                         mergedFieldData.TypeName = field.TypeName;
//                         mergedFieldData.FieldName = string.Format("{0}_{1}", (object)field.TypeName,
//                             (object)mergedFieldDataList.Count);
//                         mergedFieldData.FieldNameForStructName.Add(structData.StructName, field.FieldName);
//                         mergedFieldDataList.Add(mergedFieldData);
//                         field.MergedFieldName = mergedFieldData.FieldName;
//                         intList.Add(count);
//                     }
//                     else
//                     {
//                         PolymorphicStructsSourceGenerator.MergedFieldData mergedFieldData = mergedFieldDataList[index2];
//                         mergedFieldData.FieldNameForStructName.Add(structData.StructName, field.FieldName);
//                         field.MergedFieldName = mergedFieldData.FieldName;
//                         intList.Add(index2);
//                     }
//                 }
//             }
//
//             return mergedFieldDataList;
//         }
//
//         private static List<PolymorphicStructsSourceGenerator.IndividialStructData> BuildIndividualStructsData(
//             GeneratorExecutionContext context,
//             PolymorphicStructSyntaxReceiver systemReceiver,
//             PolymorphicStructsSourceGenerator.StructDef structDef,
//             InterfaceDeclarationSyntax polymorphicInterface)
//         {
//             List<PolymorphicStructsSourceGenerator.IndividialStructData> individialStructDataList =
//                 new List<PolymorphicStructsSourceGenerator.IndividialStructData>();
//             foreach (StructDeclarationSyntax allStruct in systemReceiver.AllStructs)
//             {
//                 StructDeclarationSyntax typeSyntax = allStruct;
//                 SyntaxToken identifier1 = ((BaseTypeDeclarationSyntax)polymorphicInterface).Identifier;
//                 string text = ((SyntaxToken) ref identifier1 ).Text;
//                 if (SourceGenUtils.ImplementsInterface((BaseTypeDeclarationSyntax)typeSyntax, text))
//                 {
//                     SyntaxToken identifier2 = ((BaseTypeDeclarationSyntax)allStruct).Identifier;
//                     if (!((SyntaxToken) ref identifier2 ).Text.Equals(structDef.MergedStructName))
//                     {
//                         PolymorphicStructsSourceGenerator.IndividialStructData individialStructData =
//                             new PolymorphicStructsSourceGenerator.IndividialStructData();
//                         individialStructData.Namespace =
//                             SourceGenUtils.GetNamespace((BaseTypeDeclarationSyntax)allStruct);
//                         individialStructData.StructName = ((BaseTypeDeclarationSyntax)allStruct).Identifier.ToString();
//                         foreach (UsingDirectiveSyntax usingDirectiveSyntax in CSharpExtensions
//                                      .GetCompilationUnitRoot(((SyntaxNode)allStruct).SyntaxTree,
//                                          ((GeneratorExecutionContext) ref context).CancellationToken).Usings)
//                         PolymorphicStructsSourceGenerator.TryAddUniqueUsing(structDef.UsingDirectives,
//                             ((object)usingDirectiveSyntax.Name).ToString());
//                         INamedTypeSymbol declaredSymbol = CSharpExtensions
//                                 .GetDeclaredSymbol(((GeneratorExecutionContext) ref context).Compilation
//                                 .GetSemanticModel(((SyntaxNode)allStruct).SyntaxTree, false),
//   (BaseTypeDeclarationSyntax)allStruct, ((GeneratorExecutionContext) ref context)
//                             .CancellationToken);
//                         List<IFieldSymbol> ifieldSymbolList = declaredSymbol != null
//                             ? ((IEnumerable)ImmutableArrayExtensions.Where<ISymbol>(
//                                 ((INamespaceOrTypeSymbol)declaredSymbol).GetMembers(),
//                                 (Func<ISymbol, bool>)(it => it.Kind == 6))).Cast<IFieldSymbol>().ToList<IFieldSymbol>()
//                             : (List<IFieldSymbol>)null;
//                         if (ifieldSymbolList != null)
//                         {
//                             foreach (IFieldSymbol fieldSymbol in ifieldSymbolList)
//                                 individialStructData.Fields.Add(new PolymorphicStructsSourceGenerator.StructFieldData()
//                                 {
//                                     TypeName = ((ISymbol)fieldSymbol.Type).Name,
//                                     FieldName = PolymorphicStructsSourceGenerator.MapFieldNameToProperty(fieldSymbol)
//                                 });
//                         }
//
//                         individialStructDataList.Add(individialStructData);
//                     }
//                 }
//             }
//
//             return individialStructDataList;
//         }
//
//         private static string MapFieldNameToProperty(IFieldSymbol fieldSymbol) =>
//             fieldSymbol.AssociatedSymbol is IPropertySymbol associatedSymbol
//                 ? ((ISymbol)associatedSymbol).Name
//                 : ((ISymbol)fieldSymbol).Name;
//
//         private static void TryAddUniqueUsing(List<string> allUsings, string newUsing)
//         {
//             if (allUsings.Contains(newUsing))
//                 return;
//             allUsings.Add(newUsing);
//         }
//
//         private static void GeneratePartialIndividualStruct(
//             GeneratorExecutionContext context,
//             List<string> allUsings,
//             string mergedStructName,
//             PolymorphicStructsSourceGenerator.IndividialStructData structData,
//             List<PolymorphicStructsSourceGenerator.MergedFieldData> mergedFields)
//         {
//             FileWriter fileWriter = new FileWriter();
//             foreach (string allUsing in allUsings)
//                 fileWriter.WriteLine("using " + allUsing + ";");
//             fileWriter.WriteLine("");
//             if (!string.IsNullOrEmpty(structData.Namespace))
//             {
//                 fileWriter.WriteLine("namespace " + structData.Namespace);
//                 fileWriter.BeginScope();
//             }
//
//             fileWriter.WriteLine("public partial struct " + structData.StructName);
//             fileWriter.BeginScope();
//             fileWriter.WriteLine("public " + structData.StructName + "(" + mergedStructName + " s)");
//             fileWriter.BeginScope();
//             foreach (PolymorphicStructsSourceGenerator.StructFieldData field in structData.Fields)
//                 fileWriter.WriteLine(field.FieldName + " = s." + field.MergedFieldName + ";");
//             fileWriter.EndScope();
//             fileWriter.WriteLine("");
//             fileWriter.WriteLine("public " + mergedStructName + " To" + mergedStructName + "()");
//             fileWriter.BeginScope();
//             fileWriter.WriteLine("return new " + mergedStructName);
//             fileWriter.BeginScope();
//             fileWriter.WriteLine("CurrentTypeId = " + mergedStructName + ".TypeId." + structData.StructName + ",");
//             foreach (PolymorphicStructsSourceGenerator.MergedFieldData mergedField in mergedFields)
//             {
//                 if (mergedField.FieldNameForStructName.ContainsKey(structData.StructName))
//                     fileWriter.WriteLine(mergedField.FieldName + " = " +
//                                          mergedField.FieldNameForStructName[structData.StructName] + ",");
//             }
//
//             fileWriter.EndScope(";");
//             fileWriter.EndScope();
//             fileWriter.WriteLine("");
//             fileWriter.WriteLine("public void To" + mergedStructName + "(ref " + mergedStructName + " s)");
//             fileWriter.BeginScope();
//             fileWriter.WriteLine("s.CurrentTypeId = " + mergedStructName + ".TypeId." + structData.StructName + ";");
//             foreach (PolymorphicStructsSourceGenerator.MergedFieldData mergedField in mergedFields)
//             {
//                 if (mergedField.FieldNameForStructName.ContainsKey(structData.StructName))
//                     fileWriter.WriteLine("s." + mergedField.FieldName + " = " +
//                                          mergedField.FieldNameForStructName[structData.StructName] + ";");
//             }
//
//             fileWriter.EndScope();
//             fileWriter.EndScope();
//             if (!string.IsNullOrEmpty(structData.Namespace))
//                 fileWriter.EndScope();
//             Console.WriteLine("Generating IndividualStruct " + structData.StructName);
//             ((GeneratorExecutionContext) ref context).AddSource(structData.StructName,
//                 SourceText.From(fileWriter.FileContents, Encoding.UTF8, (SourceHashAlgorithm)1));
//         }
//
//         public class IndividialStructData
//         {
//             public string Namespace = "";
//             public string StructName = "";
//
//             public List<PolymorphicStructsSourceGenerator.StructFieldData> Fields =
//                 new List<PolymorphicStructsSourceGenerator.StructFieldData>();
//         }
//
//         public class StructFieldData
//         {
//             public string TypeName = "";
//             public string FieldName = "";
//             public string MergedFieldName = "";
//         }
//
//         public class MergedFieldData
//         {
//             public string TypeName = "";
//             public string FieldName = "";
//             public Dictionary<string, string> FieldNameForStructName = new Dictionary<string, string>();
//         }
//
//         public class StructDef
//         {
//             public string MergedStructName;
//             public string InterfaceName;
//             public string Namespace;
//             public List<string> UsingDirectives;
//         }
//     }
// }