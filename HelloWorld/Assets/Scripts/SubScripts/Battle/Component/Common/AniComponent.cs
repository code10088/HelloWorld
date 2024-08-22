using System;

namespace HotAssembly
{
    public class AniComponent : ECS_Component
    {
        private GameObjectComponent obj;
        private AnimationController ani;
        private string name;
        private float fade;
        private Action finish;

        public void Init(GameObjectComponent obj)
        {
            this.obj = obj;
            PlayAni(name, fade, finish);
        }
        private void Init()
        {
            if (ani != null) return;
            if (obj.Obj == null) return;
            ani = obj.Obj.GetComponentInChildren<AnimationController>();
        }
        public void PlayAni(string name, float fade = 0, Action finish = null)
        {
            Init();
            this.name = name;
            this.fade = fade;
            this.finish = finish;
            if (ani == null) return;
            if (!ani.enabled) ani.enabled = true;
            ani.Play(name.ToString(), fade, finish);
        }
        public void StopAni()
        {
            if (ani == null) return;
            if (ani.enabled) ani.enabled = false;
        }
        public void Clear()
        {
            ani = null;
            finish = null;
        }
    }
}