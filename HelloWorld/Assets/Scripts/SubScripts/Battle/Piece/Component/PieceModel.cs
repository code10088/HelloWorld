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

        public void Init(string path, Transform parent)
        {
            this.path = path;
            if (string.IsNullOrEmpty(path)) return;
            itemId = pool.Dequeue(path, parent, LoadFinish).ItemID;
        }
        protected void LoadFinish(int itemId, GameObject obj, object[] param)
        {
            this.obj = obj;
            ani = obj.GetComponentInChildren<AnimationController>();
        }
        /// <summary>
        /// ��������
        /// </summary>
        public void SetPos(Vector3 pos)
        {
            obj.transform.position = pos;
        }
        public void PlayAni(string name, float fadeLength = 0, Action finish = null)
        {
            if (!ani.enabled) ani.enabled = true;
            ani.Play(name.ToString(), fadeLength, finish);
        }
        public void StopAni()
        {
            if (ani.enabled) ani.enabled = false;
        }
        public void Clear()
        {
            pool.Enqueue(path, itemId);
            obj = null;
        }
    }
}