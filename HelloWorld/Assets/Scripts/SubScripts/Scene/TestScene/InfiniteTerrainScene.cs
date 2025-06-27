using GPUInstancerPro;
using UnityEngine;

public class InfiniteTerrainScene : SceneBase
{
    private InfiniteTerrainSceneComponent component = new InfiniteTerrainSceneComponent();
    private GPUICamera gpui;
    private GPUIFlyCamera control;

    protected override void Init()
    {
        base.Init();
        component.Init(SceneObj);
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
        GameDebug.Log("InfiniteTerrainScene OnEnable");

        if (gpui) gpui.enabled = false;
        else gpui = camera.gameObject.AddComponent<GPUICamera>();
        if (control) control.enabled = true;
        else control = camera.gameObject.AddComponent<GPUIFlyCamera>();
    }
    public override void OnDisable()
    {
        base.OnDisable();
        GameDebug.Log("InfiniteTerrainScene OnDisable");

        gpui.enabled = false;
        control.enabled = false;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        GameDebug.Log("InfiniteTerrainScene OnDestroy");

        GameObject.Destroy(gpui);
        GameObject.Destroy(control);
    }
}