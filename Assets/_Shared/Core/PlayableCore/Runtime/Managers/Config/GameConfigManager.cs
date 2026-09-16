using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Amanotes.Core
{
    public class GameConfigManager : SingletonMonoDontDestroy<GameConfigManager>
    {
        [SerializeField] private List<GameConfigBase> configs = new List<GameConfigBase>();
        private Dictionary<Type, GameConfigBase> configDict = new Dictionary<Type, GameConfigBase>();

        // Events
        public static event Action<GameConfigBase> OnConfigLoaded;
        public static event Action OnAllConfigsLoaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            OnConfigLoaded = null;
            OnAllConfigsLoaded = null;
        }

        // Stats
        public int TotalConfigs => configDict.Count;
        public int DuplicateCount { get; private set; }
        public int NullConfigCount { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            InitializeConfigs();
        }

        private void InitializeConfigs()
        {
            configDict.Clear();
            DuplicateCount = 0;
            NullConfigCount = 0;

            // Sort by priority (descending)
            var sortedConfigs = configs
                .Where(c => c != null)
                .OrderByDescending(c => c.Priority)
                .ToList();

            foreach (var config in sortedConfigs)
            {
                if (config == null)
                {
                    NullConfigCount++;
                    continue;
                }

                var type = config.GetType();

                // Check duplicate type
                if (configDict.ContainsKey(type))
                {
                    DuplicateCount++;
                    Debug.LogWarning($"[GameConfigManager] Duplicate config type: {type.Name}. Skipping...");
                    continue;
                }

                // Validate config
                if (!config.Validate(out string error))
                {
                    Debug.LogError($"[GameConfigManager] Config validation failed: {type.Name}\nReason: {error}");
                    continue;
                }

                // Register config
                configDict[type] = config;

                // Trigger load callback
                try
                {
                    config.OnConfigLoaded();
                    OnConfigLoaded?.Invoke(config);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameConfigManager] Error loading config {type.Name}: {ex.Message}");
                }
            }

            OnAllConfigsLoaded?.Invoke();

            Debug.Log($"[GameConfigManager] Loaded {configDict.Count} configs successfully!");

            if (DuplicateCount > 0)
                Debug.LogWarning($"[GameConfigManager] Skipped {DuplicateCount} duplicate configs");

            if (NullConfigCount > 0)
                Debug.LogWarning($"[GameConfigManager] Found {NullConfigCount} null config references");
        }

        #region Get Config Methods

        /// <summary>
        /// Get config by type (generic).
        /// Example: GetConfig<PlayerConfig>()
        /// </summary>
        public T GetConfig<T>() where T : GameConfigBase
        {
            GameConfigBase config;
            if (configDict.TryGetValue(typeof(T), out config))
                return config as T;

            Debug.LogError($"[GameConfigManager] Config of type {typeof(T).Name} not found!");
            return null;
        }

        /// <summary>
        /// Try get config (no error log)
        /// </summary>
        public bool TryGetConfig<T>(out T config) where T : GameConfigBase
        {
            GameConfigBase baseConfig;
            if (configDict.TryGetValue(typeof(T), out baseConfig))
            {
                config = baseConfig as T;
                return true;
            }

            config = null;
            return false;
        }

        /// <summary>
        /// Get config by runtime type
        /// </summary>
        public GameConfigBase GetConfig(Type type)
        {
            GameConfigBase config;
            if (configDict.TryGetValue(type, out config))
                return config;

            Debug.LogError($"[GameConfigManager] Config of type {type.Name} not found!");
            return null;
        }

        /// <summary>
        /// Get config by name
        /// </summary>

        /// <summary>
        /// Get all configs of specific type
        /// </summary>
        public List<T> GetAllConfigs<T>() where T : GameConfigBase
        {
            return configDict.Values
                .OfType<T>()
                .ToList();
        }

        /// <summary>
        /// Check if config exists
        /// </summary>
        public bool HasConfig<T>() where T : GameConfigBase
        {
            return configDict.ContainsKey(typeof(T));
        }

        #endregion

        #region Runtime Management

        /// <summary>
        /// Add config at runtime
        /// </summary>
        public bool AddConfig(GameConfigBase config)
        {
            if (config == null)
            {
                Debug.LogError("[GameConfigManager] Cannot add null config!");
                return false;
            }

            var type = config.GetType();

            if (configDict.ContainsKey(type))
            {
                Debug.LogWarning($"[GameConfigManager] Config {type.Name} already exists!");
                return false;
            }

            if (!configs.Contains(config))
                configs.Add(config);

            configDict[type] = config;

            config.OnConfigLoaded();
            OnConfigLoaded?.Invoke(config);

            Debug.Log($"[GameConfigManager] Added config: {type.Name}");
            return true;
        }

        /// <summary>
        /// Remove config at runtime
        /// </summary>
        public bool RemoveConfig<T>() where T : GameConfigBase
        {
            var type = typeof(T);

            GameConfigBase config;
            if (!configDict.TryGetValue(type, out config))
                return false;

            configDict.Remove(type);
            configs.Remove(config);

            Debug.Log($"[GameConfigManager] Removed config: {type.Name}");
            return true;
        }

        /// <summary>
        /// Reload all configs
        /// </summary>
        public void ReloadConfigs()
        {
            Debug.Log("[GameConfigManager] Reloading all configs...");
            InitializeConfigs();
        }

        #endregion

        #region Debug & Utilities

        /// <summary>
        /// Get all loaded configs info
        /// </summary>
        public string GetDebugInfo()
        {
            var info = $"=== GameConfigManager Debug Info ===\n";
            info += $"Total Configs: {configDict.Count}\n";
            info += $"Duplicates: {DuplicateCount}\n";
            info += $"Null References: {NullConfigCount}\n\n";
            info += "Loaded Configs:\n";

            foreach (var kvp in configDict.OrderBy(x => x.Value.Priority).Reverse())
            {
                var config = kvp.Value;
                info += $"  [{config.Priority}] {kvp.Key.Name}\n";
            }

            return info;
        }

        /// <summary>
        /// Print debug info to console
        /// </summary>
        [ContextMenu("Print Debug Info")]
        public void PrintDebugInfo()
        {
            Debug.Log(GetDebugInfo());
        }

        #endregion
    }
}
