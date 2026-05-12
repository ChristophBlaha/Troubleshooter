using System.Collections.Generic;
using UnityEngine;

public static class UnitCollisionRegistry
{
    private static readonly List<Collider2D> RegisteredColliders = new List<Collider2D>();

    public static void RegisterUnit(Collider2D[] colliders)
    {
        if (colliders == null || colliders.Length == 0)
            return;

        CleanupNullEntries();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D ownCollider = colliders[i];
            if (ownCollider == null)
                continue;

            for (int j = 0; j < RegisteredColliders.Count; j++)
            {
                Collider2D otherCollider = RegisteredColliders[j];
                if (otherCollider == null || otherCollider == ownCollider)
                    continue;

                Physics2D.IgnoreCollision(ownCollider, otherCollider, true);
            }
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D ownCollider = colliders[i];
            if (ownCollider != null && !RegisteredColliders.Contains(ownCollider))
                RegisteredColliders.Add(ownCollider);
        }
    }

    public static void UnregisterUnit(Collider2D[] colliders)
    {
        if (colliders == null || colliders.Length == 0)
            return;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D ownCollider = colliders[i];
            if (ownCollider != null)
                RegisteredColliders.Remove(ownCollider);
        }

        CleanupNullEntries();
    }

    private static void CleanupNullEntries()
    {
        for (int i = RegisteredColliders.Count - 1; i >= 0; i--)
        {
            if (RegisteredColliders[i] == null)
                RegisteredColliders.RemoveAt(i);
        }
    }
}
