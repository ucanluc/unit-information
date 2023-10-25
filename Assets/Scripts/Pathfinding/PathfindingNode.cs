using UnityEngine;

namespace Pathfinding
{
    public class PathfindingNode
    {
        public int movementPenalty;
        public int gCost;
        public int hCost;
        public Node parent;
        private int heapIndex;

        public PathfindingNode(int movementPenalty)
        {
            this.movementPenalty = movementPenalty;
        }

        public int HeapIndex
        {
            get { return heapIndex; }
            set { heapIndex = value; }
        }

        public int fCost
        {
            get { return gCost + hCost; }
        }

        public int CompareTo(Node nodeToCompare)
        {
            int compare = fCost.CompareTo(nodeToCompare.PathfindingNode.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(nodeToCompare.PathfindingNode.hCost);
            }
            return -compare;
        }
    }

    public class Node : IHeapItem<Node>
    {
        public bool walkable;
        public Vector3 worldPosition;
        public int gridX;
        public int gridY;

        private readonly PathfindingNode _pathfindingNode;

        public Node(bool _walkable, Vector3 _worldPosition, int _gridX, int _gridY, int _penalty)
        {
            walkable = _walkable;
            worldPosition = _worldPosition;
            gridX = _gridX;
            gridY = _gridY;
            _pathfindingNode = new PathfindingNode(_penalty);
        }


        public int HeapIndex
        {
            set { _pathfindingNode.HeapIndex = value; }
            get { return _pathfindingNode.HeapIndex; }
        }

        public PathfindingNode PathfindingNode
        {
            get { return _pathfindingNode; }
        }

        public int CompareTo(Node nodeToCompare)
        {
            return _pathfindingNode.CompareTo(nodeToCompare);
        }
    }
}