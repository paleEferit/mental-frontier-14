using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client._Mental.Occlusion;

public sealed class PolygonShadowOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private static readonly ProtoId<ShaderPrototype> Shader = "EraserShader";
    private ShaderInstance? _eraserShader = null;
    private PolygonOccluderSystem? _occluderSystem = null;

    // Defining the layer of shadow drawing. 
    // WorldSpace should work.
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public PolygonShadowOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_occluderSystem is null && !_entManager.TrySystem(out _occluderSystem))
        {
            return false;
        }
        if (_eraserShader is null)
        {
            ShaderPrototype? shaderProto = null;
            if (!_prototypeManager.TryIndex<ShaderPrototype>(Shader, out shaderProto))
            {
                return false;
            }
            _eraserShader = shaderProto.InstanceUnique();
        }
        return _occluderSystem is not null && _eraserShader is not null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        // Нам нужна позиция игрока, от которого строятся тени
        var localPlayer = _playerManager.LocalEntity;
        if (_occluderSystem is null)
        {
            return;
        }

        if (_eraserShader is null)
        {
            return;
        }

        if (localPlayer == null)
        {
            return;
        }

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        if (!xformQuery.TryGetComponent(localPlayer.Value, out var playerXform)) return;

        var playerWorldPos = _entManager.System<TransformSystem>().GetWorldPosition(playerXform);

        // Getting screen bounds in world coordinates
        var worldBounds = args.WorldBounds;
        var polygons = _occluderSystem.GetWorldPolygonsInBounds(worldBounds);

        var handle = args.WorldHandle;
        handle.UseShader(null);

        // Shadow color with transpacrency
        var shadowColor = Color.Black.WithAlpha(1.0f);
        // Empty color to override the zone
        var emptyColor = Color.White.WithAlpha(1.0f);
        // Length of shadow ray (should go outside screen)
        float shadowLength = 30f;

        foreach (var polygon in polygons)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                // Getting current polygon edge
                Vector2 p1 = polygon[i];
                Vector2 p2 = polygon[(i + 1) % polygon.Count];

                // Check if edge is facing the player.If it is, it should drop shadow.
                Vector2 edge = p2 - p1;
                Vector2 normal = new Vector2(-edge.Y, edge.X).Normalized(); // Edge normal
                Vector2 toPlayer = (p1 - playerWorldPos).Normalized();

                // If normal looks away from the player, edge should drop shadow
                if (Vector2.Dot(normal, toPlayer) < 0)
                {
                    // Calculation shadow directions from edge vertices
                    Vector2 dir1 = (p1 - playerWorldPos).Normalized() * shadowLength;
                    Vector2 dir2 = (p2 - playerWorldPos).Normalized() * shadowLength;

                    Vector2 p1Shadow = p1 + dir1;
                    Vector2 p2Shadow = p2 + dir2;

                    // Making a shadow volume
                    var verticesBase = new[]
                    {
                        p1, p2, p2Shadow,
                        p1, p2Shadow, p1Shadow
                    };

                    // Drawing shadow polygon
                    handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, verticesBase, shadowColor);
                }
            }
            // Drawing empty pixels to override object zone
            handle.UseShader(_eraserShader);
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, polygon, emptyColor);
            handle.UseShader(null);
        }
    }
}
