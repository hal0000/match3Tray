using Match3Tray.Interface;
using PrimeTween;
using UnityEngine;

namespace Match3Tray.Gameplay
{
    public class TrayController : MonoBehaviour
    {
        [Header("Slots (0..N-1 sırayla)")] public Transform[] Slots;

        [Header("Tween")] public float PlaceTween = 0.15f;

        private Tray _tray;
        private IFruit[] _visual;

        private void Awake()
        {
            _visual = new IFruit[Slots.Length];
        }

        public void Init(int capacity)
        {
            _tray = new Tray(Slots.Length);
            for (var i = 0; i < _visual.Length; i++) _visual[i] = null;
        }

        public (bool accepted, bool cleared, IFruit[] clearedFruits) TryAdd(IFruit fruit)
        {
            var res = _tray.TryAdd(fruit.TypeId);
            if (!res.Accepted) return (false, false, null);


            var free = -1;
            for (var i = 0; i < Slots.Length; i++)
                if (_visual[i] == null)
                {
                    free = i;
                    break;
                }


            _visual[free] = fruit;
            fruit.MarkInTray(true);
            fruit.SetColliderActive(false);


            SnapRootToSlot(fruit, Slots[free]);

            IFruit[] cleared = null;

            if (res.Cleared && res.ClearedIndices != null)
            {
                cleared = new IFruit[res.ClearedIndices.Length];
                for (var k = 0; k < res.ClearedIndices.Length; k++)
                {
                    var idx = res.ClearedIndices[k];
                    cleared[k] = _visual[idx];
                    _visual[idx] = null;
                }


                var w = 0;
                for (var r = 0; r < Slots.Length; r++)
                {
                    if (_visual[r] == null) continue;
                    if (w != r)
                    {
                        var f = _visual[r];
                        _visual[w] = f;
                        _visual[r] = null;
                        SnapRootToSlot(f, Slots[w]);
                    }

                    w++;
                }
            }

            return (true, res.Cleared, cleared);
        }


        private void SnapRootToSlot(IFruit fruit, Transform slot)
        {
            var root = fruit.Transform;
            var anchor = fruit.MeshCollider.transform;

            root.SetParent(slot, true);


            var targetWorldRot = slot.rotation * Quaternion.Inverse(anchor.localRotation);
            var targetWorldPos = slot.position - targetWorldRot * anchor.localPosition;


            Tween.Rotation(root, targetWorldRot, PlaceTween, Ease.OutQuad);
            Tween.Position(root, targetWorldPos, PlaceTween, Ease.OutQuad);
        }
    }
}