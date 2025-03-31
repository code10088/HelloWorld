using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;

public class FightCache : EntityCache
{
    public void AddMonster(int monsterId, int allyId, Vector3 pos)
    {
        var config = ConfigManager.Instance.GameConfigs.TbMonsterConfig[monsterId];
        Type t;
        int index;
        switch (config.MonsterType)
        {
            case MonsterType.Normal:
                t = typeof(MonsterMoveEntity);
                index = cache.FindIndex(a => a?.GetType() == t);
                MonsterMoveEntity move = index < 0 ? new MonsterMoveEntity() : (MonsterMoveEntity)cache[index];
                if (index >= 0) cache.RemoveAt(index);
                move.Init();
                move.Init(allyId);
                move.Init(config, pos);
                entities.Add(move);
                break;
            case MonsterType.Rvo:
                t = typeof(MonsterRvoEntity);
                index = cache.FindIndex(a => a?.GetType() == t);
                MonsterRvoEntity rvo = index < 0 ? new MonsterRvoEntity() : (MonsterRvoEntity)cache[index];
                if (index >= 0) cache.RemoveAt(index);
                rvo.Init();
                rvo.Init(allyId);
                rvo.Init(config, pos);
                entities.Add(rvo);
                break;
        }
    }
    public FightEntity FindNearTarget(Vector3 pos, int allyId, List<int> exclude = null)
    {
        float dis = 10000;
        FightEntity target = null;
        for (int i = 0; i < entities.Count; i++)
        {
            var a = entities[i];
            if (a == null) continue;
            var e = a as FightEntity;
            if (e.AllyId == allyId) continue;
            if (exclude != null && exclude.Contains(e.ItemId)) continue;
            float f = Vector3.Distance(pos, e.Transform.Pos);
            if (f > dis) continue;
            dis = f;
            target = e;
        }
        return target;
    }
    public FightEntity[] FindNearTarget(Vector3 pos, int allyId, int num)
    {
        float[] dis = new float[num];
        FightEntity[] target = new FightEntity[num];
        for (int i = 0; i < entities.Count; i++)
        {
            var a = entities[i];
            if (a == null) continue;
            var e = a as FightEntity;
            if (e.AllyId == allyId) continue;
            float f = Vector3.Distance(pos, e.Transform.Pos);
            for (int j = 0; j < num; j++)
            {
                if (dis[j] == 0 || dis[j] > f)
                {
                    dis[j] = f;
                    target[j] = e;
                    break;
                }
            }
        }
        return target;
    }
    public FightEntity FindNearSelfTarget(Vector3 pos, int allyId, List<int> exclude = null)
    {
        float dis = 10000;
        FightEntity target = null;
        for (int i = 0; i < entities.Count; i++)
        {
            var a = entities[i];
            if (a == null) continue;
            var e = a as FightEntity;
            if (e.AllyId != allyId) continue;
            if (exclude != null && exclude.Contains(e.ItemId)) continue;
            float f = Vector3.Distance(pos, e.Transform.Pos);
            if (f > dis) continue;
            dis = f;
            target = e;
        }
        return target;
    }
}