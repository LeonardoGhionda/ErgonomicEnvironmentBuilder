using System;
using UnityEngine;

public class Managers : MonoBehaviour
{
    public static Managers Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public static T Get<T>() where T : Component
    {
        if (Instance == null) throw new MissingManagerException(typeof(Managers));

        T manager = Instance.GetComponentInChildren<T>(includeInactive: true) ?? 
            throw new MissingManagerException(typeof(T));

        return manager;
    }
}

public class MissingManagerException : Exception
{
    // Use the colon to pass the formatted string to the base Exception constructor
    public MissingManagerException(Type t)
        : base($"Failed to find manager of type: {t.Name}")
    {
        Debug.Log($"[INFO] New manager must be set as child of the Manager GameObject to be able to be found");
    }
}
