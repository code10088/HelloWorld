using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private void OnGUI()
    {
        if (GUI.Button(new Rect(100, 100, 100, 100), ""))
        {
            UIManager.Instance.OpenUI(UIType.UITest);
        }
        if (GUI.Button(new Rect(100, 200, 100, 100), ""))
        {
            UIManager.Instance.CloseUI(UIType.UITest);
        }
    }
}
