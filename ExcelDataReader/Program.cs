using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using ExcelDataReader;
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

            string gameConfigsCode = "";
            gameConfigsCode += "namespace SubAssembly\n";
            gameConfigsCode += "{\n";
            gameConfigsCode += $"    public partial class GameConfigs\n";
            gameConfigsCode += "    {\n";
            foreach (FileInfo info in infos)
            {
                FileStream stream = info.Open(FileMode.Open, FileAccess.Read);
                IExcelDataReader excelReader = ExcelReaderFactory.CreateReader(stream);
                DataSet result = excelReader.AsDataSet();
                excelReader.Close();
                stream.Close();

                string fileName = info.Name.Replace(".xls", string.Empty);
                string className = "Data_" + fileName;
                gameConfigsCode += $"        public {className}Array {className} = new {className}Array();\n";
                GenerateCode(codePath, className, result.Tables[0]);
                GenerateBinaryFile(binaryPath, className, result.Tables[0]);
            }
            gameConfigsCode += "    }\n";
            gameConfigsCode += "}\n";
            string targetPath = codePath + "/GameConfigs.cs";
            File.WriteAllText(targetPath, gameConfigsCode, System.Text.Encoding.UTF8);
        }
        static void GenerateCode(string targetPath, string className, DataTable table)
        {
            string code = "";
            string variableCode = "";
            string propertyCode = "";
            string enumCode = "";
            string serializeCode = "";
            string deserializeCode = "";
            code += "using System;\n";
            code += "using UnityEngine;\n";
            code += $"public class {className}Array : BytesDecodeInterface\n";
            code += "{\n";
            code += $"#if UNITY_EDITOR\n[SerializeField]\n#endif\n    private {className}[] _array;\n";
            code += $"    public {className}[] array {{ get => _array; }}\n";
            code += $"    public {className} GetDataByID(int id)\n";
            code += "    {\n";
            code += "        for (int i = 0; i < _array.Length; i++)\n";
            code += "        {\n";
            code += "            if (_array[i].ID == id) return _array[i];\n";
            code += "        }\n";
            code += "        return null;\n";
            code += "    }\n";
            code += "    public void Deserialize(BytesDecode bd)\n";
            code += "    {\n";
            code += $"        _array = bd.ToBDIArray(() => new {className}());\n";
            code += "    }\n";
            code += "    public void Serialize(BytesDecode bd)\n";
            code += "    {\n";
            code += "        bd.ToBytes(_array);\n";
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
                        variableCode += $"#if UNITY_EDITOR\n[SerializeField]\n#endif\n    private {fieldType} _{fieldName};\n";
                        propertyCode += $"    public {fieldType} {fieldName} {{ get => _{fieldName}; }}\n";
                        serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                        deserializeCode += $"        _{fieldName} = bd.ToBool();\n";
                        break;
                    case "byte":
                        variableCode += $"#if UNITY_EDITOR\n[SerializeField]\n#endif\n    private {fieldType} _{fieldName};\n";
                        propertyCode += $"    public {fieldType} {fieldName} {{ get => _{fieldName}; }}\n";
                        serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                        deserializeCode += $"        _{fieldName} = bd.ToByte();\n";
                        break;
                    case "short":
                        variableCode += $"#if UNITY_EDITOR\n[SerializeField]\n#endif\n    private {fieldType} _{fieldName};\n";
                        propertyCode += $"    public {fieldType} {fieldName} {{ get => _{fieldName}; }}\n";
                        serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                        deserializeCode += $"        _{fieldName} = bd.ToShort();\n";
                        break;
                    case "int":
                        variableCode += $"#if UNITY_EDITOR\n[SerializeField]\n#endif\n    private {fieldType} _{fieldName};\n";
                        propertyCode += $"    public {fieldType} {fieldName} {{ get => _{fieldName}; }}\n";
                        serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                        deserializeCode += $"        _{fieldName} = bd.ToInt();\n";
                        break;
                    case "float":
                        variableCode += $"#if UNITY_EDITOR\n[SerializeField]\n#endif\n    private {fieldType} _{fieldName};\n";
                        propertyCode += $"    public {fieldType} {fieldName} {{ get => _{fieldName}; }}\n";
                        serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                        deserializeCode += $"        _{fieldName} = bd.ToFloat();\n";
                        break;
                    case "string":
                        variableCode += $"#if UNITY_EDITOR\n[SerializeField]\n#endif\n    private {fieldType} _{fieldName};\n";
                        propertyCode += $"    public {fieldType} {fieldName} {{ get => _{fieldName}; }}\n";
                        serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                        deserializeCode += $"        _{fieldName} = bd.ToStr();\n";
                        break;
                    case "Vector3":
                        variableCode += $"#if UNITY_EDITOR\n[SerializeField]\n#endif\n    private {fieldType} _{fieldName};\n";
                        propertyCode += $"    public {fieldType} {fieldName} {{ get => _{fieldName}; }}\n";
                        serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                        deserializeCode += $"        _{fieldName} = bd.ToVector3();\n";
                        break;
                    case "enum":
                        string enumNameStr = table.Rows[3][i].ToString();
                        string[] enumName = enumNameStr.Split(':');
                        variableCode += $"#if UNITY_EDITOR\n[SerializeField]\n#endif\n    private {enumName[0]} _{fieldName};\n";
                        propertyCode += $"    public {enumName[0]} {fieldName} {{ get => _{fieldName}; }}\n";
                        enumCode += $"public enum {enumName[0]}\n";
                        enumCode += "{\n";
                        string[] strs = enumName[1].Split('|');
                        foreach (var item in strs)
                        {
                            enumCode += $"    {item},\n";
                        }
                        enumCode += "}\n";
                        serializeCode += $"        bd.ToBytes((int)_{fieldName});\n";
                        deserializeCode += $"        _{fieldName} = ({enumName[0]})bd.ToInt();\n";
                        break;
                    case "Array":
                        fieldType = table.Rows[3][i].ToString();
                        variableCode += $"#if UNITY_EDITOR\n[SerializeField]\n#endif\n    private {fieldType}[] _{fieldName};\n";
                        propertyCode += $"    public {fieldType}[] {fieldName} {{ get => _{fieldName}; }}\n";
                        switch (fieldType)
                        {
                            case "bool":
                                serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                                deserializeCode += $"        _{fieldName} = bd.ToBoolArray();\n";
                                break;
                            case "byte":
                                serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                                deserializeCode += $"        _{fieldName} = bd.ToByteArray();\n";
                                break;
                            case "short":
                                serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                                deserializeCode += $"        _{fieldName} = bd.ToShortArray();\n";
                                break;
                            case "int":
                                serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                                deserializeCode += $"        _{fieldName} = bd.ToIntArray();\n";
                                break;
                            case "float":
                                serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                                deserializeCode += $"        _{fieldName} = bd.ToFloatArray();\n";
                                break;
                            case "string":
                                serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                                deserializeCode += $"        _{fieldName} = bd.ToStrArray();\n";
                                break;
                            case "Vector3":
                                serializeCode += $"        bd.ToBytes(_{fieldName});\n";
                                deserializeCode += $"        _{fieldName} = bd.ToVector3Array();\n";
                                break;
                        }
                        break;
                }
            }
            code += variableCode;
            code += propertyCode;
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
        }
        static void GenerateBinaryFile(string targetPath, string className, DataTable table)
        {
            List<byte> bytes = new List<byte>(1024);
            BytesDecode bd = new BytesDecode(bytes);
            bd.ToBytes(className + "Array");
            bd.ToBytes(table.Rows.Count - 4);

            for (int i = 4; i < table.Rows.Count; i++)
            {
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    string fieldType = table.Rows[1][j].ToString();
                    string fieldValue = table.Rows[i][j].ToString();
                    switch (fieldType)
                    {
                        case "bool":
                            Type t = Type.GetType("System.Boolean");
                            dynamic convertValue = TypeDescriptor.GetConverter(t).ConvertFrom(fieldValue);
                            bd.ToBytes(convertValue);
                            break;
                        case "byte":
                            t = Type.GetType("System.Byte");
                            convertValue = TypeDescriptor.GetConverter(t).ConvertFrom(fieldValue);
                            bd.ToBytes(convertValue);
                            break;
                        case "short":
                            t = Type.GetType("System.Int16");
                            convertValue = TypeDescriptor.GetConverter(t).ConvertFrom(fieldValue);
                            bd.ToBytes(convertValue);
                            break;
                        case "int":
                            t = Type.GetType("System.Int32");
                            convertValue = TypeDescriptor.GetConverter(t).ConvertFrom(fieldValue);
                            bd.ToBytes(convertValue);
                            break;
                        case "float":
                            t = Type.GetType("System.Single");
                            convertValue = TypeDescriptor.GetConverter(t).ConvertFrom(fieldValue);
                            bd.ToBytes(convertValue);
                            break;
                        case "string":
                            t = Type.GetType("System.String");
                            convertValue = TypeDescriptor.GetConverter(t).ConvertFrom(fieldValue);
                            bd.ToBytes(convertValue);
                            break;
                        case "Vector3":
                            string[] vs = fieldValue.Split(',');
                            Vector3 v = Vector3.zero;
                            v.x = vs.Length > 0 ? float.Parse(vs[0]) : 0;
                            v.y = vs.Length > 1 ? float.Parse(vs[1]) : 0;
                            v.z = vs.Length > 2 ? float.Parse(vs[2]) : 0;
                            bd.ToBytes(v);
                            break;
                        case "enum":
                            string enumNameStr = table.Rows[3][j].ToString();
                            string[] enumName = enumNameStr.Split(':');
                            string[] strs = enumName[1].Split('|');
                            for (int k = 0; k < strs.Length; k++)
                            {
                                if(strs[k] == fieldValue)
                                {
                                    bd.ToBytes(k);
                                    break;
                                }
                            }
                            break;
                        case "Array":
                            fieldType = table.Rows[3][j].ToString();
                            string[] fields = fieldValue.Split('|');
                            bd.ToBytes(fields.Length);
                            switch (fieldType)
                            {
                                case "bool":
                                    t = Type.GetType("System.Boolean");
                                    for (int k = 0; k < fields.Length; k++)
                                    {
                                        dynamic tempData = TypeDescriptor.GetConverter(t).ConvertFrom(fields[k]);
                                        bd.ToBytes(tempData);
                                    }
                                    break;
                                case "byte":
                                    t = Type.GetType("System.Byte");
                                    for (int k = 0; k < fields.Length; k++)
                                    {
                                        dynamic tempData = TypeDescriptor.GetConverter(t).ConvertFrom(fields[k]);
                                        bd.ToBytes(tempData);
                                    }
                                    break;
                                case "short":
                                    t = Type.GetType("System.Int16");
                                    for (int k = 0; k < fields.Length; k++)
                                    {
                                        dynamic tempData = TypeDescriptor.GetConverter(t).ConvertFrom(fields[k]);
                                        bd.ToBytes(tempData);
                                    }
                                    break;
                                case "int":
                                    t = Type.GetType("System.Int32");
                                    for (int k = 0; k < fields.Length; k++)
                                    {
                                        dynamic tempData = TypeDescriptor.GetConverter(t).ConvertFrom(fields[k]);
                                        bd.ToBytes(tempData);
                                    }
                                    break;
                                case "float":
                                    t = Type.GetType("System.Single");
                                    for (int k = 0; k < fields.Length; k++)
                                    {
                                        dynamic tempData = TypeDescriptor.GetConverter(t).ConvertFrom(fields[k]);
                                        bd.ToBytes(tempData);
                                    }
                                    break;
                                case "string":
                                    t = Type.GetType("System.String");
                                    for (int k = 0; k < fields.Length; k++)
                                    {
                                        dynamic tempData = TypeDescriptor.GetConverter(t).ConvertFrom(fields[k]);
                                        bd.ToBytes(tempData);
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
                                        bd.ToBytes(v);
                                    }
                                    break;
                            }
                            break;
                    }
                }
            }

            File.WriteAllBytes(targetPath + "/" + className + ".bytes", bytes.ToArray());
            Console.WriteLine("导出成功：" + className);
        }
    }
}
