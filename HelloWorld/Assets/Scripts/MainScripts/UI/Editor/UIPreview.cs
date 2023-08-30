using UnityEditor;
using UnityEngine;

[CustomPreview(typeof(GameObject))]
public class UIPreview : ObjectPreview
{
    private Texture2D texture;
    private int id;
    private Rect rect1 = new Rect(0, 0, 1280f, 720f);
    private float rate1 = 1280f / 720f;
    private float rate2 = 720f / 1280f;
    private Rect rect2, rect3;
    private void GetAssetPreview(Rect r, GameObject obj)
    {
        if (rect2 == r) return;
        if (r.width / r.height > rate1) rect3 = new Rect(r.x + r.width / 2 - r.height * rate1 / 2, r.y, r.height * rate1, r.height);
        else rect3 = new Rect(r.x, r.y + r.height / 2 - r.width * rate2 / 2, r.width, r.width * rate2);
        rect2 = r;

        int temp = target.GetInstanceID();
        if (temp == id) return;
        id = temp;

        var previewRender = new PreviewRenderUtility();
        previewRender.camera.clearFlags =  CameraClearFlags.SolidColor;
        previewRender.camera.backgroundColor = new Color(0.7568628f, 0.7568628f, 0.7568628f);
        previewRender.camera.cameraType = CameraType.SceneView;
        previewRender.camera.nearClipPlane = 0.1f;
        previewRender.camera.farClipPlane = 100f;

        if (obj = previewRender.InstantiatePrefabInScene(obj))
        {
            var canvasInstance = obj.GetComponent<Canvas>();
            canvasInstance.renderMode = RenderMode.ScreenSpaceCamera;
            canvasInstance.worldCamera = previewRender.camera;
        }

        previewRender.BeginStaticPreview(rect1);
        previewRender.Render();
        texture = previewRender.EndStaticPreview();
        previewRender.Cleanup();
    }
    public override bool HasPreviewGUI()
    {
        var t = PrefabUtility.GetPrefabAssetType(target);
        return t == PrefabAssetType.Regular;
    }
    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        base.OnPreviewGUI(r, background);
        GetAssetPreview(r, (GameObject)target);
        GUI.DrawTexture(rect3, texture);
    }
}
