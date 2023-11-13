using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class UICommonTips : UIBase
    {
        private UICommonTipsComponent component = new UICommonTipsComponent();
        private List<UICommonTipsItem> items = new List<UICommonTipsItem>();

        protected override void Init()
        {
            base.Init();
            component.Init(UIObj);
        }
        public override async UniTask OnEnable(params object[] param)
        {
            await base.OnEnable(param);
            ShowTips((string)param[0]);
        }
        public void ShowTips(string str)
        {
            var item = items.Find(a => a.free);
            if (item == null)
            {
                var obj = Instantiate(component.itemObj, UIObj.transform);
                var rt = obj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, -200);
                item = new UICommonTipsItem();
                item.Init(obj);
                item.Show(str);
                items.Add(item);
            }
            else
            {
                item.Show(str);
            }
        }
    }
    public partial class UICommonTipsItem
    {
        private int timerId = -1;
        public bool free = true;
        public void Show(string str)
        {
            free = false;
            obj.SetActive(true);
            obj.transform.SetAsLastSibling();
            contentTextMeshProUGUI.SetText(str);
            itemTweenPlayer.SetForwardDirectionAndEnabled();
            if (timerId < 0) timerId = TimeManager.Instance.StartTimer(1, finish: Finish);
        }
        private void Finish()
        {
            free = true;
            timerId = -1;
            obj.SetActive(false);
            UIManager.Instance.CheckCommonTips();
        }
    }
}