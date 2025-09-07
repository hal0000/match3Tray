using System;
using System.Collections.Generic;
using Match3Tray.Gameplay;
using Match3Tray.Model;
using UnityEngine;

namespace Match3Tray.Pool
{
    public sealed class FruitPool : MonoBehaviour
    {
        public List<FruitTypeDefinition> FruitTypeDefinitions = new();

        private readonly Dictionary<Enums.FruitType, Queue<FruitController>> _pools = new();


        public void InitializePools()
        {
            _pools.Clear();
            foreach (var def in FruitTypeDefinitions)
            {
                var q = new Queue<FruitController>(Mathf.Max(def.PoolSize, 1));
                for (var i = 0; i < def.PoolSize; i++)
                {
                    var c = Instantiate(def.Prefab, transform, false);
                    c.gameObject.SetActive(false);
                    c.Init(new FruitModel { Type = def.Type, Busy = false });
                    q.Enqueue(c);
                }

                _pools[def.Type] = q;
            }
        }

        public FruitController GetFruit(Enums.FruitType type)
        {
            if (!_pools.TryGetValue(type, out var q)) return null;

            var e = q.Count > 0
                ? q.Dequeue()
                : Instantiate(FindDef(type).Prefab, transform, false);

            if (e.Model == null || e.Model.Type != type)
                e.Init(new FruitModel { Type = type, Busy = false });


            e.gameObject.SetActive(true);
            e.OnSpawn();
            return e;
        }

        public void ReturnFruit(FruitController c)
        {
            if (!_pools.TryGetValue(c.Model.Type, out var q))
            {
                Destroy(c.gameObject);
                return;
            }

            c.gameObject.SetActive(false);
            c.transform.SetParent(transform, false);
            c.transform.localPosition = Vector3.zero;
            c.transform.localRotation = Quaternion.identity;
            q.Enqueue(c);
        }

        private FruitTypeDefinition FindDef(Enums.FruitType t)
        {
            for (var i = 0; i < FruitTypeDefinitions.Count; i++)
                if (FruitTypeDefinitions[i].Type == t)
                    return FruitTypeDefinitions[i];
            throw new ArgumentOutOfRangeException($"No definition for {t}");
        }

        [Serializable]
        public class FruitTypeDefinition
        {
            public Enums.FruitType Type;
            public FruitController Prefab;
            public int PoolSize = 12;
        }
    }
}