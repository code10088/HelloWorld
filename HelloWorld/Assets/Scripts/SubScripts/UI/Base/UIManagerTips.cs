using cfg;
using System.Collections.Generic;

namespace HotAssembly
{
    public partial class UIManager
    {
        private Queue<UICommonBoxParam> commonBoxQueue = new Queue<UICommonBoxParam>();
        public void OpenCommonBox(UICommonBoxParam param)
        {
            commonBoxQueue.Enqueue(param);
            CheckCommonBox();
        }
        public void CheckCommonBox()
        {
            bool open = HasOpen(UIType.UICommonBox);
            if (open) return;
            if (commonBoxQueue.Count == 0) return;
            var param = commonBoxQueue.Dequeue();
            OpenUI(UIType.UICommonBox, param: param);
        }
    }
}