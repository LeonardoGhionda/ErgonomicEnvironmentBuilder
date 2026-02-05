using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class GameObjectExtension
{ 
    public static bool HasComponent<T>(this GameObject parent) where T : Component => parent.GetComponent<T>() != null;
}