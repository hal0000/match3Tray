using System.Collections;
using System.Collections.Generic;
using Match3Tray.Binding;
using Match3Tray.Core;
using Match3Tray.Gameplay;
using Match3Tray.Interface;
using Match3Tray.Logging;
using Match3Tray.Manager;
using Match3Tray.Model;
using Match3Tray.Pool;
using PrimeTween;
using UnityEngine;

namespace Match3Tray.Scene
{
    [DefaultExecutionOrder(-450)]
    public class GameScene : BaseScene, IBindingContext
    {
        private const int FAIL_AT = 5;
        [Header("Refs")] [SerializeField] private RayController _ray;
        [SerializeField] private TrayController _tray;
        [SerializeField] private FruitPool _pool;

        [Header("Tray")] [SerializeField] [Min(3)]
        private int _traySize = 7;

        [HideInInspector] public Enums.GameState GameState = Enums.GameState.Idle;

        [SerializeField] private string _levelsResourceName = "levels"; // Assets/Resources/levels.json
        [SerializeField] private int _levelTimeSeconds = 70; // 1:10
        private int _levelIndex;
        private List<LevelModel> _levels;
        private Dictionary<Enums.FruitType, int> _remaining;
        private Coroutine _timerCo;

        public Bindable<int> GameStateIndex { get; } = new();
        public Bindable<string> GameStateText { get; private set; }
        public Bindable<int> Level { get; } = new();
        public Bindable<int> Gold { get; } = new();
        public Bindable<string> TaskText { get; } = new();

        public Bindable<string> TimerText { get; } = new(); // UI’ya bind et: "00:00"

        public override void Awake()
        {
            base.Awake();
            _gameManager.CurrentScene = this;
            _ray.OnPicked += OnPicked;
            RegisterBindingContext();
            _levels = LoadLevelsFromResources(_levelsResourceName);
        }

        public override void Start()
        {
            base.Start();
            _tray.Init(_traySize);
            _pool.InitializePools();
            _levelIndex = 0;
            SetBindingData();
            EventManager.OnGameStateChanged += GameStateChanged;
            SetupLevel(_levelIndex);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_ray != null) _ray.OnPicked -= OnPicked;
            EventManager.OnGameStateChanged -= GameStateChanged;
            UnregisterBindingContext();
        }

        public void SetBindingData()
        {
            GameStateText = new Bindable<string>(GameState.ToString());
            Gold.Value = 5000;
        }

        public void RegisterBindingContext()
        {
            BindingContextRegistry.Register(GetType().Name, this);
        }

        public void UnregisterBindingContext()
        {
            BindingContextRegistry.Unregister(GetType().Name, this);
        }

        private List<LevelModel> LoadLevelsFromResources(string resName)
        {
            var ta = Resources.Load<TextAsset>(resName);
            if (ta == null)
            {
                LoggerExtra.LogError($"Resources/{resName}.json not found.");
                return new List<LevelModel>();
            }

            var pack = JsonUtility.FromJson<LevelPack>(ta.text);
            return pack?.levels ?? new List<LevelModel>();
        }

        private void ResetBoard()
        {
            if (_tray != null && _tray.Slots != null)
                foreach (var slot in _tray.Slots)
                {
                    if (!slot) continue;
                    var fruits = slot.GetComponentsInChildren<FruitController>(true);
                    for (var i = 0; i < fruits.Length; i++) _pool.ReturnFruit(fruits[i]);
                }

            foreach (Transform t in _pool.transform)
            {
                if (!t) continue;
                if (t.TryGetComponent<FruitController>(out var fc) && t.gameObject.activeSelf)
                    _pool.ReturnFruit(fc);
            }

            _tray.Init(_traySize);
        }

        private void SetupLevel(int idx)
        {
            ResetBoard();

            var lm = _levels[idx];

            int RoundUpToTriple(int x)
            {
                if (x <= 0) return 0;
                var r = x % 3;
                return r == 0 ? x : x + (3 - r);
            }

            _remaining = new Dictionary<Enums.FruitType, int>(8);
            if (lm.tasks != null)
                foreach (var code in lm.tasks)
                {
                    var type = TaskCodec.DecodeType(code);
                    var count = RoundUpToTriple(TaskCodec.DecodeCount(code));
                    if (_remaining.TryGetValue(type, out var cur)) _remaining[type] = cur + count;
                    else _remaining[type] = count;
                }

            var plan = new Dictionary<Enums.FruitType, int>(_remaining.Count + 8);
            foreach (var kv in _remaining) plan[kv.Key] = kv.Value;

            var allTypes = new List<Enums.FruitType>(_pool.FruitTypeDefinitions.Count);
            foreach (var def in _pool.FruitTypeDefinitions)
                if (def.Prefab != null)
                    allTypes.Add(def.Type);

            var keys = new List<Enums.FruitType>(plan.Keys);
            for (var i = 0; i < keys.Count; i++)
                if (!allTypes.Contains(keys[i]))
                    plan.Remove(keys[i]);

            var targetTotal = Random.Range(30, 72);
            var current = 0;
            foreach (var kv in plan) current += kv.Value;
            if (allTypes.Count > 0)
                while (current < targetTotal)
                {
                    var t = allTypes[Random.Range(0, allTypes.Count)];
                    var add = 3;
                    if (plan.TryGetValue(t, out var old)) plan[t] = old + add;
                    else plan[t] = add;
                    current += add;
                }

            if (plan.Count == 0 && allTypes.Count > 0) plan[allTypes[0]] = 3;

            StopAllCoroutines();
            StartCoroutine(SpawnSeq(plan, 0.05f));

            Level.Value = _levelIndex + 1;
            TaskText.Value = $"Remaining Tasks: {FormatRemainingEnglish()}";
            EventManager.GameStateChanged(Enums.GameState.Playing);

            IEnumerator SpawnSeq(Dictionary<Enums.FruitType, int> p, float delay)
            {
                var spawnPos = new Vector3(0f, 2f, 0f);

                foreach (var kv in p)
                {
                    var type = kv.Key;
                    var count = kv.Value;
                    for (var n = 0; n < count; n++)
                    {
                        var fruit = _pool.GetFruit(type);
                        if (fruit == null) continue;

                        var tr = fruit.transform;
                        tr.SetParent(null, true);
                        tr.position = spawnPos;
                        tr.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                        var rb = fruit.Rigidbody;
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                            rb.useGravity = false;
                        }

                        yield return null;

                        if (rb != null)
                        {
                            rb.isKinematic = false;
                            rb.useGravity = true;
                            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                            rb.maxDepenetrationVelocity = 5f;
                            rb.solverIterations = 12;
                            rb.solverVelocityIterations = 12;
                            rb.WakeUp();
                        }

                        tr.SetParent(_pool.transform, true);

                        if (delay > 0f) yield return new WaitForSeconds(delay);
                        else yield return null;
                    }
                }
            }

            StartLevelTimer();
        }

        private bool ApplyClear(Enums.FruitType type, int cleared)
        {
            if (_remaining == null || !_remaining.ContainsKey(type) || cleared <= 0) return IsLevelComplete();
            var left = _remaining[type] - cleared;
            _remaining[type] = left <= 0 ? 0 : left;
            return IsLevelComplete();
        }

        private bool IsLevelComplete()
        {
            if (_remaining == null) return false;
            foreach (var kv in _remaining)
                if (kv.Value > 0)
                    return false;
            return true;
        }

        private string FormatRemainingEnglish()
        {
            if (_remaining == null || _remaining.Count == 0) return "-";
            var list = new List<string>(_remaining.Count);
            foreach (var kv in _remaining)
                if (kv.Value > 0)
                    list.Add($"{kv.Key} {kv.Value}");
            return list.Count == 0 ? "-" : string.Join(", ", list);
        }

        // --- UI ---
        public void StartButton()
        {
            EventManager.GameStateChanged(Enums.GameState.Playing);
        }

        public void ConfirmNextLevel()
        {
            Gold.Value += _levels[_levelIndex].rewardGold;
            _levelIndex++;
            if (_levelIndex >= _levels.Count)
            {
                EventManager.GameStateChanged(Enums.GameState.GameFinishedPrompt);
                return;
            }

            SetupLevel(_levelIndex);
        }

        public void RetryLevel()
        {
            SetupLevel(_levelIndex);
        }

        public void RestartGame()
        {
            StopAllCoroutines();
            ResetBoard();
            _levelIndex = 0;
            Gold.Value = 0;
            GameState = Enums.GameState.Idle;
            GameStateText.Value = GameState.ToString();
            GameStateIndex.Value = (int)GameState;
            SetupLevel(_levelIndex);
        }

        private void OnPicked(IFruit fruit)
        {
            if (fruit == null) return;

            var res = _tray.TryAdd(fruit);
            if (!res.accepted)
            {
                fruit.SetColliderActive(true);
                if (_tray.IsFull()) EventManager.GameStateChanged(Enums.GameState.FailedPrompt);
                return;
            }

            if (!res.cleared || res.clearedFruits == null)
            {
                if (_tray.CurrentCount >= FAIL_AT)
                    EventManager.GameStateChanged(Enums.GameState.FailedPrompt);
                return;
            }

            // ... (cleared path: shrink + pool return + remaining update vs. aynen kalsın)
            var first = res.clearedFruits[0];
            var type = (Enums.FruitType)first.TypeId;

            var clearedCount = 0;
            foreach (var f in res.clearedFruits)
                if (f != null)
                    clearedCount++;

            var pending = clearedCount;
            foreach (var f in res.clearedFruits)
            {
                if (f == null) continue;
                var go = f.Transform.gameObject;

                Tween.Scale(f.Transform, Vector3.zero, 0.12f).OnComplete(() =>
                {
                    if (go.TryGetComponent<FruitController>(out var ctrl)) _pool.ReturnFruit(ctrl);
                    else Destroy(go);

                    if (--pending != 0) return;
                    var finished = ApplyClear(type, clearedCount);
                    TaskText.Value = $"Remaining Tasks: {FormatRemainingEnglish()}";
                    if (finished) EventManager.GameStateChanged(Enums.GameState.NextLevelPrompt);
                });
            }
        }

        private static string ToMMSS(int sec)
        {
            if (sec < 0) sec = 0;
            int m = sec / 60, s = sec % 60;
            return $"{m:00}:{s:00}";
        }

        private void KillTimer()
        {
            if (_timerCo != null)
            {
                StopCoroutine(_timerCo);
                _timerCo = null;
            }
        }

        private void StartLevelTimer()
        {
            KillTimer();
            TimerText.Value = ToMMSS(_levelTimeSeconds);
            _timerCo = StartCoroutine(TimerCo(_levelTimeSeconds));
        }

        private IEnumerator TimerCo(int seconds)
        {
            float t = seconds;
            while (t > 0f && GameState == Enums.GameState.Playing)
            {
                var sec = Mathf.CeilToInt(t);
                var str = ToMMSS(sec);
                if (TimerText.Value != str) TimerText.Value = str;
                yield return null;
                t -= Time.deltaTime;
            }

            _timerCo = null;
            if (GameState == Enums.GameState.Playing) EventManager.GameStateChanged(Enums.GameState.FailedPrompt);
        }

        private void GameStateChanged(Enums.GameState newState)
        {
            if (newState != Enums.GameState.Playing) KillTimer();
            GameState = newState;
            GameStateText.Value = GameState.ToString();
            GameStateIndex.Value = (int)newState;
        }
    }
}