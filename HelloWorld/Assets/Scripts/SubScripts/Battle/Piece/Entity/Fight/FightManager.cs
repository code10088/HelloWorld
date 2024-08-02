using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class FightManager : Singletion<FightManager>
    {
        private List<FightEntity> pieces = new List<FightEntity>(10000);
        private List<FightEntity> cache = new List<FightEntity>(10000);

        public void AddMonster(int monsterId, int allyId, Vector3 pos)
        {
            var config = ConfigManager.Instance.GameConfigs.TbMonsterConfig[monsterId];
            Type t;
            int index;
            switch (config.MonsterType)
            {
                case MonsterType.Normal:
                    t = typeof(MonsterEntity);
                    index = cache.FindIndex(a => a.GetType() == t);
                    MonsterEntity nromal = index < 0 ? new MonsterEntity() : (MonsterEntity)cache[index];
                    if (index >= 0) cache.RemoveAt(index);
                    nromal.Init();
                    nromal.Init(allyId);
                    nromal.Init(config, pos);
                    pieces.Add(nromal);
                    break;
            }
        }
        public FightEntity GetFight(int id)
        {
            var result = pieces.Find(a => a.ItemId == id);
            return result;
        }
        public T GetFight<T>(int id) where T : FightEntity
        {
            var result = GetFight(id);
            if (result == null) return null;
            else return result as T;
        }
        public void Update()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                var temp = pieces[i];
                if (temp.Update(Time.deltaTime))
                {
                    temp.Clear();
                    cache.Add(temp);
                    pieces.RemoveAt(i);
                    i--;
                }
            }
        }
        public FightEntity FindNearTarget(Vector3 pos, int allyId, List<int> exclude = null)
        {
            float dis = 10000;
            FightEntity target = null;
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                if (p.AllyId == allyId) continue;
                if (exclude != null && exclude.Contains(p.ItemId)) continue;
                float f = Vector3.Distance(pos, p.PieceModel.Pos);
                if (f > dis) continue;
                dis = f;
                target = p;
            }
            return target;
        }
        public FightEntity[] FindNearTarget(Vector3 pos, int allyId, int num)
        {
            float[] dis = new float[num];
            FightEntity[] target = new FightEntity[num];
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                if (p.AllyId == allyId) continue;
                float f = Vector3.Distance(pos, p.PieceModel.Pos);
                for (int j = 0; j < num; j++)
                {
                    if (dis[j] == 0 || dis[j] > f)
                    {
                        dis[j] = f;
                        target[j] = p;
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
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                if (p.AllyId != allyId) continue;
                if (exclude != null && exclude.Contains(p.ItemId)) continue;
                float f = Vector3.Distance(pos, p.PieceModel.Pos);
                if (f > dis) continue;
                dis = f;
                target = p;
            }
            return target;
        }
    }
}