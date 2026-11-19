using cfg;

public abstract class ConditionBase
{
	protected ConditionConfig config;

	public ConditionBase(ConditionConfig _config)
	{
		config = _config;
	}
	public abstract bool CheckCondition(params object[] param);
}