using System;
using UnityEngine;

namespace HotAssembly
{
    public enum PieceAniEnum
    {
        Enter,
        Idle,
    }
    public class PieceModel
    {
        protected static GameObjectPool pool = new GameObjectPool();

        private int itemId;
        private string path;
        private GameObject obj;
        private AnimationController ani;
        private Vector3 pos;

        public Vector3 Pos => pos;

        public void Init(string path, Transform parent, Vector3 pos)
        {
            if (string.IsNullOrEmpty(path)) return;
            this.path = path;
            this.pos = pos;
            itemId = pool.Dequeue(path, parent, LoadFinish).ItemID;
        }
        protected void LoadFinish(int itemId, GameObject obj, object[] param)
        {
            this.obj = obj;
            ani = obj.GetComponentInChildren<AnimationController>();
            SetPos(pos);
        }
        /// <summary>
        /// ÊÀ½ç×ø±ê
        /// </summary>
        public void SetPos(Vector3 pos)
        {
            if (obj == null) return;
            obj.transform.position = pos;
            this.pos = pos;
        }
        public void PlayAni(string name, float fadeLength = 0, Action finish = null)
        {
            if (ani == null) return;
            if (!ani.enabled) ani.enabled = true;
            ani.Play(name.ToString(), fadeLength, finish);
        }
        public void StopAni()
        {
            if (ani == null) return;
            if (ani.enabled) ani.enabled = false;
        }
        public void Clear()
        {
            if (string.IsNullOrEmpty(path)) return;
            pool.Enqueue(path, itemId);
            obj = null;
        }
    }
}