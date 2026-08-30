using Content.Shared.Construction.Conditions;
using Content.Shared.Decals;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using System.Numerics;
using System.Reflection.Metadata;

namespace Content.Shared._Mental.DirectionalTile;
public sealed class SharedGridTopologySystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private readonly Dictionary<string, DirectionFlag> _tilesWithDirections = new();
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("SharedGridTopologySystem");
        //Adding directions to the dictionary since there is no prototype-level access to tiles. Yeah, I know, a lot of tiles
        _tilesWithDirections.Add("LatticeHalfH", DirectionFlag.South | DirectionFlag.North);
        _tilesWithDirections.Add("PlatingHalfH", DirectionFlag.South | DirectionFlag.North);
        _tilesWithDirections.Add("LatticeHalfTiltNESLower", DirectionFlag.West);
        _tilesWithDirections.Add("PlatingHalfTiltNESLower", DirectionFlag.West);
        _tilesWithDirections.Add("LatticeHalfTiltNESUpper", DirectionFlag.West | DirectionFlag.South);
        _tilesWithDirections.Add("PlatingHalfTiltNESUpper", DirectionFlag.West | DirectionFlag.South);
        _tilesWithDirections.Add("LatticeHalfTiltNEWLower", DirectionFlag.South);
        _tilesWithDirections.Add("PlatingHalfTiltNEWLower", DirectionFlag.South);
        _tilesWithDirections.Add("LatticeHalfTiltNEWUpper", DirectionFlag.West | DirectionFlag.South);
        _tilesWithDirections.Add("PlatingHalfTiltNEWUpper", DirectionFlag.West | DirectionFlag.South);
        _tilesWithDirections.Add("LatticeHalfTiltNWELower", DirectionFlag.South);
        _tilesWithDirections.Add("PlatingHalfTiltNWELower", DirectionFlag.South);
        _tilesWithDirections.Add("LatticeHalfTiltNWEUpper", DirectionFlag.East | DirectionFlag.South);
        _tilesWithDirections.Add("PlatingHalfTiltNWEUpper", DirectionFlag.East | DirectionFlag.South);
        _tilesWithDirections.Add("LatticeHalfTiltNWSLower", DirectionFlag.East);
        _tilesWithDirections.Add("PlatingHalfTiltNWSLower", DirectionFlag.East);
        _tilesWithDirections.Add("LatticeHalfTiltNWSUpper", DirectionFlag.East | DirectionFlag.South);
        _tilesWithDirections.Add("PlatingHalfTiltNWSUpper", DirectionFlag.East | DirectionFlag.South);
        _tilesWithDirections.Add("LatticeHalfTiltSENLower", DirectionFlag.West);
        _tilesWithDirections.Add("PlatingHalfTiltSENLower", DirectionFlag.West);
        _tilesWithDirections.Add("LatticeHalfTiltSENUpper", DirectionFlag.West | DirectionFlag.North);
        _tilesWithDirections.Add("PlatingHalfTiltSENUpper", DirectionFlag.West | DirectionFlag.North);
        _tilesWithDirections.Add("LatticeHalfTiltSEWLower", DirectionFlag.North);
        _tilesWithDirections.Add("PlatingHalfTiltSEWLower", DirectionFlag.North);
        _tilesWithDirections.Add("LatticeHalfTiltSEWUpper", DirectionFlag.West | DirectionFlag.North);
        _tilesWithDirections.Add("PlatingHalfTiltSEWUpper", DirectionFlag.West | DirectionFlag.North);
        _tilesWithDirections.Add("LatticeHalfTiltSWELower", DirectionFlag.North);
        _tilesWithDirections.Add("PlatingHalfTiltSWELower", DirectionFlag.North);
        _tilesWithDirections.Add("LatticeHalfTiltSWEUpper", DirectionFlag.East | DirectionFlag.North);
        _tilesWithDirections.Add("PlatingHalfTiltSWEUpper", DirectionFlag.East | DirectionFlag.North);
        _tilesWithDirections.Add("LatticeHalfTiltSWNLower", DirectionFlag.East);
        _tilesWithDirections.Add("PlatingHalfTiltSWNLower", DirectionFlag.East);
        _tilesWithDirections.Add("LatticeHalfTiltSWNUpper", DirectionFlag.East | DirectionFlag.North);
        _tilesWithDirections.Add("PlatingHalfTiltSWNUpper", DirectionFlag.East | DirectionFlag.North);
        _tilesWithDirections.Add("LatticeHalfV", DirectionFlag.East | DirectionFlag.West);
        _tilesWithDirections.Add("PlatingHalfV", DirectionFlag.East | DirectionFlag.West);
        _tilesWithDirections.Add("LatticeWedgeE", DirectionFlag.West | DirectionFlag.South | DirectionFlag.North);
        _tilesWithDirections.Add("PlatingWedgeE", DirectionFlag.West | DirectionFlag.South | DirectionFlag.North);
        _tilesWithDirections.Add("LatticeWedgeW", DirectionFlag.East | DirectionFlag.South | DirectionFlag.North);
        _tilesWithDirections.Add("PlatingWedgeW", DirectionFlag.East | DirectionFlag.South | DirectionFlag.North);
        _tilesWithDirections.Add("LatticeWedgeN", DirectionFlag.East | DirectionFlag.West | DirectionFlag.South);
        _tilesWithDirections.Add("PlatingWedgeN", DirectionFlag.East | DirectionFlag.West | DirectionFlag.South);
        _tilesWithDirections.Add("LatticeWedgeS", DirectionFlag.East | DirectionFlag.West | DirectionFlag.North);
        _tilesWithDirections.Add("PlatingWedgeS", DirectionFlag.East | DirectionFlag.West | DirectionFlag.North);
        _tilesWithDirections.Add("LatticeCornerNE", DirectionFlag.West | DirectionFlag.South);
        _tilesWithDirections.Add("PlatingCornerNE", DirectionFlag.West | DirectionFlag.South);
        _tilesWithDirections.Add("LatticeCornerNW", DirectionFlag.East | DirectionFlag.South);
        _tilesWithDirections.Add("PlatingCornerNW", DirectionFlag.East | DirectionFlag.South);
        _tilesWithDirections.Add("LatticeCornerSE", DirectionFlag.West | DirectionFlag.North);
        _tilesWithDirections.Add("PlatingCornerSE", DirectionFlag.West | DirectionFlag.North);
        _tilesWithDirections.Add("LatticeCornerSW", DirectionFlag.East | DirectionFlag.North);
        _tilesWithDirections.Add("PlatingCornerSW", DirectionFlag.East | DirectionFlag.North);
        _tilesWithDirections.Add("LatticeHalfS", DirectionFlag.North);
        _tilesWithDirections.Add("PlatingHalfS", DirectionFlag.North);
        _tilesWithDirections.Add("LatticeHalfN", DirectionFlag.South);
        _tilesWithDirections.Add("PlatingHalfN", DirectionFlag.South);
        _tilesWithDirections.Add("LatticeHalfW", DirectionFlag.East);
        _tilesWithDirections.Add("PlatingHalfW", DirectionFlag.East);
        _tilesWithDirections.Add("LatticeHalfE", DirectionFlag.West);
        _tilesWithDirections.Add("PlatingHalfE", DirectionFlag.West);
        _tilesWithDirections.Add("LatticeQuarterDiagonalNE", DirectionFlag.West | DirectionFlag.South);
        _tilesWithDirections.Add("PlatingQuarterDiagonalNE", DirectionFlag.West | DirectionFlag.South);
        _tilesWithDirections.Add("LatticeQuarterDiagonalNW", DirectionFlag.East | DirectionFlag.South);
        _tilesWithDirections.Add("PlatingQuarterDiagonalNW", DirectionFlag.East | DirectionFlag.South);
        _tilesWithDirections.Add("LatticeQuarterDiagonalSE", DirectionFlag.West | DirectionFlag.North);
        _tilesWithDirections.Add("PlatingQuarterDiagonalSE", DirectionFlag.West | DirectionFlag.North);
        _tilesWithDirections.Add("LatticeQuarterDiagonalSW", DirectionFlag.North | DirectionFlag.East);
        _tilesWithDirections.Add("PlatingQuarterDiagonalSW", DirectionFlag.North | DirectionFlag.East);

        // Subscribe to the global broadcast event for tile changes
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialized);
    }

    // Replicating existing solution to avoid pulling it all into shared system
    private DirectionFlag GetDirectionFlag(Vector2i startingPoint, Vector2i externalPoint)
    {
        if (startingPoint == externalPoint)
        {
            return DirectionFlag.None;
        }
        var diff = externalPoint - startingPoint;
        var vertical = diff.Y > 0 ? DirectionFlag.North : (diff.Y < 0 ? DirectionFlag.South : DirectionFlag.None);
        var horizontal = diff.X > 0 ? DirectionFlag.East : (diff.X < 0 ? DirectionFlag.West : DirectionFlag.None);
        return vertical | horizontal;
    }

    private void OnTileChanged(ref TileChangedEvent ev)
    {
        var gridUid = ev.Entity.Owner;
        foreach (var chagne in ev.Changes)
        {
            var indices = chagne.GridIndices;
            var newTileType = chagne.NewTile;
            var oldTileType = chagne.OldTile;
            // 2. Validate the entity exists and has a map grid component
            if (!TryComp<MapGridComponent>(gridUid, out var grid))
            {
                continue;
            }

            // 3. Implement your Topology Logic here
            // Example: Do nothing if the tile type didn't actually change
            if (newTileType.TypeId == oldTileType.TypeId)
            {
                continue;
            }

            UpdateGridTopology(gridUid, grid, indices, newTileType, oldTileType);
        }
    }

    private void ProcessGridTopology(EntityUid uid)
    {
        _sawmill.Debug("Process Grid Topology called for id {0}", uid);
        EnsureComp<GridConnectivityComponent>(uid);
        if (TryComp<GridConnectivityComponent>(uid, out var gridConnectivity))
        {
            if (TryComp<MapGridComponent>(uid, out var mapGrid))
            {
                var tileRefs = _mapSystem.GetAllTiles(uid, mapGrid);
                var counter = 0;
                foreach (var tileRef in tileRefs)
                {
                    var gridCoords = tileRef.GridIndices;
                    var tile = tileRef.Tile;
                    var typeId = tile.TypeId;
                    var tileDef = _tileDefManager[typeId];
                    string tileTypeId = tileDef.ID;

                    if (_tilesWithDirections.TryGetValue(tileTypeId, out var blockedTileDirections))
                    {
                        gridConnectivity.DirectionalTiles.Add(gridCoords, blockedTileDirections);
                    }
                    counter++;
                }
                _sawmill.Debug("Found and processed {0} tileRefs for grid id {1}", counter, uid);
            }
        }
    }

    private void OnGridInitialized(GridInitializeEvent msg)
    {
        ProcessGridTopology(msg.EntityUid);
    }

    private bool AreTilesConnected(Vector2i tileA, Vector2i tileB, GridConnectivityComponent comp)
    {
        // Check if tile A is in Directional Tile list for the grid and blocks tile B
        if (comp.DirectionalTiles.TryGetValue(tileA, out var blockedA))
        {
            var dir = GetDirectionFlag(tileA, tileB);
            if (blockedA.HasFlag(dir))
            {
                return false;
            }
        }
        // Check if tile B is in Directional Tile list for the grid and blocks tile A
        if (comp.DirectionalTiles.TryGetValue(tileB, out var blockedB))
        {
            var dir = GetDirectionFlag(tileB, tileA);
            if (blockedB.HasFlag(dir))
            {
                return false;
            }
        }
        return true;
    }

    private void UpdateGridTopology(EntityUid gridUid, MapGridComponent grid, Vector2i indices, Tile newTile, Tile oldTile)
    {
        _sawmill.Debug("UpdateGridTopology called for id {0} at position ({1}; {2}), replacing {3} with {4}", gridUid, indices.X, indices.Y, _tileDefManager[oldTile.TypeId].ID, _tileDefManager[newTile.TypeId].ID);
        if (TryComp<GridConnectivityComponent>(gridUid, out var gridConnectivity))
        {
            if (gridConnectivity.DirectionalTiles.ContainsKey(indices))
            {
                _sawmill.Debug("old tile on map {0} at position ({1}; {2}) was directional, cleaning it up", gridUid, indices.X, indices.Y);
                gridConnectivity.DirectionalTiles.Remove(indices);
            }

            if (_tilesWithDirections.TryGetValue(_tileDefManager[newTile.TypeId].ID, out var blockedTileDirections))
            {
                _sawmill.Debug("new tile on map {0} at position ({1}; {2}) is directional {3}, adding it", gridUid, indices.X, indices.Y, blockedTileDirections);
                gridConnectivity.DirectionalTiles.Add(indices, blockedTileDirections);
            }
        }
    }
}
