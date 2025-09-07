using Match3Tray.Interface;
using Match3Tray.Model;
using UnityEngine;

namespace Match3Tray.Gameplay
{
    public class FruitController : MonoBehaviour, IFruit
    {
        [SerializeField] public Rigidbody Rigidbody;
        [SerializeField] public Collider Collider;

        public FruitModel Model;

        private void Awake()
        {
            if (Rigidbody != null)
            {
                Rigidbody.isKinematic = true;
                Rigidbody.useGravity = false;
                Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            if (Collider != null) Collider.enabled = true;
        }

        public int TypeId => (int)Model.Type;
        public Transform Transform => transform;
        public Collider MeshCollider => Collider;

        public void MarkInTray(bool v)
        {
            Model.Busy = v;
            if (Collider != null) Collider.enabled = !v;
            if (Rigidbody != null)
            {
                Rigidbody.isKinematic = v;
                Rigidbody.useGravity = !v;
                if (!v) Rigidbody.WakeUp();
            }
        }

        public void SetColliderActive(bool val)
        {
            if (Collider != null) Collider.enabled = val;
        }

        public void Init(FruitModel model)
        {
            Model = model;
        }

        public void OnSpawn()
        {
            if (Collider != null) Collider.enabled = true;
            Model.Busy = false;
        }
    }
}