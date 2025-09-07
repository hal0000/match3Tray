using Match3Tray.Manager;
using UnityEngine;

namespace Match3Tray.Core
{
    public class BaseScene : MonoBehaviour
    {
        private protected GameManager _gameManager;

        public virtual void Awake()
        {
            _gameManager = GameManager.Instance;
        }

        public virtual void Start()
        {
        }

        public virtual void OnEnable()
        {
        }

        public virtual void OnDisable()
        {
        }

        public virtual void OnDestroy()
        {
        }
    }
}