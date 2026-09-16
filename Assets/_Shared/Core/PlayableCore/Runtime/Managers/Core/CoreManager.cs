using System;
using System.Collections.Generic;
using UnityEngine;

namespace Amanotes.Core
{
    public class CoreManager : SingletonMonoDontDestroy<CoreManager>
    {
        private readonly List<CoreWrapper> cores = new List<CoreWrapper>();
        private int coreCount = 0;
        private bool isInitialized = false;

        #region Core Wrapper

        private class CoreWrapper : IComparable<CoreWrapper>
        {
            public ICore core;
            public int priority;

            public int CompareTo(CoreWrapper other)
            {
                return priority.CompareTo(other.priority);
            }
        }

        #endregion

        #region Core Controller

        protected override void Awake()
        {
            base.Awake();

            for (int i = 0; i < coreCount; i++)
            {
                try
                {
                    cores[i].core?.OnAwake();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoreManager] OnAwake Error: {e}");
                }
            }

            isInitialized = true;
        }

        void Start()
        {
            for (int i = 0; i < coreCount; i++)
            {
                try
                {
                    cores[i].core?.OnStart();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoreManager] OnStart Error: {e}");
                }
            }
        }

        void Update()
        {
            for (int i = 0; i < coreCount; i++)
            {
                cores[i].core?.OnUpdate();
            }
        }

        void FixedUpdate()
        {
            for (int i = 0; i < coreCount; i++)
            {
                cores[i].core?.OnFixedUpdate();
            }
        }

        #endregion

        #region Registration

        public void Register(ICore core, int priority = 0)
        {
            if (core == null)
            {
                Debug.LogWarning("[CoreManager] Cannot register null core");
                return;
            }

            for (int i = 0; i < coreCount; i++)
            {
                if (cores[i].core == core)
                {
                    Debug.LogWarning($"[CoreManager] Core already registered: {core.GetType().Name}");
                    return;
                }
            }

            var wrapper = new CoreWrapper { core = core, priority = priority };
            cores.Add(wrapper);
            coreCount++;

            cores.Sort();

            if (isInitialized)
            {
                try
                {
                    core.OnAwake();
                    core.OnStart();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoreManager] Late registration error: {e}");
                }
            }
        }

        public void Unregister(ICore core)
        {
            if (core == null) return;

            for (int i = 0; i < coreCount; i++)
            {
                if (cores[i].core == core)
                {
                    cores[i] = cores[coreCount - 1];
                    cores.RemoveAt(coreCount - 1);
                    coreCount--;
                    return;
                }
            }
        }

        #endregion

        #region Query

        public T GetCore<T>() where T : class, ICore
        {
            for (int i = 0; i < coreCount; i++)
            {
                var result = cores[i].core as T;
                if (result != null)
                    return result;
            }
            return null;
        }

        public bool HasCore<T>() where T : class, ICore
        {
            return GetCore<T>() != null;
        }

        #endregion
    }
}
