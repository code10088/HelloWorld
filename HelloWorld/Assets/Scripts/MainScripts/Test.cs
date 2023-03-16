using UnityEngine;
using MainAssembly;

namespace HotAssembly
{
    public class Test : MonoBehaviour
    {
        private void OnGUI()
        {
            if (GUI.Button(new Rect(100, 100, 100, 100), ""))
            {
                UIManager.Instance.OpenUI(1001);
            }
            if (GUI.Button(new Rect(100, 200, 100, 100), ""))
            {
                UIManager.Instance.CloseUI(1001);
            }
        }
    }
}