using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using System.Collections.Generic;
using System.Numerics;
using Content.Client.Physics;
using Content.Shared._Mental.Occlusion;

namespace Content.Client._Mental.Occlusion;

public sealed class PolygonOccluderSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize() {
        base.Initialize();
    }

    /// <summary>
    /// Getting all polygons (in world coords) that are in presented bounding box (AABB)
    /// </summary>
    public List<List<Vector2>> GetWorldPolygonsInBounds(Box2Rotated worldBounds)
    {
        var result = new List<List<Vector2>>();
        var query = EntityQueryEnumerator<PolygonOccluderComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var occluder, out var xform))
        {
            if (!occluder.Enabled)
                continue;

            // Rough check to skip invisible polygons
            var worldEntityPos = _transform.GetWorldPosition(uid);
            if (!worldBounds.Contains(worldEntityPos))
                continue;

            // Trasforming local coords to world coords for polygon
            var worldPoints = new List<Vector2>(occluder.LocalVertices.Count);
            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);

            foreach (var localVertex in occluder.LocalVertices)
            {
                // rotating vertices
                var rotated = worldRot.RotateVec(localVertex);
                worldPoints.Add(worldPos + rotated);
            }

            result.Add(worldPoints);
        }

        return result;
    }
}
