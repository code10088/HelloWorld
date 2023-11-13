using cfg;
using System.Collections.Generic;

namespace HotAssembly
{
    public partial class UIManager
    {
        private Queue<UICommonBoxParam> commonBoxQueue = new Queue<UICommonBoxParam>();
        private Queue<string> commonTipsQueue = new Queue<string>();
        public void OpenCommonBox(UICommonBoxParam param)
        {
            commonBoxQueue.Enqueue(param);
            CheckCommonBox();
        }
        public void CheckCommonBox()
        {
            if (commonBoxQueue.Count == 0) return;
            if (HasOpen(UIType.UICommonBox)) return;
            var param = commonBoxQueue.Dequeue();
            OpenUI(UIType.UICommonBox, param: param);
        }
        public void ShowTips(string str)
        {
            commonTipsQueue.Enqueue(str);
            CheckCommonTips();
        }
        public void CheckCommonTips()
        {
            if (commonTipsQueue.Count == 0) return;
            var commonTips = GetUI(UIType.UICommonTips);
            var str = commonTipsQueue.Dequeue();
            if (commonTips == null) OpenUI(UIType.UICommonTips, param: str);
            else ((UICommonTips)commonTips).ShowTips(str);
        }
    }
}