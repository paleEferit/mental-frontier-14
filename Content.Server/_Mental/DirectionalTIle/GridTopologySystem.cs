using Content.Shared._Mental.DirectionalTile;
using Content.Shared.Construction.Conditions;
using Content.Shared.Decals;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;

namespace Content.Server._Mental.DirectionalTile;
public sealed class GridTopologySystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    private readonly Dictionary<string, DirectionFlag> _tilesWithDirections = new();
    private ISawmill _sawmill = default!;
    /// <summary>
    /// Recursion detection to avoid splitting while handling an existing split
    /// </summary>
    private bool _isSplitting = false;
    internal bool SplitAllowed = true;
    private HashSet<EntityUid> _entSet = new();
    private EntityQuery<PhysicsComponent> _bodyQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("GridTopologySystem");
        _bodyQuery = GetEntityQuery<PhysicsComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
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

            CheckSplits(gridUid, grid, gridConnectivity, indices);
        }
    }

    /// <summary>
    /// Replicates engine-style grid split checks incorporating directional tile blockages.
    /// </summary>
    public void CheckSplits(
        EntityUid gridUid,
        MapGridComponent grid,
        GridConnectivityComponent connectivity,
        Vector2i pos)
    {
        _sawmill.Debug("Calling CheckSplits for gird {0} at ({1}; {2})", gridUid, pos.X, pos.Y);
        if (_isSplitting || !SplitAllowed ||
               !grid.CanSplit)
        {
            _sawmill.Debug("splid check denied. _isSplitting: {0}; SplitAllowed: {1}; can grid split {2}", _isSplitting, SplitAllowed, grid.CanSplit);
            return;
        }
        _sawmill.Debug("proceeding with split check");
        _isSplitting = true;
        Span<Direction> directions = stackalloc Direction[]
        {
            Direction.North, Direction.South, Direction.East, Direction.West
        };

        // 1. Gather valid adjacent neighbor tiles
        var neighborSeeds = new List<Vector2i>();
        foreach (var dir in directions)
        {
            var neighborPos = pos.Offset(dir);
            if (_mapSystem.TryGetTileRef(gridUid, grid, neighborPos, out var tile) && !tile.Tile.IsEmpty)
            {
                // Verify directional connection is blocked between pos and neighborPos
                if (IsConnectionBlocked(connectivity, pos, neighborPos))
                {
                    neighborSeeds.Add(neighborPos);
                }
            }
        }
        neighborSeeds.Add(pos);
        _sawmill.Debug("neighbor seeds found: {0}", neighborSeeds.Count);
        // Early exit
        if (neighborSeeds.Count < 2)
        {
            _isSplitting = false;
            return;
        }

        // 2. Perform graph traversal (Flood Fill) considering custom directional rules
        var subgraphs = FindDisconnectedSubgraphs(gridUid, grid, connectivity, neighborSeeds);
        _sawmill.Debug("subgrapghs found: {0}", subgraphs.Count);

        // If all seeds belong to a single connected subgraph, no split occurred
        if (subgraphs.Count <= 1)
        {
            _isSplitting = false;
            return;
        }

        // 3. Grid Partitioning:
        // Keep the largest partition on the existing grid to minimize entity movement overhead
        subgraphs.Sort((a, b) => b.Count.CompareTo(a.Count));

        var newGrids = new EntityUid[subgraphs.Count - 1];

        for (var i = 1; i < subgraphs.Count; i++)
        {
            _sawmill.Debug("calling split for grid: {0} of size {1}", gridUid, subgraphs[i].Count);
            var newGridUid = ExecuteGridSplit(gridUid, grid, subgraphs[i]);
            _sawmill.Debug("split finished, got new grid {0} from grid {1}", newGridUid, gridUid);
            newGrids[i - 1] = newGridUid;
            var splitEvent = new PostGridSplitEvent(gridUid, newGridUid);
            RaiseLocalEvent(gridUid, ref splitEvent, true);
        }
        var ev = new GridSplitEvent(newGrids, gridUid);
        RaiseLocalEvent(gridUid, ref ev, true);
        _isSplitting = false;
    }

    private List<HashSet<Vector2i>> FindDisconnectedSubgraphs(
        EntityUid gridUid,
        MapGridComponent grid,
        GridConnectivityComponent connectivity,
        List<Vector2i> seedNodes)
    {
        _sawmill.Debug("called FindDisconnectedSubgraphs for grid: {0} and node count of {1}", gridUid, seedNodes.Count);
        var subgraphs = new List<HashSet<Vector2i>>();
        var unvisitedSeeds = new HashSet<Vector2i>(seedNodes);
        var globalVisited = new HashSet<Vector2i>();

        Span<Direction> directions = stackalloc Direction[]
        {
            Direction.North, Direction.South, Direction.East, Direction.West
        };

        while (unvisitedSeeds.Count > 0)
        {
            var start = unvisitedSeeds.First();
            var currentGroup = new HashSet<Vector2i>();
            var queue = new Queue<Vector2i>();

            queue.Enqueue(start);
            globalVisited.Add(start);
            currentGroup.Add(start);

            while (queue.TryDequeue(out var current))
            {
                unvisitedSeeds.Remove(current);

                foreach (var dir in directions)
                {
                    var next = current.Offset(dir);

                    if (globalVisited.Contains(next))
                    {
                        continue;
                    }

                    if (!_mapSystem.TryGetTileRef(gridUid, grid, next, out var tileRef) || tileRef.Tile.IsEmpty)
                    {
                        continue;
                    }

                    // Directional connection check between 'current' and 'next'
                    if (IsConnectionBlocked(connectivity, current, next))
                    {
                        continue;
                    }

                    globalVisited.Add(next);
                    currentGroup.Add(next);
                    queue.Enqueue(next);
                }
            }

            subgraphs.Add(currentGroup);
        }

        return subgraphs;
    }

    // Sadly can't use one in the engine, so made this
    private bool ReAnchor(
        EntityUid uid,
        TransformComponent xform,
        MapGridComponent oldGrid,
        MapGridComponent newGrid,
        Vector2i oldTilePos,
        Vector2i tilePos,
        EntityUid oldGridUid,
        EntityUid newGridUid,
        Angle rotation)
    {
        var oldRot = xform.LocalRotation;
        _transformSystem.Unanchor(uid);
        _mapSystem.RemoveFromSnapGridCell(oldGridUid, oldGrid, oldTilePos, uid);
        _transformSystem.SetParent(uid, newGridUid);
        //_mapSystem.AddToSnapGridCell(newGridUid, newGrid, tilePos, uid);
        //var oldPos = xform.LocalPosition;
        //var oldMap = xform.MapUid;
        _transformSystem.SetLocalPosition(uid, tilePos + newGrid.TileSizeHalfVector);
        _transformSystem.SetLocalRotation(uid, oldRot + rotation);
        Entity<TransformComponent> entityToAnchor = (uid, xform);
        Entity<MapGridComponent> mapForAnchoring = (newGridUid, newGrid);
        //_transformSystem.AnchorEntity(uid);
        var result = _transformSystem.AnchorEntity(entityToAnchor, mapForAnchoring, tilePos);

        var meta = MetaData(uid);

        Dirty(uid, xform, meta);
        return result;
    }

    /// <summary>
    /// Creates a new grid entity and transfers the disconnected partition to it.
    /// </summary>
    private EntityUid ExecuteGridSplit(EntityUid sourceGridUid, MapGridComponent sourceGrid, HashSet<Vector2i> tilesToMove)
    {
        _sawmill.Debug("Calling ExecuteGridSplit for {0} with tiles to move count {1}", sourceGridUid, tilesToMove.Count);
        var xform = Transform(sourceGridUid);

        // Create new grid at the exact transform of the parent grid
        var newGridUid = _mapManager.CreateGridEntity(xform.MapID);
        var newGridXform = _xformQuery.GetComponent(newGridUid);
        var newGridComp = _gridQuery.GetComponent(newGridUid);
        var mapBody = _bodyQuery.GetComponent(sourceGridUid);
        var sourceGridXform = _xformQuery.GetComponent(sourceGridUid);

        _transformSystem.SetWorldPosition(newGridUid, _transformSystem.GetWorldPosition(sourceGridUid));
        _transformSystem.SetWorldRotation(newGridUid, _transformSystem.GetWorldRotation(sourceGridUid));
        var splitBody = _bodyQuery.GetComponent(newGridUid);
        _physics.SetLinearVelocity(newGridUid, mapBody.LinearVelocity, body: splitBody);
        _physics.SetAngularVelocity(newGridUid, mapBody.AngularVelocity, body: splitBody);

        // Prepare batch tile changes
        var tilesToSet = new List<(Vector2i, Tile)>(tilesToMove.Count);
        var tilesToClear = new List<(Vector2i, Tile)>(tilesToMove.Count);

        foreach (var pos in tilesToMove)
        {
            if (_mapSystem.TryGetTileRef(sourceGridUid, sourceGrid, pos, out var tileRef))
            {
                tilesToSet.Add((pos, tileRef.Tile));
                tilesToClear.Add((pos, Tile.Empty));
            }
        }

        // Apply batch operation add
        _mapSystem.SetTiles(newGridUid, newGridComp, tilesToSet);

        // Move all entities
        foreach (var tile in tilesToMove)
        {
            var tilePos = tile;

            Entity<MapGridComponent> sourceGridEntity = (sourceGridUid, sourceGrid);

            var snapgrid = _mapSystem.GetAnchoredEntities(sourceGridEntity, tilePos);
            //var snapgrid = node.Group.Chunk.GetSnapGrid((ushort)tile.X, (ushort)tile.Y);
            var snapgridCount = snapgrid == null ? 0 : snapgrid.Count();
            if (snapgrid != null && snapgridCount != 0)
            {
                _sawmill.Debug("got anchored entities to move: {0}", snapgridCount);
                for (var j = snapgridCount - 1; j >= 0; j--)
                {
                    var ent = snapgrid.ElementAt(j);
                    if (Exists(ent))
                    {
                        var entXform = _xformQuery.GetComponent(ent);
                        _sawmill.Debug("reanchoring entity {0} from gird {1} to grid {2}", ent, sourceGridUid, newGridUid);
                        var reacbchoringResult = ReAnchor(ent, entXform,
                            sourceGrid, newGridComp,
                            tilePos, tilePos,
                            sourceGridUid, newGridUid,
                            Angle.Zero);
                        _sawmill.Debug("reanchoring result entity {0} from gird {1} to grid {2} is {3}. Entity anchored is {4}", ent, sourceGridUid, newGridUid, reacbchoringResult, xform.Anchored);
                        //DebugTools.Assert(xform.Anchored);
                    }
                    else
                    {
                        _sawmill.Debug("tried reanchoring entity {0} from gird {1} to grid {2} while it DOES NOT EXIST", ent, sourceGridUid, newGridUid);
                    }
                }
            }
            else
            {
                _sawmill.Debug("got no anchored entities to move");
            }

            var bounds = _lookup.GetLocalBounds(tilePos, sourceGrid.TileSize);
            _entSet.Clear();
            _lookup.GetLocalEntitiesIntersecting(sourceGridUid, tilePos, _entSet, 0f, LookupFlags.All | ~LookupFlags.Uncontained | LookupFlags.Approximate);
            _sawmill.Debug("got intersection entitites to move: {0}", _entSet.Count);
            foreach (var ent in _entSet)
            {
                // Consider centre of entity position maybe?
                var entXform = _xformQuery.GetComponent(ent);

                if (entXform.ParentUid != sourceGridUid ||
                    !bounds.Contains(entXform.LocalPosition))
                {
                    continue;
                }
                _sawmill.Debug("moving entity {0} to new grid {1}", ent, newGridUid);
                _transformSystem.SetParent(ent, entXform, newGridUid, _xformQuery, newGridXform);
            }
        }

        // Apply batch operation remove
        _mapSystem.SetTiles(sourceGridUid, sourceGrid, tilesToClear);



        // Ensure new grid has the connectivity component attached
        var newConnectivity = EnsureComp<GridConnectivityComponent>(newGridUid);

        // Copy relevant directional tile data to the new grid's component
        foreach (var pos in tilesToMove)
        {
            if (TryComp<GridConnectivityComponent>(sourceGridUid, out var oldConn) &&
                oldConn.DirectionalTiles.TryGetValue(pos, out var flag))
            {
                newConnectivity.DirectionalTiles[pos] = flag;
                oldConn.DirectionalTiles.Remove(pos);
            }
        }

        return newGridUid;
    }

    private bool IsConnectionBlocked(
        GridConnectivityComponent comp,
        Vector2i tileFromPos,
        Vector2i tileToPos)
    {
        _sawmill.Debug("called IsConnectionBlocked for ({0}; {1}) to ({2}; {3})", tileFromPos.X, tileFromPos.Y, tileToPos.X, tileToPos.Y);
        if (comp.DirectionalTiles.TryGetValue(tileFromPos, out var fromFlags))
        {
            var dir = GetDirectionFlag(tileFromPos, tileToPos);
            if (fromFlags.HasFlag(dir))
            {
                _sawmill.Debug("IsConnectionBlocked returns true (1)");
                return true;
            }
        }

        if (comp.DirectionalTiles.TryGetValue(tileToPos, out var toFlags))
        {
            var dir = GetDirectionFlag(tileToPos, tileFromPos);
            if (toFlags.HasFlag(dir))
            {
                _sawmill.Debug("IsConnectionBlocked returns true (2)");
                return true;
            }
        }

        _sawmill.Debug("IsConnectionBlocked returns false");
        return false;
    }
}
