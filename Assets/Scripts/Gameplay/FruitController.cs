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
            Rigidbody.isKinematic = true;
            Rigidbody.useGravity = false;
            Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Collider.enabled = true;
        }

        public int TypeId => (int)Model.Type;
        public Transform Transform => transform;
        public Collider MeshCollider => Collider;

        public void MarkInTray(bool v)
        {
            Model.Busy = v;
            Collider.enabled = !v;
            if (v)
            {
                Rigidbody.isKinematic = true;
                Rigidbody.useGravity = false;
            }
            else
            {
                Rigidbody.isKinematic = false;
                Rigidbody.useGravity = true;
                Rigidbody.WakeUp();
            }
        }

        public void SetColliderActive(bool val)
        {
            Collider.enabled = val;
        }

        public void Init(FruitModel model)
        {
            Model = model;
        }

        public void OnSpawn()
        {
            Collider.enabled = true;
            Model.Busy = false;
        }
    }
}