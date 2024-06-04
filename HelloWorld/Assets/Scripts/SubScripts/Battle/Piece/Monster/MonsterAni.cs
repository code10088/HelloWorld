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
        private Animator ani;

        public void PlayAni(MonsterAniEnum name, float duration = 0.2f)
        {
            if (!ani.enabled) ani.enabled = true;
            ani.CrossFade(name.ToString(), duration);
        }
        public void StopAni()
        {
            if (ani.enabled) ani.enabled = false;
        }
    }
}