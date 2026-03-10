using System.Linq;
using UnityEngine;

public static class TransformExtension
{
    /// <summary>
    /// Tries to get a single component in children, excluding the parent.
    /// </summary>
    public static T GetComponentOnlyInChildren<T>(this Transform parent) where T : Component
    {
        T component = parent.GetComponentsInChildren<T>(true)
            .FirstOrDefault(c => c.transform != parent);

        return component;
    }

    /// <summary>
    /// Tries to get all components in children, excluding the parent.
    /// Returns true if at least one was found.
    /// </summary>
    public static T[] GetComponentsOnlyInChildren<T>(this Transform parent) where T : Component
    {
        T[] components = parent.GetComponentsInChildren<T>(true)
            .Where(c => c.transform != parent)
            .ToArray();

        return components;
    }

    /// <summary>
    /// Tries to get a single component in children, excluding the parent.
    /// </summary>
    public static bool TryGetComponentOnlyInChildren<T>(this Transform parent, out T component) where T : Component
    {
        component = parent.GetComponentsInChildren<T>(true)
            .FirstOrDefault(c => c.transform != parent);

        return component != null;
    }

    /// <summary>
    /// Tries to get all components in children, excluding the parent.
    /// Returns true if at least one was found.
    /// </summary>
    public static bool TryGetComponentsOnlyInChildren<T>(this Transform parent, out T[] components) where T : Component
    {
        components = parent.GetComponentsInChildren<T>(true)
            .Where(c => c.transform != parent)
            .ToArray();

        return components.Length > 0;
    }

    public static bool HasComponent<T>(this Transform parent) where T : Component => parent.GetComponent<T>() != null;
}