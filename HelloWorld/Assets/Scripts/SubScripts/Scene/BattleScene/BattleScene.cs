using UnityEngine;

public class BattleScene : SceneBase
{
    public Transform GetTransform(string path)
    {
        return SceneObj.transform.Find(path);
    }
    public Vector3 ScreenToWorldPoint(Vector2 p)
    {
        float dis = Mathf.Abs(config.CameraPos.z);
        return camera.ScreenToWorldPoint(new Vector3(p.x, p.y, dis));
    }
}