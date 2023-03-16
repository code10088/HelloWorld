using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

public class BytesDecode
{
    private byte[] bytes;
    private int index = 0;
    private List<byte> result;

    public BytesDecode(byte[] bytes, int index = 0)
    {
        this.bytes = bytes;
        this.index = index;
    }
    public BytesDecode(List<byte> result)
    {
        this.result = result;
    }

    /// <summary>
    /// 导出二进制数据
    /// </summary>
    /// <param name="bdi"></param>
    /// <param name="num"></param>
    /// <param name="path"></param>
    public static void Serialize(BytesDecodeInterface bdi, int num, string path)
    {
        List<byte> list = new List<byte>(num);
        BytesDecode bd = new BytesDecode(list);
        bd.ToBytes(bdi.GetType().ToString());
        bdi.Serialize(bd);
        File.WriteAllBytes(path, list.ToArray());

        bd.bytes = null;
        bd.result = null;
    }
    public static void Deserialize(BytesDecodeInterface bdi, byte[] bytes, Action<dynamic> complete, dynamic param = null)
    {
        //TODO：多线程
        Deserialize(bdi, bytes);
        complete?.Invoke(param);
    }
    public static void Deserialize(BytesDecodeInterface bdi, byte[] bytes)
    {
        BytesDecode bd = new BytesDecode(bytes);
        bd.ToStr();//类名
        bdi.Deserialize(bd);

        bd.bytes = null;
        bd.result = null;
    }


    public bool ToBool()
    {
        bool bc = BitConverter.ToBoolean(bytes, index);
        index++;
        return bc;
    }
    public bool[] ToBoolArray()
    {
        int len = ToInt();
        if (len > 0)
        {
            bool[] bc = new bool[len];
            for (int i = 0; i < len; i++) bc[i] = ToBool();
            return bc;
        }
        return null;
    }
    public byte ToByte()
    {
        byte bc = bytes[index];
        index++;
        return bc;
    }
    public byte[] ToByteArray()
    {
        int len = ToInt();
        if (len > 0)
        {
            byte[] bc = new byte[len];
            Buffer.BlockCopy(bytes, index, bc, 0, len);
            index += len;
            return bc;
        }
        return null;
    }
    public short ToShort()
    {
        short bc = BitConverter.ToInt16(bytes, index);
        index += 2;
        return bc;
    }
    public short[] ToShortArray()
    {
        int len = ToInt();
        if (len > 0)
        {
            short[] bc = new short[len];
            for (int i = 0; i < len; i++) bc[i] = ToShort();
            return bc;
        }
        return null;
    }
    public int ToInt()
    {
        int bc = BitConverter.ToInt32(bytes, index);
        index += 4;
        return bc;
    }
    public int[] ToIntArray()
    {
        int len = ToInt();
        if (len > 0)
        {
            int[] bc = new int[len];
            for (int i = 0; i < len; i++) bc[i] = ToInt();
            return bc;
        }
        return null;
    }
    public float ToFloat()
    {
        float bc = BitConverter.ToSingle(bytes, index);
        index += 4;
        return bc;
    }
    public float[] ToFloatArray()
    {
        int len = ToInt();
        if (len > 0)
        {
            float[] bc = new float[len];
            for (int i = 0; i < len; i++) bc[i] = ToFloat();
            return bc;
        }
        return null;
    }
    public string ToStr()
    {
        int len = ToInt();
        if (len > 0)
        {
            string result = System.Text.Encoding.Default.GetString(bytes, index, len);
            index += len;
            return result;
        }
        return string.Empty;
    }
    public string[] ToStrArray()
    {
        int len = ToInt();
        if (len > 0)
        {
            string[] bc = new string[len];
            for (int i = 0; i < len; i++) bc[i] = ToStr();
            return bc;
        }
        return null;
    }
    public Vector3 ToVector3()
    {
        float x = ToFloat();
        float y = ToFloat();
        float z = ToFloat();
        return new Vector3(x, y, z);
    }
    public Vector3[] ToVector3Array()
    {
        int len = ToInt();
        if (len > 0)
        {
            Vector3[] bc = new Vector3[len];
            for (int i = 0; i < len; i++) bc[i] = ToVector3();
            return bc;
        }
        return null;
    }
    public Quaternion ToQuaternion()
    {
        float x = ToFloat();
        float y = ToFloat();
        float z = ToFloat();
        float w = ToFloat();
        return new Quaternion(x, y, z, w);
    }
    public T ToBDI<T>(Func<T> create) where T : BytesDecodeInterface//, new()
    {
        T target = create();
        target.Deserialize(this);
        return target;
    }
    public T[] ToBDIArray<T>(Func<T> create) where T : BytesDecodeInterface//, new()
    {
        int len = ToInt();
        if (len > 0)
        {
            T[] target = new T[len];
            for (int i = 0; i < len; i++)
            {
                T temp = create();
                temp.Deserialize(this);
                target[i] = temp;
            }
            return target;
        }
        return null;
    }



    public void ToBytes(bool a)
    {
        byte[] target = BitConverter.GetBytes(a);
        result.AddRange(target);
    }
    public void ToBytes(byte a)
    {
        result.Add(a);
    }
    public void ToBytes(byte[] a)
    {
        ToBytes(a.Length);
        result.AddRange(a);
    }
    public void ToBytes(short a)
    {
        byte[] target = BitConverter.GetBytes(a);
        result.AddRange(target);
    }
    public void ToBytes(int a)
    {
        byte[] target = BitConverter.GetBytes(a);
        result.AddRange(target);
    }
    public void ToBytes(int[] a)
    {
        ToBytes(a.Length);
        for (int i = 0; i < a.Length; i++) ToBytes(a[i]);
    }
    public List<byte> ToBytes(float a)
    {
        byte[] target = BitConverter.GetBytes(a);
        result.AddRange(target);
        return result;
    }
    public void ToBytes(string a)
    {
        ToBytes(a.Length);
        byte[] s = System.Text.Encoding.Default.GetBytes(a);
        result.AddRange(s);
    }
    public void ToBytes(string[] a)
    {
        ToBytes(a.Length);
        for (int i = 0; i < a.Length; i++) ToBytes(a[i]);
    }
    public void ToBytes(Vector3 a)
    {
        ToBytes(a.x);
        ToBytes(a.y);
        ToBytes(a.z);
    }
    public void ToBytes(Vector3[] a)
    {
        ToBytes(a.Length);
        for (int i = 0; i < a.Length; i++) ToBytes(a[i]);
    }
    public void ToBytes(Quaternion a)
    {
        ToBytes(a.x);
        ToBytes(a.y);
        ToBytes(a.z);
        ToBytes(a.w);
    }
    public void ToBytes<T>(T target) where T : BytesDecodeInterface
    {
        target.Serialize(this);
    }
    public void ToBytes<T>(T[] target) where T : BytesDecodeInterface
    {
        int l = target == null ? 0 : target.Length;
        ToBytes(l);
        for (int i = 0; i < l; i++) target[i].Serialize(this);
    }


#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/BytesDecodeCopy")]
    public static void Copy()
    {
        var target = UnityEditor.Selection.activeObject;
        if (target == null) return;
        var path = UnityEditor.AssetDatabase.GetAssetPath(target);
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        var str = asset.text;

        if (str.Contains("BytesDecodeInterface"))
        {
            int index = str.IndexOf("public void Serialize(BytesDecode bd)");
            int _index = str.IndexOf("}", index);
            str = str.Remove(index, _index - index + 1);

            index = str.IndexOf("public void Deserialize(BytesDecode bd)");
            _index = str.IndexOf("}", index);
            str = str.Remove(index, _index - index + 1);
        }
        else
        {
            int index = str.IndexOf($"class {target.name}", StringComparison.OrdinalIgnoreCase);
            if (index < 0) return;
            int _index = str.IndexOf(":", index, target.name.Length + 20);
            if (_index > -1)
            {
                _index = str.IndexOf("\n", _index);
                str = str.Insert(_index - 1, ", BytesDecodeInterface");
            }
            else
            {
                index = str.IndexOf("\n", index);
                str = str.Insert(index - 1, " : BytesDecodeInterface");
            }
        }


        var fileds = Type.GetType(target.name).GetFields();
        string str1 = string.Empty, str2 = string.Empty;
        for (int i = 0; i < fileds.Length; i++)
        {
            str1 += $"        bd.ToBytes({fileds[i].Name});\n";
            string tempStr = fileds[i].FieldType.ToString();
            switch (tempStr)
            {
                case "System.Boolean": str2 += $"        {fileds[i].Name} = bd.ToBool();\n"; break;
                case "System.Boolean[]": str2 += $"        {fileds[i].Name} = bd.ToBoolArray();\n"; break;
                case "System.Byte": str2 += $"        {fileds[i].Name} = bd.ToByte();\n"; break;
                case "System.Byte[]": str2 += $"        {fileds[i].Name} = bd.ToByteArray();\n"; break;
                case "System.Int16": str2 += $"        {fileds[i].Name} = bd.ToShort();\n"; break;
                case "System.Int16[]": str2 += $"        {fileds[i].Name} = bd.ToShortArray();\n"; break;
                case "System.Int32": str2 += $"        {fileds[i].Name} = bd.ToInt();\n"; break;
                case "System.Int32[]": str2 += $"        {fileds[i].Name} = bd.ToIntArray();\n"; break;
                case "System.Single": str2 += $"        {fileds[i].Name} = bd.ToFloat();\n"; break;
                case "System.Single[]": str2 += $"        {fileds[i].Name} = bd.ToFloatArray();\n"; break;
                case "System.String": str2 += $"        {fileds[i].Name} = bd.ToStr();\n"; break;
                case "System.String[]": str2 += $"        {fileds[i].Name} = bd.ToStrArray();\n"; break;
                case "UnityEngine.Vector3": str2 += $"        {fileds[i].Name} = bd.ToVector3();\n"; break;
                case "UnityEngine.Vector3[]": str2 += $"        {fileds[i].Name} = bd.ToVector3Array();\n"; break;
                case "UnityEngine.Quaternion": str2 += $"        {fileds[i].Name} = bd.ToQuaternion();\n"; break;
                default:
                    if (tempStr.EndsWith("[]"))
                    {
                        tempStr = tempStr.Replace("[]", string.Empty);
                        str2 += $"        {fileds[i].Name} = bd.ToBDIArray(() => new {tempStr}());\n";
                        break;
                    }
                    else
                    {
                        str2 += $"        {fileds[i].Name} = bd.ToBDI(() => new {tempStr}());\n";
                        break;
                    }
            }
        }
        string result = $"    public void Serialize(BytesDecode bd)\n    {{\n{str1}    }}\n";
        result += $"    public void Deserialize(BytesDecode bd)\n    {{\n{str2}    }}\n";

        int tempIdx = str.IndexOf($"class {target.name}");
        int num = 0;
        while (true)
        {
            tempIdx = str.IndexOfAny(new char[] { '{', '}' }, tempIdx + 1);
            if (str[tempIdx] == '{') num++;
            if (str[tempIdx] == '}') num--;
            if (num == 0) break;
        }
        str = str.Insert(tempIdx, result);
        //GUIUtility.systemCopyBuffer = ;
        File.WriteAllText(Application.dataPath + path.Remove(0, 6), str);
        UnityEditor.AssetDatabase.Refresh();
    }
    [UnityEditor.MenuItem("Assets/Tools/BytesDecode/ToBytesDecode", false)]
    public static void ToBytesDecode()
    {
        var objs = UnityEditor.Selection.objects;
        for (int i = 0; i < objs.Length; i++)
        {
            if (objs[i] is ScriptableObject && objs[i] is BytesDecodeInterface)
            {
                string path = UnityEditor.AssetDatabase.GetAssetPath(objs[i]);
                path = Application.dataPath + path.Substring(6).Replace(".asset", ".bytes");
                Serialize((BytesDecodeInterface)objs[i], 0, path);
            }
        }
    }
    [UnityEditor.MenuItem("Assets/Tools/BytesDecode/ToScriptableObject", false)]
    public static void ToScriptableObject()
    {
        var objs = UnityEditor.Selection.objects;
        for (int i = 0; i < objs.Length; i++)
        {
            var ta = objs[i] as TextAsset;
            var bytes = ta.bytes;
            int len = BitConverter.ToInt32(bytes, 0);
            string className = System.Text.Encoding.Default.GetString(bytes, 4, len);
            Type type = Assembly.GetExecutingAssembly().GetType(className);
            if (type != null)
            {
                object bytesObject = Activator.CreateInstance(type);
                Deserialize((BytesDecodeInterface)bytesObject, bytes);
                UnityEngine.Object obj = (UnityEngine.Object)bytesObject;
                string path = UnityEditor.AssetDatabase.GetAssetPath(objs[i]);
                string dir = Path.GetDirectoryName(path);
                string fileName = Path.GetFileNameWithoutExtension(path);
                UnityEditor.AssetDatabase.CreateAsset(obj, $"{dir}/{fileName}.asset");
            }
        }
    }
#endif
}
public interface BytesDecodeInterface
{
    void Deserialize(BytesDecode bd);
    void Serialize(BytesDecode bd);
}