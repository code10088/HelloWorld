using cfg;
using System.Collections.Generic;
using UnityEngine;

public class UICommonTips : UIBase
{
    private UICommonTipsComponent component = new UICommonTipsComponent();
    private static Queue<string> commonTipsQueue = new Queue<string>();
    private static Queue<UICommonTipsItem> items = new Queue<UICommonTipsItem>();

    protected override void Init()
    {
        base.Init();
        component.Init(UIObj);
        var item = new UICommonTipsItem();
        item.Init(component.itemObj);
        items.Enqueue(item);
        for (int i = 0; i < 5; i++)
        {
            var obj = Instantiate(component.itemObj, UIObj.transform);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -200);
            item = new UICommonTipsItem();
            item.Init(obj);
            items.Enqueue(item);
        }
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
        Show();
    }

    public static void Show()
    {
        if (items.Count == 0) return;
        if (commonTipsQueue.Count == 0) return;
        var str = commonTipsQueue.Dequeue();
        var item = items.Dequeue();
        item.Show(str);
    }

    public static void ShowTips(string str)
    {
        commonTipsQueue.Enqueue(str);
        Check();
    }
    private static void Check()
    {
        if (UIManager.Instance.HasOpen(UIType.UICommonTips)) Show();
        else UIManager.Instance.OpenUI(UIType.UICommonTips);
    }
    public static void Recycle(UICommonTipsItem item)
    {
        items.Enqueue(item);
    }
}
public partial class UICommonTipsItem
{
    private int timerId = -1;
    public void Show(string str)
    {
        obj.SetActive(true);
        obj.transform.SetAsLastSibling();
        contentTextMeshProUGUI.SetText(str);
        itemTweenPlayer.SetForwardDirectionAndEnabled();
        if (timerId < 0) timerId = Driver.Instance.StartTimer(1, finish: Finish);
    }
    private void Finish()
    {
        timerId = -1;
        obj.SetActive(false);
        UICommonTips.Recycle(this);
    }
}