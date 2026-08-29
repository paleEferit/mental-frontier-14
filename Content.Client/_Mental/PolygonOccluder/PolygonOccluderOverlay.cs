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
    [Dependency] private readonly IClyde _clyde = default!;
    private static readonly ProtoId<ShaderPrototype> Shader = "MaskCutShader";
    private ShaderInstance? _maskCutShader = null;
    private PolygonOccluderSystem? _occluderSystem = null;
    private IRenderTexture? _shadowBuffer;
    // Shadow color with transpacrency
    private static readonly Color ShadowColor = Color.Black.WithAlpha(1.0f);
    // Empty color to override the zone
    private static readonly Color EmptyColor = Color.White.WithAlpha(1.0f);
    // Length of shadow ray (should go outside screen)
    private static readonly float ShadowLength = 30f;

    // Defining the layer of shadow drawing. 
    // WorldSpace should work.
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    // Will need screen texture for shader
    public override bool RequestScreenTexture => true;

    public PolygonShadowOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    private Vector2[] ConvertFromWorldToLocal(Vector2[] input, IClydeViewport viewport)
    {
        Vector2[] result = new Vector2[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            result[i] = viewport.WorldToLocal(input[i]);
        }
        return result;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_occluderSystem is null && !_entManager.TrySystem(out _occluderSystem))
        {
            return false;
        }
        if (_maskCutShader is null)
        {
            ShaderPrototype? shaderProto = null;
            if (!_prototypeManager.TryIndex<ShaderPrototype>(Shader, out shaderProto))
            {
                return false;
            }
            _maskCutShader = shaderProto.InstanceUnique();
        }
        return _occluderSystem is not null && _maskCutShader is not null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        // Нам нужна позиция игрока, от которого строятся тени
        var localPlayer = _playerManager.LocalEntity;
        if (ScreenTexture == null)
        {
            return;
        }
        if (_occluderSystem is null)
        {
            return;
        }

        if (_maskCutShader is null)
        {
            return;
        }

        if (localPlayer == null)
        {
            return;
        }

        var viewport = args.Viewport;
        var viewportSize = args.Viewport.Size;
        if (_shadowBuffer?.Texture.Size != viewportSize)
        {
            _shadowBuffer?.Dispose();
            _shadowBuffer = _clyde.CreateRenderTarget(viewportSize, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "polygon-occluder-shadow-buffer");
        }

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        if (!xformQuery.TryGetComponent(localPlayer.Value, out var playerXform)) return;

        var playerWorldPos = _entManager.System<TransformSystem>().GetWorldPosition(playerXform);

        // Getting screen bounds in world coordinates
        var worldBounds = args.WorldBounds;
        var polygons = _occluderSystem.GetWorldPolygonsInBounds(worldBounds, playerWorldPos);

        var handle = args.WorldHandle;
        handle.UseShader(null);

        // Drawing a shadow layer to buffer
        handle.RenderInRenderTarget(_shadowBuffer!, () =>
        {
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
                        Vector2 dir1 = (p1 - playerWorldPos).Normalized() * ShadowLength;
                        Vector2 dir2 = (p2 - playerWorldPos).Normalized() * ShadowLength;

                        Vector2 p1Shadow = p1 + dir1;
                        Vector2 p2Shadow = p2 + dir2;

                        // Making a shadow volume
                        var verticesBase = new[]
                        {
                            p1, p2, p2Shadow,
                            p1, p2Shadow, p1Shadow
                        };

                        // Drawing shadow polygon
                        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, this.ConvertFromWorldToLocal(verticesBase, viewport), ShadowColor);
                    }
                }
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, this.ConvertFromWorldToLocal(polygon.ToArray(), viewport), EmptyColor);
            }
        },
           Color.Transparent);
        // Updating shader params
        _maskCutShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _maskCutShader.SetParameter("maskTexture", _shadowBuffer!.Texture);

        // Drawing to trigger shader
        handle.UseShader(_maskCutShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
