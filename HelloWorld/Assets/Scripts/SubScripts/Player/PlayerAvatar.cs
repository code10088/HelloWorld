using UnityEngine;
using System.Collections.Generic;
using cfg;
using System;

namespace HotAssembly
{
    public class PlayerAvatar
    {
        private Transform modelT;
        private Dictionary<string, Transform> allBones = new();
        private List<PlayerAvatarPartItem> dress = new();
        private List<PlayerAvatarPartItem> cache = new();
        private int cacheCount = 10;

        public PlayerAvatar(GameObject obj)
        {
            modelT = obj.transform;
            GameObject origin = modelT.Find("AvatarBone").gameObject;
            var allTransform = origin.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allTransform.Length; i++)
            {
                Transform temp = allTransform[i];
                allBones.Add(temp.name, temp);
            }
        }

        private void ChangeAvatar(int partId)
        {
            int index = dress.FindIndex(a => a.PartCfg.ID == partId);
            if (index >= 0) return;

            var target = cache.Find(a => a.PartCfg.ID == partId);
            if (target == null) target = new(this, partId, ChangeFinish);
            else cache.Remove(target);
            target.SetActive(true);
            dress.Add(target);
        }
        private void ChangeFinish(int partId)
        {
            if (partId < 0) return;
            var partCfg = ConfigManager.Instance.GameConfigs.TbPlayerAvatarPart.Get(partId);
            for (int i = 0; i < partCfg.OccupyPart.Count; i++)
            {
                var tempItem = dress.Find(a => a.PartCfg.ID != partId && a.PartCfg.OccupyPart.Contains(partCfg.OccupyPart[i]));
                if (tempItem == null) continue;
                tempItem.SetActive(false);
                dress.Remove(tempItem);
                cache.Add(tempItem);
                if (cache.Count < cacheCount) continue;
                cache[0].Release();
                cache.RemoveAt(0);
            }
        }
        public void Release()
        {
            for (int i = 0; i < dress.Count; i++) dress[i].Release();
            for (int i = 0; i < cache.Count; i++) cache[i].Release();
            allBones.Clear();
            dress.Clear();
            cache.Clear();
        }


        class PlayerAvatarPartItem : LoadGmaeObjectItem
        {
            private PlayerAvatar avatar;
            private PlayerAvatarPart partCfg;
            private Action<int> finish;
            public PlayerAvatarPart PartCfg => partCfg;

            public PlayerAvatarPartItem(PlayerAvatar avatar, int partId, Action<int> finish)
            {
                this.avatar = avatar;
                this.finish = finish;
                partCfg = ConfigManager.Instance.GameConfigs.TbPlayerAvatarPart.Get(partId);
                Init(partCfg.PrefabName, avatar.modelT);
            }
            protected override void Finish(int state, GameObject obj)
            {
                if (state < 0) return;
                if (state == 0) Combine(obj);
                finish(partCfg.ID);
            }
            /// <summary>
            /// 合并mesh+合并mat(合并贴图)
            ///     优点： 减少drawcall(drawcall非主要性能瓶颈)
            ///     缺点： 消耗cpu和内存，mat合并条件有限，只合并mesh性能提升有限
            /// </summary>
            private void Combine(GameObject obj)
            {
                var smr = obj.GetComponent<SkinnedMeshRenderer>();
                var newBones = new Transform[smr.bones.Length];
                for (int i = 0; i < newBones.Length; i++)
                {
                    if (avatar.allBones.TryGetValue(smr.bones[i].name, out Transform t)) newBones[i] = t;
                    else GameDebug.LogError("模型非同一骨骼");
                }
                smr.bones = newBones;
            }
        }
    }
}