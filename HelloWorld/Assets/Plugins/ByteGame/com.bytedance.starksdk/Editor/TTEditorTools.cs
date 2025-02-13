using UnityEditor;
using UnityEngine;

public class TTEditorTools : EditorWindow
{
    private string inputPath = "";
    
    [MenuItem("ByteGame/TTSDK API UPDATE")]
    private static void APIUpdate()
    {
        GetWindow<TTEditorTools>("TTSDK API UPDATE");
        
       
    }

    private void OnGUI()
    {
        
        GUILayout.Label("请提前做好代码备份!!!");
        GUILayout.Label("请提前做好代码备份!!!");
        GUILayout.Label("请提前做好代码备份!!!");
        GUILayout.Label("StarkSDK 替换为 TTSDK 调用目录输入(eg:Assets/AAA/BBB)");
        inputPath = EditorGUILayout.TextField("Input Path: ", inputPath);
        if (GUILayout.Button("确认"))
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                return;
            }
            bool userCancel = EditorUtility.DisplayDialog("谨慎操作!!!", $"\n确认替换\"{inputPath}\"目录下 cs 文件的 StarkSDK 调用为 TTSDK 调用吗？\n请提前做好代码备份!!!\n请提前做好代码备份!!!\n请提前做好代码备份!!!",  "取消","开始替换操作");
            if (!userCancel)
            {
                Debug.Log("TTSDK API UPDATE start...");
                TTAPIUpdate.UpdateAPI(inputPath);
                return;
            }

            Debug.Log("TTSDK API UPDATE Canceled.");
        }
    }
}