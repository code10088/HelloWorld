using cfg;
using UnityEngine;

public class BattleManager : Singletion<BattleManager>
{
    private int updateId = -1;
    private int sceneId = -1;
    public BattleScene BattleScene;
    public AssetObjectPool Pool = new AssetObjectPool();

    public Vector2 InputWorldPos => BattleScene.ScreenToWorldPoint(Input.mousePosition);

    public void Init(SceneType type)
    {
        sceneId = SceneManager.Instance.OpenScene(type, Init);
    }
    private void Init(int id, bool success)
    {
        BattleScene = SceneManager.Instance.GetScene(id) as BattleScene;
        updateId = Updater.Instance.StartUpdate(Update);
    }
    public void Exit()
    {
        Pool.Destroy();
        Updater.Instance.StopUpdate(updateId);
        SceneManager.Instance.CloseScene(sceneId);
        BattleScene = null;
        sceneId = -1;
    }
    private void Update(float t)
    {

    }
}