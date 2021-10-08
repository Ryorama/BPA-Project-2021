using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// A MonoBehaviour class for those objects that should only have one instance
    /// </summary>
    /// <typeparam name="T">The object type</typeparam>
    public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviourSingleton<T>
    {
        private static T instance;
        public static T Instance
        {
            get { return instance; }
            set { instance = value; }
        }

        protected virtual void Awake()
        {
            if (instance == null)
                instance = (T)this;
            else if (instance != this)
            {
                Debug.LogWarning("Destroying extra instance of " + name);
                Destroy(this);
            }
        }
    }
}
