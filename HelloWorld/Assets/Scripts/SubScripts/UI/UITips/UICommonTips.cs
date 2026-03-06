using cfg;
using System.Collections.Generic;
using UnityEngine;

public class UICommonTips : UIBase
{
    private UICommonTipsComponent comp;
    private static Queue<string> commonTipsQueue = new Queue<string>();
    private static Queue<UICommonTipsItem> items = new Queue<UICommonTipsItem>();

    protected override void Init()
    {
        base.Init();
        comp = component as UICommonTipsComponent;
        var item = new UICommonTipsItem();
        item.Init(comp.itemGameObject);
        items.Enqueue(item);
        for (int i = 0; i < 5; i++)
        {
            var obj = Instantiate(comp.itemGameObject, UIObj.transform);
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
public class UICommonTipsItem
{
    private UICommonTipsItemComponent comp;
    private int timerId = -1;
    public void Init(GameObject obj)
    {
        comp = obj.GetComponent<UICommonTipsItemComponent>();
    }
    public void Show(string str)
    {
        comp.gameObject.SetActive(true);
        comp.gameObject.transform.SetAsLastSibling();
        comp.contentTextMeshProUGUI.SetText(str);
        if (timerId < 0) timerId = Driver.Instance.StartTimer(1, finish: Finish);
    }
    private void Finish()
    {
        timerId = -1;
        comp.gameObject.SetActive(false);
        UICommonTips.Recycle(this);
    }
}