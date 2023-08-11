using UnityEngine;
using System.Collections.Generic;
using cfg;
using System;
using Object = UnityEngine.Object;

namespace HotAssembly
{
    public class PlayerAvatar
    {
        private Transform modelT;
        private Dictionary<string, Transform> allBones = new();
        private List<PlayerAvatarPartItem> dress = new();
        private List<PlayerAvatarPartItem> cache = new();

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
                if (cache.Count > 10)
                {
                    cache[0].Release();
                    cache.RemoveAt(0);
                }
            }
        }


        class PlayerAvatarPartItem
        {
            private PlayerAvatar avatar;
            private PlayerAvatarPart partCfg;
            private Action<int> finish;
            private Object partAsset;
            private GameObject partObj;
            private int loaderID;
            private int state = 0;//7：二进制111：分别表示release instantiate load
            public bool Active => state <= 3;
            public PlayerAvatarPart PartCfg => partCfg;

            public PlayerAvatarPartItem(PlayerAvatar avatar, int partId, Action<int> finish)
            {
                this.avatar = avatar;
                this.finish = finish;
                partCfg = ConfigManager.Instance.GameConfigs.TbPlayerAvatarPart.Get(partId);
            }
            public void SetActive(bool b)
            {
                if (b && !Active)
                {
                    partObj?.SetActive(true);
                    state &= 3;
                    Load();
                }
                else if (!b && Active)
                {
                    partObj?.SetActive(false);
                    state |= 4;
                }
            }
            private void Load()
            {
                if (state == 0)
                {
                    loaderID = AssetManager.Instance.Load<GameObject>(partCfg.PrefabName, LoadFinish);
                }
                else if (state == 1)
                {
                    LoadFinish(loaderID, partAsset);
                }
                else
                {
                    partObj.SetActive(true);
                    finish(partCfg.ID);
                }
            }
            private void LoadFinish(int id, Object asset)
            {
                if (asset == null)
                {
                    Release();
                    finish(-1);
                }
                else if (state > 3)
                {
                    partAsset = asset;
                    state |= 1;
                    finish(-1);
                }
                else
                {
                    state = 3;
                    partObj = Object.Instantiate(asset, Vector3.zero, Quaternion.identity, avatar.modelT) as GameObject;
                    Combine();
                    finish(partCfg.ID);
                }
            }
            /// <summary>
            /// 合并mesh+合并mat(合并贴图)
            ///     优点： 减少drawcall(drawcall非主要性能瓶颈)
            ///     缺点： 消耗cpu和内存，mat合并条件有限，只合并mesh性能提升有限
            /// </summary>
            private void Combine()
            {
                var smr = partObj.GetComponent<SkinnedMeshRenderer>();
                var newBones = new Transform[smr.bones.Length];
                for (int i = 0; i < newBones.Length; i++)
                {
                    if (avatar.allBones.TryGetValue(smr.bones[i].name, out Transform t)) newBones[i] = t;
                    else GameDebug.LogError("模型非同一骨骼");
                }
                smr.bones = newBones;
            }
            public void Release()
            {
                if (partObj != null) GameObject.Destroy(partObj);
                AssetManager.Instance.Unload(loaderID);
                avatar = null;
                partCfg = null;
                partAsset = null;
                partObj = null;
                loaderID = -1;
                state = 0;
            }
        }
    }
}