using System;
using System.Collections;
using System.Collections.Generic;
using MapMono;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    public MapHandler mapHandler;
    public VisionMemory visionMemory;

    public int viewRadius;

    public Dictionary<Vector2Int, bool> visibleTiles = new Dictionary<Vector2Int, bool>();
    public List<Shadow> shadows = new List<Shadow>();

    public bool drawVisible = true;
    public bool drawShadows = true;


    public void Start()
    {
        mapHandler.mapDataReal.fieldsOfView.Add(gameObject.GetInstanceID(),this);
    }

    public void OnDrawGizmos()
    {
        if (!drawVisible && !drawShadows) return;

        // draw the visible tiles
        foreach (KeyValuePair<Vector2Int, bool> tile in visibleTiles)
        {
            if (tile.Value)
            {
                if (!drawVisible) continue;
                Gizmos.color = Color.green;
            }
            else
            {
                if (!drawShadows) continue;
                Gizmos.color = Color.red;
            }

            // convert position to world position
            Vector3 worldPos = mapHandler.GetWorldPosition(tile.Key.x, tile.Key.y);
            Gizmos.DrawCube(worldPos, Vector3.one / 2);
        }

        // // draw the shadows
        // foreach (Shadow shadow in shadows)
        // {
        //     Gizmos.color = Color.black;
        //     var currentGridPos = mapHandler.GetGlobalPosAsGridPos(transform.position);
        //     var currentPos = mapHandler.GetWorldPosition(currentGridPos);
        //     var start = mapHandler.GetWorldPosition(currentGridPos +
        //                                             TransformOctant(shadow.startPos.x, shadow.startPos.y,
        //                                                 shadow.octant));
        //     var end = mapHandler.GetWorldPosition(currentGridPos +
        //                                           TransformOctant(shadow.endPos.x, shadow.endPos.y, shadow.octant));
        //
        //     // draw a line from the start to the end
        //     Gizmos.DrawLine(start + Vector3.up * 3, end + Vector3.up * 4);
        // }
    }


    public void VisibilityUpdate()
    {
        // get the current transform as a Vector2Int from mapHandler
        Vector2Int currentPos = mapHandler.GetGlobalPosAsGridPos(transform.position);

        RefreshVisibility(currentPos);
    }

    /// Updates the visible flags
    private void RefreshVisibility(Vector2Int pos)
    {
        visibleTiles.Clear();
        var allShadows = new List<Shadow>();
        // Sweep through the octants.
        for (int octant = 0; octant < 8; octant++)
        {
            var shadows = RefreshOctant(pos, octant, viewRadius);
            allShadows.AddRange(shadows);
        }

        shadows = allShadows;
        
    }

    private List<Shadow> RefreshOctant(Vector2Int start, int octant, int maxRows = 99)
    {
        var line = new ShadowLine();
        var fullShadow = false;
        var maxDstSqr = maxRows * maxRows;

        // Sweep through the rows ('rows' may be vertical or horizontal based on
        // the incrementors). Start at row 1 to skip the center position.
        for (var row = 1; row < maxRows; row++)
        {
            var rowSquare = row * row;
            //If we've gone out of bounds, bail.
            if (!mapHandler.IsInBounds(start + TransformOctant(row, 0, octant))) break;

            for (var col = 0; col <= row; col++)
            {
                var colSquare = col * col;
                var pos = start + TransformOctant(row, col, octant);

                // If we've traversed out of bounds, bail on this row.
                // note: this improves performance, but works on the assumption that
                // the starting tile of the FOV is in bounds.
                if (!mapHandler.IsInBounds(pos)) break;

                // If we know the entire row is in shadow, we don't need to be more
                // specific.
                if (fullShadow || (rowSquare + colSquare) > maxDstSqr)
                {
                    visibleTiles[pos] = false;
                    continue;
                }
                else
                {
                    var projection = ProjectTile(row, col, octant);

                    // Set the visibility of this tile.
                    var visible = !line.IsInShadow(projection);
                    visibleTiles[pos] = visible;

                    // add any opaque tiles to the shadow map
                    if (visible && mapHandler.BlocksVision(pos))
                    {
                        line.Add(projection);
                        fullShadow = line.IsFullShadow();
                    }
                }
            }
        }

        return line._shadows;
    }

    private Vector2Int TransformOctant(int row, int col, int octant)
    {
        switch (octant)
        {
            case 0: return new Vector2Int(col, -row);
            case 1: return new Vector2Int(row, -col);
            case 2: return new Vector2Int(row, col);
            case 3: return new Vector2Int(col, row);
            case 4: return new Vector2Int(-col, row);
            case 5: return new Vector2Int(-row, col);
            case 6: return new Vector2Int(-row, -col);
            case 7: return new Vector2Int(-col, -row);
            default: throw new ArgumentOutOfRangeException("octant", "Octant must be between 0 and 7.");
        }
    }

    private Vector3 TransformOctant(float row, float col, int octant)
    {
        switch (octant)
        {
            case 0: return new Vector3(col, -row);
            case 1: return new Vector3(row, -col);
            case 2: return new Vector3(row, col);
            case 3: return new Vector3(col, row);
            case 4: return new Vector3(-col, row);
            case 5: return new Vector3(-row, col);
            case 6: return new Vector3(-row, -col);
            case 7: return new Vector3(-col, -row);
            default: throw new ArgumentOutOfRangeException("octant", "Octant must be between 0 and 7.");
        }
    }


    /// Creates a [Shadow] that corresponds to the projected silhouette of the
    /// given tile. This is used both to determine visibility (if any of the
    /// projection is visible, the tile is) and to add the tile to the shadow map.
    ///
    /// The maximal projection of a square is always from the two opposing
    /// corners. From the perspective of octant zero, we know the square is
    /// above and to the right of the viewpoint, so it will be the top left and
    /// bottom right corners.
    private Shadow ProjectTile(int row, int col, int octant)
    {
        // calculate the slope of the top left and bottom right corners
        // offsetInTile is the offset of the player in the current tile
        // row is the grid row of the tile, col is the grid column of the tile


        var topLeft = (col - 0.5f) / (row + 0.5f);
        var bottomRight = (col + 0.5f) / (row - 0.5f);


        // this line also had problems.
        // I'm still not completely sure if the startPos and endPos are correct.
        return new Shadow(topLeft, bottomRight,
            new Vector2Int(row, col), new Vector2Int(row, col), octant);
    }

    class ShadowLine
    {
        public List<Shadow> _shadows = new List<Shadow>();

        public bool IsInShadow(Shadow projection)
        {
            // Check the shadow list
            foreach (var shadow in _shadows)
            {
                if (shadow.Contains(projection)) return true;
            }

            return false;
        }

        /// Add [shadow] to the list of non-overlapping shadows. May merge one or
        /// more shadows.
        public void Add(Shadow shadow)
        {
            // Figure out where to slot the new shadow in the sorted list.
            var index = 0;
            for (; index < _shadows.Count; index++)
            {
                // Stop when we hit the insertion point.
                if (_shadows[index].start >= shadow.start) break;
            }

            // The new shadow is going here. See if it overlaps the previous or next.
            Shadow overlappingPrevious = null;
            if (index > 0 && _shadows[index - 1].end > shadow.start)
            {
                overlappingPrevious = _shadows[index - 1];
            }

            Shadow overlappingNext = null;
            if (index < _shadows.Count && _shadows[index].start < shadow.end)
            {
                overlappingNext = _shadows[index];
            }

            // Insert and unify with overlapping shadows.
            if (overlappingNext != null)
            {
                if (overlappingPrevious != null)
                {
                    // Overlaps both, so unify one and delete the other.
                    overlappingPrevious.end = overlappingNext.end;
                    overlappingPrevious.endPos = overlappingNext.endPos;
                    _shadows.RemoveAt(index);
                }
                else
                {
                    // Only overlaps the next shadow, so unify it with that.
                    overlappingNext.start = shadow.start;
                    overlappingNext.startPos = shadow.startPos;
                }
            }
            else
            {
                if (overlappingPrevious != null)
                {
                    // Only overlaps the previous shadow, so unify it with that.
                    overlappingPrevious.end = shadow.end;
                    overlappingPrevious.endPos = shadow.endPos;
                }
                else
                {
                    // Does not overlap anything, so insert.
                    _shadows.Insert(index, shadow);
                }
            }
        }

        public bool IsFullShadow()
        {
            return _shadows.Count == 1 &&
                   _shadows[0].start == 0 &&
                   _shadows[0].end == 1;
        }
    }

    /// Represents the 1D projection of a 2D shadow onto a normalized line. In
    /// other words, a range from 0.0 to 1.0.
    public class Shadow
    {
        public double start;
        public double end;

        public Vector2Int startPos;
        public Vector2Int endPos;

        public int octant;

        public Shadow(double start, double end, Vector2Int startPos, Vector2Int endPos, int octant)
        {
            this.start = start;
            this.end = end;
            this.startPos = startPos;
            this.endPos = endPos;
            this.octant = octant;
        }

        public override string ToString()
        {
            return $"Shadow: {start} - {end}";
        }

        /// Returns `true` if [other] is completely covered by this shadow.
        public bool Contains(Shadow other)
        {
            return start <= other.start && end >= other.end;
        }
    }

    public void PushUpdate()
    {
        if (visionMemory != null)
        {
            visionMemory.UpdateVisible(visibleTiles);
        }
    }

    public void Compose(FieldOfView friendFov)
    {
        if (friendFov == null) return;

        foreach (var tile in friendFov.visibleTiles)
        {
            if (visibleTiles.ContainsKey(tile.Key))
            {
                visibleTiles[tile.Key] = tile.Value || visibleTiles[tile.Key];
            }
            else
            {
                visibleTiles.Add(tile.Key, tile.Value);
            }
        }

        foreach (var tile in visibleTiles)
        {
            if (friendFov.visibleTiles.ContainsKey(tile.Key))
            {
                friendFov.visibleTiles[tile.Key] = tile.Value || friendFov.visibleTiles[tile.Key];
            }
            else
            {
                friendFov.visibleTiles.Add(tile.Key, tile.Value);
            }
            
        }
    }
}