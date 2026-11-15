using System;
using System.Collections.Generic;
using UnityEngine;

// Four-neighbour A*: no diagonal corner cutting. Shared by placement validation
// and NPC movement, so a layout is checked with the same clearance it will use.
public sealed class ShopRouteGrid
{
    public const float Cell = .25f;
    private readonly int width, height;
    private readonly Vector2 origin;
    private readonly bool[] walkable;
    private readonly Func<Vector3, bool> canWalk;

    public ShopRouteGrid(Rect area, Func<Vector3, bool> canWalk)
    {
        this.canWalk = canWalk;
        origin = area.min;
        width = Mathf.FloorToInt(area.width / Cell) + 1;
        height = Mathf.FloorToInt(area.height / Cell) + 1;
        walkable = new bool[width * height];
        for (int i = 0; i < walkable.Length; i++) walkable[i] = canWalk(Point(i));
    }

    private Vector3 Point(int index) => new Vector3(origin.x + index % width * Cell, 0, origin.y + index / width * Cell);
    private int Nearest(Vector3 point)
    {
        int best = -1;
        float distance = .46f * .46f;
        int x = Mathf.RoundToInt((point.x - origin.x) / Cell);
        int z = Mathf.RoundToInt((point.z - origin.y) / Cell);
        for (int dz = -2; dz <= 2; dz++)
            for (int dx = -2; dx <= 2; dx++)
            {
                int nx = x + dx, nz = z + dz;
                if (nx < 0 || nz < 0 || nx >= width || nz >= height) continue;
                int index = nz * width + nx;
                float d = (Point(index) - new Vector3(point.x, 0, point.z)).sqrMagnitude;
                if (walkable[index] && d < distance) { best = index; distance = d; }
            }
        return best;
    }

    public bool TryPath(Vector3 from, Vector3 to, out List<Vector3> path)
    {
        path = new List<Vector3>();
        if (!canWalk(new Vector3(to.x, 0, to.z))) return false;
        int start = Nearest(from), goal = Nearest(to);
        if (start < 0 || goal < 0) return false;
        int count = walkable.Length;
        var previous = new int[count];
        var costs = new float[count];
        var closed = new bool[count];
        for (int i = 0; i < count; i++) { previous[i] = -1; costs[i] = float.PositiveInfinity; }
        var open = new List<int> { start };
        costs[start] = 0;
        while (open.Count > 0)
        {
            int pick = 0;
            float best = float.PositiveInfinity;
            for (int i = 0; i < open.Count; i++)
            {
                int node = open[i];
                float priority = costs[node] + Mathf.Abs(node % width - goal % width) + Mathf.Abs(node / width - goal / width);
                if (priority < best) { best = priority; pick = i; }
            }
            int current = open[pick];
            open.RemoveAt(pick);
            if (current == goal)
            {
                for (int node = goal; node != -1; node = previous[node]) path.Add(Point(node));
                path.Reverse();
                // The physical approach points are checked when furnishing is saved.
                path.Add(new Vector3(to.x, 0, to.z));
                return true;
            }
            closed[current] = true;
            int x = current % width, z = current / width;
            foreach (int next in new[] { x > 0 ? current - 1 : -1, x + 1 < width ? current + 1 : -1,
                z > 0 ? current - width : -1, z + 1 < height ? current + width : -1 })
            {
                if (next < 0 || !walkable[next] || closed[next] || costs[current] + 1 >= costs[next]) continue;
                costs[next] = costs[current] + 1;
                previous[next] = current;
                if (!open.Contains(next)) open.Add(next);
            }
        }
        return false;
    }

    // Greedy line-of-sight shortcutting for NPC movement. Placement validation
    // keeps using the raw grid path; only the walked waypoints are reduced.
    // Every shortcut segment is sampled with the same walkability predicate,
    // so smoothed paths keep the identical clearance as the raw grid route.
    public List<Vector3> SmoothPath(List<Vector3> raw)
    {
        var smooth = new List<Vector3>();
        if (raw == null || raw.Count == 0) return smooth;
        smooth.Add(raw[0]);
        int anchor = 0;
        while (anchor < raw.Count - 1)
        {
            int farthest = anchor + 1;
            for (int i = raw.Count - 1; i > anchor; i--)
            {
                if (LineWalkable(raw[anchor], raw[i])) { farthest = i; break; }
            }
            smooth.Add(raw[farthest]);
            anchor = farthest;
        }
        return smooth;
    }

    private bool LineWalkable(Vector3 a, Vector3 b)
    {
        float distance = Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z));
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / .05f));
        for (int i = 0; i <= steps; i++)
        {
            Vector3 point = Vector3.Lerp(a, b, (float)i / steps);
            if (!canWalk(new Vector3(point.x, 0, point.z))) return false;
        }
        return true;
    }
}
