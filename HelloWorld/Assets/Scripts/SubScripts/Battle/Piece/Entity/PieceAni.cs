using System;
using UnityEngine;

namespace HotAssembly
{
    public enum PieceAniEnum
    {
        Enter,
        Idle,
    }
    public partial class PieceEntity
    {
        private AnimationController ani;

        public void PlayAni(string name, float fadeLength = 0, Action finish = null)
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