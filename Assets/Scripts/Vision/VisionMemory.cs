using System;
using System.Collections;
using System.Collections.Generic;
using Knowledge;
using MapMono;
using Unity.VisualScripting;
using UnityEngine;


public class VisionMemory : MonoBehaviour
{
    public bool drawGizmos = true;
    public MapHandler mapHandler;
    public UnitKnowledge unitKnowledge;
    public Dictionary<Vector2Int, TileMemory> tileMemory = new Dictionary<Vector2Int, TileMemory>();
    public List<Vector2Int> lastSeen = new List<Vector2Int>();


    public void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        var currentPos = mapHandler.GetGlobalPosAsGridPos(transform.position);
        Debug.Log($"{gameObject.name}:{currentPos}");
        // draw green for currently seen tiles
        foreach (var tile in tileMemory)
        {
            var tileWorldPos = mapHandler.GetWorldPosition(tile.Key.x, tile.Key.y);
            if (tile.Key == currentPos)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(tileWorldPos, Vector3.one);
            }
            else if (tile.Value.currentlyVisible)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawCube(tileWorldPos, Vector3.one);
            }
            else if (tile.Value.previouslyVisible)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawCube(tileWorldPos, Vector3.one);
            }
            else
            {
                // draw in gray for tiles that have never been seen
                Gizmos.color = Color.red;
                Gizmos.DrawCube(tileWorldPos, Vector3.one);
            }
        }
    }

    public void Start()
    {
        mapHandler.mapDataReal.VisionMemories.Add(gameObject.GetInstanceID(), this);

        ResetMemory();
    }

    public void ResetMemory()
    {
        // create empty tile data objects for all tiles
        tileMemory.Clear();
        for (int x = 0; x < mapHandler.mapSettings.mapSize.x; x++)
        {
            for (int y = 0; y < mapHandler.mapSettings.mapSize.y; y++)
            {
                tileMemory.Add(new Vector2Int(x, y), new TileMemory(0));
            }
        }
    }

    public void UpdateVisible(Dictionary<Vector2Int, bool> visibilityUpdate)
    {
        // current tile is always visible
        var currentTile = mapHandler.GetGlobalPosAsGridPos(transform.position);
        visibilityUpdate[currentTile] = true;
        
        var currentlyVisible = new List<Vector2Int>();

        foreach (var tile in visibilityUpdate)
        {
            if (tile.Value)
            {
                currentlyVisible.Add(tile.Key);
            }
        }

        var newlyVisible = new List<Vector2Int>();
        foreach (var tile in currentlyVisible)
        {
            if (lastSeen.Contains(tile) == false)
            {
                newlyVisible.Add(tile);
            }
        }

        var firstSeen = new List<Vector2Int>();
        foreach (var tile in currentlyVisible)
        {
            if (tileMemory[tile].currentlyVisible == false && tileMemory[tile].previouslyVisible == false)
            {
                firstSeen.Add(tile);
            }
        }

        var lostVisibility = new List<Vector2Int>();
        foreach (var tile in lastSeen)
        {
            if (!currentlyVisible.Contains(tile))
            {
                lostVisibility.Add(tile);
            }
        }

        // update the tile data objects with the new visibility data
        foreach (var tile in visibilityUpdate)
        {
            var tileData = mapHandler.mapDataReal.mapNodes[tile.Key.x, tile.Key.y].AssignedTile;
            var tileIndex = mapHandler.mapSettings.tileData.IndexOf(tileData);
            tileMemory[tile.Key].UpdateMemory(tile.Value, tileIndex);
        }


        // for tiles in last seen that are not in visibility update, set visible to false
        foreach (var tile in lostVisibility)
        {
            var tileData = mapHandler.mapDataReal.mapNodes[tile.x, tile.y].AssignedTile;
            var tileIndex = mapHandler.mapSettings.tileData.IndexOf(tileData);
            tileMemory[tile].UpdateMemory(false, tileIndex);
        }

        // keep the last seen list up to date
        lastSeen.Clear();
        foreach (var tile in tileMemory)
        {
            if (tile.Value.currentlyVisible)
            {
                lastSeen.Add(tile.Key);
            }
        }

        // update the knowledge of the unit
        unitKnowledge.UpdateKnowledge(firstSeen, newlyVisible, lostVisibility, currentlyVisible);
    }

    public class TileMemory
    {
        public int tileType;
        public bool currentlyVisible;
        public bool previouslyVisible;

        public int lastSeenTurn;

        public TileMemory(int tileType, bool currentlyVisible = false, bool previouslyVisible = false,
            int lastSeenTurn = -1)
        {
            this.tileType = tileType;
            this.currentlyVisible = currentlyVisible;
            this.previouslyVisible = previouslyVisible;
            this.lastSeenTurn = lastSeenTurn;
        }


        public void ResetMemory()
        {
            currentlyVisible = false;
            previouslyVisible = false;
            tileType = -1;
            lastSeenTurn = -1;
        }

        public void UpdateMemory(bool tileValue, int tileType, int turn = -1)
        {
            currentlyVisible = tileValue;

            if (!currentlyVisible) return;
            if (!previouslyVisible)
            {
                previouslyVisible = true;
            }

            this.tileType = tileType;
            lastSeenTurn = turn;
        }
    }
}