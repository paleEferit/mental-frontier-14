using Robust.Shared.GameStates;

namespace Content.Shared._Mental.DirectionalTile;

[RegisterComponent, NetworkedComponent]
public sealed partial class GridConnectivityComponent : Component
{
    /// <summary>
    /// Stores all directional tiles on grid
    /// </summary>
    public readonly Dictionary<Vector2i, DirectionFlag> DirectionalTiles = new();
}
