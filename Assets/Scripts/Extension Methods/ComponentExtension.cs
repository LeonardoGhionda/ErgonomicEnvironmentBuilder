using UnityEngine;

public static class ComponentExtensions
{
    public static bool HasComponent<T>(this Component parent) where T : Component => parent.GetComponent<T>() != null;
}