using cfg;
using System.Collections.Generic;

namespace HotAssembly
{
    public partial class UIManager
    {
        private Queue<UIMessageBoxParam> messageBoxQueue = new Queue<UIMessageBoxParam>();
        public void OpenMessageBox(UIMessageBoxParam param)
        {
            messageBoxQueue.Enqueue(param);
            CheckMessageBox();
        }
        public void CheckMessageBox()
        {
            bool open = HasOpen(UIType.UIMessageBox);
            if (open) return;
            if (messageBoxQueue.Count == 0) return;
            var param = messageBoxQueue.Dequeue();
            OpenUI(UIType.UIMessageBox, param: param);
        }
    }
}