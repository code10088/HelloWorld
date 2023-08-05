using UnityEngine;
using System.Collections.Generic;

public class PlayerAvatar
{
    private Transform modelT;
    private Dictionary<string, Transform> allBones = new();

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

    //合并mesh+合并mat(合并贴图)
    //  优点： 减少drawcall
    //  缺点： mat合并条件有限，只合并mesh性能提升有限
    //         贴图合并后要缓存所有贴图，卸载之后无法进行下一次合并
    //         drawcall非主要性能瓶颈
    private void ChangeAvatar(GameObject obj)
    {
        obj.transform.parent = modelT;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        var smr = obj.GetComponent<SkinnedMeshRenderer>();
        var newBones = new Transform[smr.bones.Length];
        for (int i = 0; i < newBones.Length; i++)
        {
            if (allBones.TryGetValue(smr.bones[i].name, out Transform t)) newBones[i] = t;
            else GameDebug.LogError("模型非同一骨骼");                
        }
        smr.bones = newBones;
    }
}
