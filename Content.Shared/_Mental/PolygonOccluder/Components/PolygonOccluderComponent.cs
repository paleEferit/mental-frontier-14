using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared._Mental.Occlusion;

[RegisterComponent, NetworkedComponent]
public sealed partial class PolygonOccluderComponent : Component
{
    /// <summary>
    /// Local coords of polygon relative to center of the entity
    /// Set clock-wise
    /// </summary>
    [DataField("vertices", required: true)]
    public List<Vector2> LocalVertices = new List<Vector2>();

    /// <summary>
    /// Occlusion activity flag
    /// </summary>
    [DataField("enabled")]
    public bool Enabled = true;
}
