using cfg;
using Message;
using System.Collections.Generic;

public class ShowRewardData : DataBase
{
    private Queue<ShowRewardStruct[]> queue = new Queue<ShowRewardStruct[]>();
    private ShowRewardStruct[] current;

    public ShowRewardStruct[] Current => current;

    public void Init()
    {
    }
    public void Clear()
    {
    }

    public void ShowRewards(List<RewardInfo> rewards)
    {
        if (rewards == null || rewards.Count == 0)
        {
            return;
        }
        ShowRewardStruct[] temp = new ShowRewardStruct[rewards.Count];
        for (int i = 0; i < rewards.Count; i++)
        {
            temp[i] = new ShowRewardStruct { Id = (int)rewards[i].itemId, Count = (int)rewards[i].Count };
        }
        if (current == null)
        {
            current = temp;
            UIManager.Instance.OpenUI(UIType.UIShowReward);
        }
        else
        {
            queue.Enqueue(temp);
        }
    }
    public void Next()
    {
        if (queue.Count > 0)
        {
            current = queue.Dequeue();
            UIManager.Instance.OpenUI(UIType.UIShowReward);
        }
        else
        {
            UIManager.Instance.CloseUI(UIType.UIShowReward);
            current = null;
        }
    }
}
public struct ShowRewardStruct
{
    public int Id { get; set; }
    public int Count { get; set; }
}