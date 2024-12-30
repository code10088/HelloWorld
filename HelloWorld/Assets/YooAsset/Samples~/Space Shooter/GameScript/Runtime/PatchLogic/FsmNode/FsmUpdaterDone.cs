using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Machine;

/// <summary>
/// 流程更新完毕
/// </summary>
internal class FsmUpdaterDone : IStateNode
{
    private PatchOperation _owner;

    void IStateNode.OnCreate(StateMachine machine)
    {
        _owner = machine.Owner as PatchOperation;
    }
    void IStateNode.OnEnter()
    {
        _owner.SetFinish();
    }
    void IStateNode.OnUpdate()
    {
    }
    void IStateNode.OnExit()
    {
    }
}