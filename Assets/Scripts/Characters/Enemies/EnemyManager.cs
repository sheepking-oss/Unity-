using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Core.Managers;
using SurvivalGame.Data.Enemies;
using SurvivalGame.Data.Managers;
using SurvivalGame.World.Environment;

namespace SurvivalGame.Characters.Enemies
{
    public class EnemyManager : ManagerBase
    {
        [Header("Spawning Settings")]
        [SerializeField] private int _maxActiveEnemies = 50;
        [SerializeField] private float _spawnInterval = 10f;
        [SerializeField] private float _minSpawnDistance = 20f;
        [SerializeField] private float _maxSpawnDistance = 50f;
        [SerializeField] private LayerMask _spawnLayers;

        [Header("Enemy Types")]
        [SerializeField] private List<EnemySpawnData> _enemySpawnData = new List<EnemySpawnData>();

        [Header("References")]
        [SerializeField] private Transform _player;
        [SerializeField] private DataManager _dataManager;

        private List<Enemy> _registeredEnemies = new List<Enemy>();
        private Dictionary<string, Enemy> _enemiesByID = new Dictionary<string, Enemy>();
        private float _spawnTimer;

        public static EnemyManager Instance => GetInstance<EnemyManager>();

        public int ActiveEnemyCount => _registeredEnemies.Count;
        public IReadOnlyList<Enemy> Enemies => _registeredEnemies.AsReadOnly();

        public override void Initialize()
        {
            base.Initialize();

            if (_player == null)
                _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (_dataManager == null)
                _dataManager = DataManager.Instance;

            RefreshEnemyList();
            _spawnTimer = _spawnInterval;
        }

        private void Update()
        {
            if (!IsInitialized) return;

            UpdateSpawning();
            RemoveDeadEnemies();
        }

        private void UpdateSpawning()
        {
            if (_player == null) return;
            if (_registeredEnemies.Count >= _maxActiveEnemies) return;
            if (!ShouldSpawnEnemies()) return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                TrySpawnEnemy();
                _spawnTimer = _spawnInterval;
            }
        }

        private bool ShouldSpawnEnemies()
        {
            foreach (EnemySpawnData spawnData in _enemySpawnData)
            {
                if (spawnData == null || spawnData.EnemyData == null) continue;

                if (IsTimeOfDayValid(spawnData) && spawnData.SpawnWeight > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsTimeOfDayValid(EnemySpawnData spawnData)
        {
            if (spawnData == null) return false;

            if (spawnData.EnemyData == null) return true;

            if (spawnData.EnemyData.IsNocturnal)
            {
                return TimeManager.Instance != null && TimeManager.Instance.IsNight;
            }

            if (spawnData.OnlyActiveAtNight)
            {
                return TimeManager.Instance != null && TimeManager.Instance.IsNight;
            }

            if (spawnData.OnlyActiveDuringDay)
            {
                return TimeManager.Instance != null && TimeManager.Instance.IsDay;
            }

            return true;
        }

        private void TrySpawnEnemy()
        {
            if (_player == null) return;

            EnemySpawnData selectedSpawnData = SelectSpawnData();
            if (selectedSpawnData == null || selectedSpawnData.EnemyData == null) return;
            if (selectedSpawnData.EnemyData.Prefab == null) return;

            Vector3 spawnPosition = GetRandomSpawnPosition();
            if (spawnPosition == Vector3.zero) return;

            GameObject enemyObject = Instantiate(
                selectedSpawnData.EnemyData.Prefab,
                spawnPosition,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            );

            Enemy enemy = enemyObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Initialize(selectedSpawnData.EnemyData);
                RegisterEnemy(enemy);
            }
        }

        private EnemySpawnData SelectSpawnData()
        {
            if (_enemySpawnData.Count == 0) return null;

            float totalWeight = 0f;
            foreach (EnemySpawnData spawnData in _enemySpawnData)
            {
                if (spawnData == null || spawnData.EnemyData == null) continue;
                if (!IsTimeOfDayValid(spawnData)) continue;

                totalWeight += spawnData.SpawnWeight;
            }

            if (totalWeight <= 0f) return null;

            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (EnemySpawnData spawnData in _enemySpawnData)
            {
                if (spawnData == null || spawnData.EnemyData == null) continue;
                if (!IsTimeOfDayValid(spawnData)) continue;

                cumulative += spawnData.SpawnWeight;
                if (random <= cumulative)
                {
                    return spawnData;
                }
            }

            return null;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            if (_player == null) return Vector3.zero;

            for (int i = 0; i < 10; i++)
            {
                float angle = Random.Range(0f, 360f);
                float distance = Random.Range(_minSpawnDistance, _maxSpawnDistance);

                Vector3 offset = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    0f,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );

                Vector3 spawnPosition = _player.position + offset;

                if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, _spawnLayers))
                {
                    return hit.point;
                }
            }

            return Vector3.zero;
        }

        private void RemoveDeadEnemies()
        {
            for (int i = _registeredEnemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = _registeredEnemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    UnregisterEnemy(enemy);
                }
            }
        }

        public void RefreshEnemyList()
        {
            _registeredEnemies.Clear();
            _enemiesByID.Clear();

            Enemy[] allEnemies = FindObjectsOfType<Enemy>();
            foreach (Enemy enemy in allEnemies)
            {
                RegisterEnemy(enemy);
            }

            Debug.Log($"[EnemyManager] Registered {_registeredEnemies.Count} enemies.");
        }

        public void RegisterEnemy(Enemy enemy)
        {
            if (enemy == null) return;

            if (!_registeredEnemies.Contains(enemy))
            {
                _registeredEnemies.Add(enemy);
            }

            if (!string.IsNullOrEmpty(enemy.EnemyID) && !_enemiesByID.ContainsKey(enemy.EnemyID))
            {
                _enemiesByID[enemy.EnemyID] = enemy;
            }

            enemy.OnEnemyDeath += OnEnemyDeath;
        }

        public void UnregisterEnemy(Enemy enemy)
        {
            if (enemy == null) return;

            _registeredEnemies.Remove(enemy);

            if (!string.IsNullOrEmpty(enemy.EnemyID))
            {
                _enemiesByID.Remove(enemy.EnemyID);
            }

            enemy.OnEnemyDeath -= OnEnemyDeath;
        }

        private void OnEnemyDeath(Enemy enemy)
        {
        }

        public Enemy GetEnemyByID(string enemyID)
        {
            if (_enemiesByID.TryGetValue(enemyID, out Enemy enemy))
            {
                return enemy;
            }
            return null;
        }

        public List<Enemy> GetEnemiesInRange(Vector3 position, float range)
        {
            List<Enemy> result = new List<Enemy>();

            foreach (Enemy enemy in _registeredEnemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;

                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance <= range)
                {
                    result.Add(enemy);
                }
            }

            return result;
        }

        public List<Enemy> GetEnemiesByType(EnemyType type)
        {
            List<Enemy> result = new List<Enemy>();

            foreach (Enemy enemy in _registeredEnemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (enemy.EnemyData == null) continue;

                if (enemy.EnemyData.EnemyType == type)
                {
                    result.Add(enemy);
                }
            }

            return result;
        }

        public void KillAllEnemies()
        {
            foreach (Enemy enemy in new List<Enemy>(_registeredEnemies))
            {
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(enemy.MaxHealth + 1f);
                }
            }
        }

        public List<EnemySaveData> GetAllEnemySaveData()
        {
            List<EnemySaveData> saveData = new List<EnemySaveData>();

            foreach (Enemy enemy in _registeredEnemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (string.IsNullOrEmpty(enemy.EnemyID)) continue;

                saveData.Add(enemy.GetSaveData());
            }

            Debug.Log($"[EnemyManager] Saved {saveData.Count} enemies.");
            return saveData;
        }

        public void LoadEnemySaveData(List<EnemySaveData> saveData)
        {
            if (saveData == null) return;

            KillAllEnemies();

            if (_dataManager == null) return;

            int loadedCount = 0;
            foreach (EnemySaveData enemySave in saveData)
            {
                if (enemySave == null) continue;

                EnemyData enemyData = _dataManager.GetEnemy(enemySave.EnemyDataID);
                if (enemyData == null || enemyData.Prefab == null) continue;

                GameObject enemyObject = Instantiate(
                    enemyData.Prefab,
                    enemySave.Position,
                    enemySave.Rotation
                );

                Enemy enemy = enemyObject.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.Initialize(enemyData);
                    enemy.LoadFromSaveData(enemySave);
                    RegisterEnemy(enemy);
                    loadedCount++;
                }
            }

            Debug.Log($"[EnemyManager] Loaded {loadedCount}/{saveData.Count} enemies from save.");
        }
    }

    [System.Serializable]
    public class EnemySpawnData
    {
        public EnemyData EnemyData;
        public float SpawnWeight = 1f;
        public bool OnlyActiveDuringDay = false;
        public bool OnlyActiveAtNight = false;
    }
}
