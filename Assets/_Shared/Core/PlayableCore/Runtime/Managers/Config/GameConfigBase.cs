using UnityEngine;

namespace Amanotes.Core
{
    /// <summary>
    /// Base class for all game configs
    /// </summary>
    public abstract class GameConfigBase : ScriptableObject
    {
        [Header("Config Info")]
        [SerializeField] private int priority = 0; // Higher = load first

        public int Priority => priority;

        /// <summary>
        /// Called when config is loaded
        /// </summary>
        public virtual void OnConfigLoaded() { }

        /// <summary>
        /// Validate config data
        /// </summary>
        public virtual bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            return true;
        }
    }
}
