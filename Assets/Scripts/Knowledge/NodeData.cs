using System.Collections.Generic;
using MapMono;
using UnityEngine;

namespace Knowledge
{
    public class NodeData
    {
        public Vector3 worldPosition;
        public Vector2Int gridPosition;
        private TileSettings assignedTile;
        public bool isCurrentlyVisible;
        public double lastSeen;
        public List<int> UnitsOnNode = new List<int>();
        public List<int> UnitsSeeingTile = new List<int>();
        
        public TileSettings AssignedTile
        {
            get => assignedTile;
            set => assignedTile = value;
            // if (assignedTile != null)
            // {
            //     assignedTile.SetNodeData(this);
            // }
        }



        public NodeData(Vector3 worldPos, int x, int y)
        {
            worldPosition = worldPos;
            gridPosition = new Vector2Int(x, y);
        }
        
    }
}