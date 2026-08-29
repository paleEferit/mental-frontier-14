using Content.Shared._Mental.Occlusion;
using Robust.Client.GameObjects;
using System.Numerics;

namespace Content.Client._Mental.Occlusion;

public sealed class PolygonOccluderSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Getting all polygons (in world coords) that are in presented bounding box (AABB)
    /// </summary>
    public List<Vector2[]> GetWorldPolygonsInBounds(Box2Rotated worldBounds, Vector2 watcherPos)
    {
        var result = new List<Vector2[]>();
        var distances = new List<float>();
        var query = EntityQueryEnumerator<PolygonOccluderComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var occluder, out var xform))
        {
            if (!occluder.Enabled)
            {
                continue;
            }

            // Rough check to skip invisible polygons
            var worldEntityPos = _transform.GetWorldPosition(uid);
            var currentDistance = (worldEntityPos - watcherPos).Length();
            if (!worldBounds.Contains(worldEntityPos))
            {
                continue;
            }

            // Trasforming local coords to world coords for polygon
            var worldPoints = new Vector2[occluder.LocalVertices.Count];
            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);

            for (int i = 0; i < occluder.LocalVertices.Count; i++)
            {
                var rotated = worldRot.RotateVec(occluder.LocalVertices[i]);
                worldPoints[i] = worldPos + rotated;
            }

            var elementCount = distances.Count;
            // Insert sorting by distance, so closer shadows will overdraw the further ones
            if (elementCount == 0 || distances[elementCount - 1] >= currentDistance)
            {
                distances.Add(currentDistance);
                result.Add(worldPoints);
            }
            else if (distances[0] <= currentDistance)
            {
                distances.Insert(0, currentDistance);
                result.Insert(0, worldPoints);
            }
            else
            {
                for (int i = 0; i < elementCount; i++)
                {
                    if (distances[i] <= currentDistance)
                    {
                        distances.Insert(i, currentDistance);
                        result.Insert(i, worldPoints);
                        break;
                    }
                }
            }
        }

        return result;
    }
}
