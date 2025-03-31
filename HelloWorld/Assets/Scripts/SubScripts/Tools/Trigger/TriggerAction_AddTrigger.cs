using cfg;

public class TriggerAction_AddTrigger : ActionBase
{
    private TriggerManager triggerManager;

    public TriggerAction_AddTrigger(ActionConfig _config) : base(_config) { }

    public void Init(TriggerManager _triggerManager)
    {
        triggerManager = _triggerManager;
    }

    public override void Excute(params object[] param)
    {
        triggerManager.AddTrigger(config.IntParam[0]);
    }
}