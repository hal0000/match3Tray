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
                    SanitizeTransform(c.transform); // <- önemli
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

            // RB: reuse öncesi güvenli kapat
            var rb = e.Rigidbody;
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // TRANSFORM: tüm finite değerleri garanti et
            SanitizeTransform(e.transform);

            // parent world’e al; GameScene spawn’da tekrar konumlandırıyor
            e.transform.SetParent(null, true);

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

            // Tween/animasyon vb. etkileri bitmeden state’i kilitle
            var rb = c.Rigidbody;
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                // hız set etmiyoruz
            }

            // transform’u temizle (∞/NaN/0-scale parent zincirini kır)
            SanitizeTransform(c.transform);

            c.transform.SetParent(transform, false);
            c.transform.localPosition = Vector3.zero;
            c.transform.localRotation = Quaternion.identity;
            c.transform.localScale = Vector3.one;

            c.gameObject.SetActive(false);
            q.Enqueue(c);
        }

        private FruitTypeDefinition FindDef(Enums.FruitType t)
        {
            for (var i = 0; i < FruitTypeDefinitions.Count; i++)
                if (FruitTypeDefinitions[i].Type == t)
                    return FruitTypeDefinitions[i];
            throw new ArgumentOutOfRangeException($"No definition for {t}");
        }

        // --- sanitize helpers ---
        private static bool FiniteVec(Vector3 v)
        {
            return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        }

        private static bool FiniteQuat(Quaternion q)
        {
            return float.IsFinite(q.x) && float.IsFinite(q.y) && float.IsFinite(q.z) && float.IsFinite(q.w);
        }

        private void SanitizeTransform(Transform t)
        {
            if (!FiniteVec(t.position)) t.position = Vector3.zero;
            if (!FiniteQuat(t.rotation)) t.rotation = Quaternion.identity;
            if (!FiniteVec(t.localScale) ||
                Mathf.Approximately(t.localScale.x, 0f) ||
                Mathf.Approximately(t.localScale.y, 0f) ||
                Mathf.Approximately(t.localScale.z, 0f))
                t.localScale = Vector3.one;

            // parent zincirinde 0-scale varsa kopar
            var p = t.parent;
            while (p != null)
            {
                var s = p.localScale;
                if (!FiniteVec(s) || Mathf.Approximately(s.x, 0f) || Mathf.Approximately(s.y, 0f) || Mathf.Approximately(s.z, 0f))
                {
                    t.SetParent(null, true);
                    break;
                }

                p = p.parent;
            }
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