using UnityEngine;

namespace Match3Tray.Core
{
    public abstract class BasePool<T> : MonoBehaviour where T : Component
    {
        [SerializeField] public T _prefab;
        [SerializeField] public int _initialSize = 20;
        private int _count;

        private T[] _items;

        protected virtual void Awake()
        {
            _items = new T[_initialSize];
            _count = _initialSize;
            for (var i = 0; i < _initialSize; i++)
            {
                var instance = Instantiate(_prefab, transform);
                instance.gameObject.SetActive(false);
                InitializeItem(instance);
                _items[i] = instance;
            }
        }

        protected virtual void InitializeItem(T item)
        {
        }

        public T Get()
        {
            T item;
            if (_count > 0)
            {
                item = _items[--_count];
            }
            else
            {
                item = Instantiate(_prefab, transform);
                InitializeItem(item);
            }

            item.gameObject.SetActive(true);
            OnGet(item);
            return item;
        }

        public void Return(T item)
        {
            OnReturn(item);
            item.gameObject.SetActive(false);
            if (_count < _items.Length)
                _items[_count++] = item;
            else
                Destroy(item.gameObject);
        }

        protected virtual void OnGet(T item)
        {
        }

        protected virtual void OnReturn(T item)
        {
        }
    }
}