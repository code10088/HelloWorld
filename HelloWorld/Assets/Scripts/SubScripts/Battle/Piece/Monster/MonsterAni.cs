using System;
using UnityEngine;

namespace HotAssembly
{
    public enum MonsterAniEnum
    {
        Enter,
        Idle,
    }
    public class MonsterAni
    {
        private AnimationController ani;

        public void PlayAni(MonsterAniEnum name, float fadeLength = 0, Action finish = null)
        {
            if (!ani.enabled) ani.enabled = true;
            ani.Play(name.ToString(), fadeLength, finish);
        }
        public void StopAni()
        {
            if (ani.enabled) ani.enabled = false;
        }
    }
}