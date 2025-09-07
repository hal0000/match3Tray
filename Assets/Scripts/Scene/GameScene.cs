using Match3Tray.Core;
using Match3Tray.Gameplay;
using Match3Tray.Interface;
using Match3Tray.Pool;
using PrimeTween;
using UnityEngine;

namespace Match3Tray.Scene
{
    public class GameScene : BaseScene
    {
        [Header("Refs")] [SerializeField] private RayController _ray;

        [SerializeField] private TrayController _tray;
        [SerializeField] private FruitPool _pool;

        [Header("Floor Spawn")] [SerializeField]
        private Collider _floor;

        [SerializeField] private LayerMask _floorMask;
        [SerializeField] private LayerMask _fruitMask;
        [SerializeField] private float _spawnDropHeight = 0.6f;
        [SerializeField] private float _gridJitter = 0.3f;
        [SerializeField] private float _overlapRadius = 0.08f;
        [SerializeField] private int _overlapTries = 6;

        [Header("Spawn Height (Y)")] [SerializeField]
        private Vector2 _spawnYRange = new(1f, 2f);

        [Header("Tray")] [SerializeField] [Min(3)]
        private int _traySize = 7;

        public override void Awake()
        {
            base.Awake();
            _gameManager.CurrentScene = this;
            _ray.OnPicked += OnPicked;
        }

        public override void Start()
        {
            base.Start();
            _tray.Init(_traySize);
            _pool.InitializePools();
            SpawnOnFloor_FromDefinitions();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_ray != null) _ray.OnPicked -= OnPicked;
        }

        private void OnPicked(IFruit fruit)
        {
            if (fruit == null) return;

            var res = _tray.TryAdd(fruit);
            if (!res.accepted)
            {
                fruit.SetColliderActive(true);
                return;
            }

            if (!res.cleared || res.clearedFruits == null) return;


            var pending = 0;
            foreach (var f in res.clearedFruits)
                if (f != null)
                    pending++;

            foreach (var f in res.clearedFruits)
            {
                if (f == null) continue;
                var go = f.Transform.gameObject;

                Tween.Scale(f.Transform, Vector3.zero, 0.12f).OnComplete(() =>
                {
                    if (go.TryGetComponent<FruitController>(out var ctrl))
                        _pool.ReturnFruit(ctrl);
                    else
                        Destroy(go);

                    pending--;
                });
            }
        }


        private void SpawnOnFloor_FromDefinitions()
        {
            if (_floor == null)
            {
                Debug.LogError("Floor collider atanmadı.");
                return;
            }

            var total = 0;
            foreach (var def in _pool.FruitTypeDefinitions)
                total += Mathf.Max(0, def.PoolSize);
            if (total == 0) return;

            var b = _floor.bounds;
            b.size *= 0.7f;

            var cols = Mathf.CeilToInt(Mathf.Sqrt(total));
            var rows = Mathf.CeilToInt((float)total / cols);
            var stepX = b.size.x / cols;
            var stepZ = b.size.z / rows;


            var cell00 = new Vector3(b.min.x + stepX * 0.5f, b.max.y + _spawnDropHeight, b.min.z + stepZ * 0.5f);

            var idx = 0;
            foreach (var def in _pool.FruitTypeDefinitions)
                for (var n = 0; n < def.PoolSize; n++, idx++)
                {
                    var r = idx / cols;
                    var c = idx % cols;

                    var jx = (Random.value * 2f - 1f) * _gridJitter * stepX;
                    var jz = (Random.value * 2f - 1f) * _gridJitter * stepZ;

                    var above = new Vector3(
                        cell00.x + c * stepX + jx,
                        cell00.y,
                        cell00.z + r * stepZ + jz
                    );


                    Vector3 place;
                    if (Physics.Raycast(above, Vector3.down, out var hit, _spawnDropHeight + 2f, _floorMask))
                        place = hit.point;
                    else
                        place = new Vector3(
                            Mathf.Clamp(above.x, b.min.x, b.max.x),
                            b.center.y,
                            Mathf.Clamp(above.z, b.min.z, b.max.z)
                        );


                    var ok = false;
                    var p = place;
                    for (var t = 0; t < _overlapTries && !ok; t++)
                    {
                        ok = !Physics.CheckSphere(p + Vector3.up * 0.05f, _overlapRadius, _fruitMask);
                        if (!ok)
                        {
                            var ang = t * 37f * Mathf.Deg2Rad;
                            p += new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (_overlapRadius * 0.75f);
                        }
                    }


                    var yOffset = Random.Range(_spawnYRange.x, _spawnYRange.y);
                    var dropPos = p + Vector3.up * yOffset;

                    var fruit = _pool.GetFruit(def.Type);
                    var tr = fruit.transform;
                    tr.SetParent(_pool.transform, true);
                    tr.position = dropPos;
                    tr.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    var rb = fruit.Rigidbody;
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;


                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;

                        rb.WakeUp();
                    }
                }
        }
    }
}