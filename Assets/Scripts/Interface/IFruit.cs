using UnityEngine;

namespace Match3Tray.Interface
{
    public interface IFruit
    {
        int TypeId { get; }
        Transform Transform { get; }
        Collider MeshCollider { get; }

        void MarkInTray(bool v);
        public void SetColliderActive(bool val);
    }
}