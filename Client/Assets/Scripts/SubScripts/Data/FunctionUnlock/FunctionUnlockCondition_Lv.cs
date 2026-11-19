using cfg;

public class FunctionUnlockCondition_Lv : ConditionBase
{
	public FunctionUnlockCondition_Lv(ConditionConfig _config) : base(_config) { }

	public override bool CheckCondition(params object[] param)
	{
		return DataManager.Instance.PlayerData.Lv >= config.IntParam[0];
	}
}