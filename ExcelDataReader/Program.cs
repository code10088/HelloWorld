using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;
using ExcelDataReader;
using Microsoft.CSharp;
using UnityEngine;

namespace MyClass
{
    class Program
    {
        static string csPath = Environment.CurrentDirectory + "../../../../HelloWorld/Assets/Scripts/SubScripts/ConfigScripts";
        static string bytePath = Environment.CurrentDirectory + "../../../../HelloWorld/Assets/ZRes/Config";
        static string excelPath = Environment.CurrentDirectory + "../../../../Excel";
        static void Main(string[] args)
        {
            string input = Console.ReadLine();
            if (input != string.Empty) excelPath = input;

            Clear(csPath);
            Clear(bytePath);
            ExportExcelToBinary(excelPath, csPath, bytePath);
            Console.ReadLine();
        }
        static void Clear(string path)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                FileSystemInfo[] fileInfos = dir.GetFileSystemInfos();
                foreach (FileSystemInfo i in fileInfos)
                {
                    if (i is DirectoryInfo)
                    {
                        DirectoryInfo subDir = new DirectoryInfo(i.FullName);
                        subDir.Delete(true);
                    }
                    else
                    {
                        File.Delete(i.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("删除文件失败：" + ex.ToString());
                Console.ResetColor();
            }
        }
        static void ExportExcelToBinary(string filePath, string codePath, string binaryPath)
        {
            FileInfo[] infos = new FileInfo[1];
            if (Directory.Exists(filePath))
            {
                DirectoryInfo dir = new DirectoryInfo(filePath);
                infos = dir.GetFiles();
            }
            else
            {
                infos[0] = new FileInfo(filePath);
            }
            foreach (FileInfo info in infos)
            {
                FileStream stream = info.Open(FileMode.Open, FileAccess.Read);
                IExcelDataReader excelReader = ExcelReaderFactory.CreateReader(stream);
                DataSet result = excelReader.AsDataSet();
                excelReader.Close();
                stream.Close();

                string className = "Data_" + info.Name.Replace(".xls", "");
                string code = GenerateCode(codePath, className, result.Tables[0]);
                CompilerResults cr = DynamicCompilation(code);
                GenerateBinaryFile(binaryPath, cr, className, result.Tables[0]);
            }
        }
        static string GenerateCode(string targetPath, string className, DataTable table)
        {
            string code = "";
            string enumCode = "";
            string serializeCode = "";
            string deserializeCode = "";
            code += "using System;\n";
            code += "using UnityEngine;\n";
            code += $"public class {className}Array : BytesDecodeInterface\n";
            code += "{\n";
            code += $"    public {className}[] array;\n";
            code += "    public void Deserialize(BytesDecode bd)\n";
            code += "    {\n";
            code += $"        array = bd.ToBDIArray(() => new {className}());\n";
            code += "    }\n";
            code += "    public void Serialize(BytesDecode bd)\n";
            code += "    {\n";
            code += "        bd.ToBytes(array);\n";
            code += "    }\n";
            code += "}\n";
            code += "#if UNITY_EDITOR\n";
            code += "[Serializable]\n";
            code += "#endif\n";
            code += $"public class {className} : BytesDecodeInterface\n";
            code += "{\n";
            for (int i = 0; i < table.Columns.Count; i++)
            {
                string fieldType = table.Rows[1][i].ToString();
                string fieldName = table.Rows[2][i].ToString();
                switch (fieldType)
                {
                    case "bool":
                        code += $"    public {fieldType} {fieldName};\n";
                        serializeCode += $"        bd.ToBytes({fieldName});\n";
                        deserializeCode += $"        {fieldName} = bd.ToBool();\n";
                        break;
                    case "byte":
                        code += $"    public {fieldType} {fieldName};\n";
                        serializeCode += $"        bd.ToBytes({fieldName});\n";
                        deserializeCode += $"        {fieldName} = bd.ToByte();\n";
                        break;
                    case "short":
                        code += $"    public {fieldType} {fieldName};\n";
                        serializeCode += $"        bd.ToBytes({fieldName});\n";
                        deserializeCode += $"        {fieldName} = bd.ToShort();\n";
                        break;
                    case "int":
                        code += $"    public {fieldType} {fieldName};\n";
                        serializeCode += $"        bd.ToBytes({fieldName});\n";
                        deserializeCode += $"        {fieldName} = bd.ToInt();\n";
                        break;
                    case "float":
                        code += $"    public {fieldType} {fieldName};\n";
                        serializeCode += $"        bd.ToBytes({fieldName});\n";
                        deserializeCode += $"        {fieldName} = bd.ToFloat();\n";
                        break;
                    case "string":
                        code += $"    public {fieldType} {fieldName};\n";
                        serializeCode += $"        bd.ToBytes({fieldName});\n";
                        deserializeCode += $"        {fieldName} = bd.ToStr();\n";
                        break;
                    case "Vector3":
                        code += $"    public {fieldType} {fieldName};\n";
                        serializeCode += $"        bd.ToBytes({fieldName});\n";
                        deserializeCode += $"        {fieldName} = bd.ToVector3();\n";
                        break;
                    case "enum":
                        string enumNameStr = table.Rows[3][i].ToString();
                        string[] enumName = enumNameStr.Split(':');
                        code += $"    public {enumName[0]} {fieldName};\n";
                        enumCode += $"public enum {enumName[0]}\n";
                        enumCode += "{\n";
                        string[] strs = enumName[1].Split('|');
                        foreach (var item in strs)
                        {
                            enumCode += $"    {item},\n";
                        }
                        enumCode += "}\n";
                        serializeCode += $"        bd.ToBytes((int){fieldName});\n";
                        deserializeCode += $"        {fieldName} = ({enumName[0]})bd.ToInt();\n";
                        break;
                    case "Array":
                        fieldType = table.Rows[3][i].ToString();
                        code += $"    public {fieldType}[] {fieldName};\n";
                        switch (fieldType)
                        {
                            case "bool":
                                serializeCode += $"        bd.ToBytes({fieldName});\n";
                                deserializeCode += $"        {fieldName} = bd.ToBoolArray();\n";
                                break;
                            case "byte":
                                serializeCode += $"        bd.ToBytes({fieldName});\n";
                                deserializeCode += $"        {fieldName} = bd.ToByteArray();\n";
                                break;
                            case "short":
                                serializeCode += $"        bd.ToBytes({fieldName});\n";
                                deserializeCode += $"        {fieldName} = bd.ToShortArray();\n";
                                break;
                            case "int":
                                serializeCode += $"        bd.ToBytes({fieldName});\n";
                                deserializeCode += $"        {fieldName} = bd.ToIntArray();\n";
                                break;
                            case "float":
                                serializeCode += $"        bd.ToBytes({fieldName});\n";
                                deserializeCode += $"        {fieldName} = bd.ToFloatArray();\n";
                                break;
                            case "string":
                                serializeCode += $"        bd.ToBytes({fieldName});\n";
                                deserializeCode += $"        {fieldName} = bd.ToStrArray();\n";
                                break;
                            case "Vector3":
                                serializeCode += $"        bd.ToBytes({fieldName});\n";
                                deserializeCode += $"        {fieldName} = bd.ToVector3Array();\n";
                                break;
                        }
                        break;
                }
            }
            code += "    public void Deserialize(BytesDecode bd)\n";
            code += "    {\n";
            code += deserializeCode;
            code += "    }\n";
            code += "    public void Serialize(BytesDecode bd)\n";
            code += "    {\n";
            code += serializeCode;
            code += "    }\n";
            code += "}\n";
            code += enumCode;
            targetPath += "/" + className + ".cs";
            File.WriteAllText(targetPath, code, System.Text.Encoding.UTF8);
            return code;
        }
        static CompilerResults DynamicCompilation(string code)
        {
            CSharpCodeProvider csharpCodePrivoder = new CSharpCodeProvider();
            CompilerParameters compilerParameters = new CompilerParameters();
            //添加需要引用的dll
            compilerParameters.ReferencedAssemblies.Add("System.dll");
            compilerParameters.ReferencedAssemblies.Add("UnityEngine.CoreModule.dll");
            compilerParameters.ReferencedAssemblies.Add("BytesDecodeDll.dll");
            //是否生成可执行文件(exe)
            compilerParameters.GenerateExecutable = false;
            //是否生成在内存中
            compilerParameters.GenerateInMemory = true;
            //编译代码
            return csharpCodePrivoder.CompileAssemblyFromSource(compilerParameters, code);
        }
        static void GenerateBinaryFile(string targetPath, CompilerResults cr, string className, DataTable table)
        {
            Assembly objAssembly = cr.CompiledAssembly;
            dynamic list = objAssembly.CreateInstance(className + "Array");
            FieldInfo listFieldInfo = list.GetType().GetField("array");
            Type arrayType = GetArrayElementType(listFieldInfo.FieldType);
            Array listInstance = Array.CreateInstance(arrayType, table.Rows.Count - 4);
            MethodInfo listMethodInfo = listInstance.GetType().GetMethod("Set");
            for (int i = 4; i < table.Rows.Count; i++)
            {
                object obj = objAssembly.CreateInstance(className);
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    string fieldType = table.Rows[1][j].ToString();
                    string fieldName = table.Rows[2][j].ToString();
                    string fieldValue = table.Rows[i][j].ToString();
                    FieldInfo fieldInfo = obj.GetType().GetField(fieldName);
                    object convertValue = null;
                    switch (fieldType)
                    {
                        case "bool":
                        case "byte":
                        case "short":
                        case "int":
                        case "float":
                        case "string":
                            convertValue = TypeDescriptor.GetConverter(fieldInfo.FieldType).ConvertFrom(fieldValue);
                            break;
                        case "Vector3":
                            string[] vs = fieldValue.Split(',');
                            Vector3 v = Vector3.zero;
                            v.x = vs.Length > 0 ? float.Parse(vs[0]) : 0;
                            v.y = vs.Length > 1 ? float.Parse(vs[1]) : 0;
                            v.z = vs.Length > 2 ? float.Parse(vs[2]) : 0;
                            convertValue = v;
                            break;
                        case "enum":
                            convertValue = Enum.Parse(fieldInfo.FieldType, fieldValue);
                            break;
                        case "Array":
                            fieldType = table.Rows[3][j].ToString();
                            string[] fields = fieldValue.Split('|');
                            arrayType = GetArrayElementType(fieldInfo.FieldType);
                            convertValue = Array.CreateInstance(arrayType, fields.Length);
                            MethodInfo add = convertValue.GetType().GetMethod("Set");
                            switch (fieldType)
                            {
                                case "bool":
                                case "byte":
                                case "short":
                                case "int":
                                case "float":
                                case "string":
                                    for (int k = 0; k < fields.Length; k++)
                                    {
                                        object tempData = TypeDescriptor.GetConverter(arrayType).ConvertFrom(fields[k]);
                                        add.Invoke(convertValue, new object[] { k, tempData });
                                    }
                                    break;
                                case "Vector3":
                                    for (int k = 0; k < fields.Length; k++)
                                    {
                                        vs = fields[k].Split(',');
                                        v = Vector3.zero;
                                        v.x = vs.Length > 0 ? float.Parse(vs[0]) : 0;
                                        v.y = vs.Length > 1 ? float.Parse(vs[1]) : 0;
                                        v.z = vs.Length > 2 ? float.Parse(vs[2]) : 0;
                                        add.Invoke(convertValue, new object[] { k, v });
                                    }
                                    break;
                            }
                            break;
                    }
                    fieldInfo.SetValue(obj, convertValue);
                }
                listMethodInfo.Invoke(listInstance, new object[] { i - 4, obj });
                listFieldInfo.SetValue(list, listInstance);
            }

            BytesDecode.Serialize(list, 1024, targetPath + "/" + className + ".bytes");
            Console.WriteLine("导出成功：" + className);
        }

        static Type GetArrayElementType(Type t)
        {
            if (!t.IsArray) return null;
            string name = t.FullName.Replace("[]", string.Empty);
            Type result = t.Assembly.GetType(name);
            return result;
        }
    }
}
