using UnityEngine;
using MainAssembly;

namespace SubAssembly
{
    public class Test : MonoBehaviour
    {
        private void OnGUI()
        {
            if (GUI.Button(new Rect(100, 100, 100, 100), ""))
            {
                UIManager.Instance.OpenUI((int)UIType.UITest);
            }
            if (GUI.Button(new Rect(100, 200, 100, 100), ""))
            {
                UIManager.Instance.CloseUI((int)UIType.UITest);
            }
        }
    }
}