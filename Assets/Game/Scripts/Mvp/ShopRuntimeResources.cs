using System;
using System.Collections.Generic;
using UnityEngine;

// Only runtime-created resources belong here. Imported assets are borrowed, never owned.
public sealed class ShopRuntimeResources : IDisposable
{
    private readonly HashSet<UnityEngine.Object> resources = new HashSet<UnityEngine.Object>();
    private bool disposed;
    public int Count => resources.Count;
    public T Own<T>(T resource) where T : UnityEngine.Object
    {
        if (disposed) throw new ObjectDisposedException(nameof(ShopRuntimeResources));
        if (resource != null) resources.Add(resource);
        return resource;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (var resource in resources)
        {
            if (resource == null) continue;
            if (resource is RenderTexture texture) texture.Release();
            UnityEngine.Object.Destroy(resource);
        }
        resources.Clear();
    }
}
