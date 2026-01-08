using System;
using System.Collections.Generic;

public interface DataBase
{
    void Init();
    void Clear();
}
public partial class DataManager : Singletion<DataManager>
{
    private Dictionary<Type, DataBase> dataMap = new Dictionary<Type, DataBase>();

    [Obsolete("DataManager_Auto中自动生成相关Data")]
    public T GetData<T>() where T : class, DataBase, new()
    {
        Type type = typeof(T);
        if (dataMap.TryGetValue(type, out var data1))
        {
            return data1 as T;
        }
        else
        {
            T data2 = new T();
            data2.Init();
            dataMap[type] = data2;
            return data2;
        }
    }
    [Obsolete("DataManager_Auto中自动生成相关Data")]
    public void ClearDataMap()
    {
        foreach (var data in dataMap.Values) data.Clear();
        dataMap.Clear();
    }
}