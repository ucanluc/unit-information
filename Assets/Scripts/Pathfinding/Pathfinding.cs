using System;
using System.Collections.Generic;
using System.Diagnostics;
using DataStruc;
using UnityEngine;

namespace Pathfinding
{
    public class Pathfinding : MonoBehaviour
    {
        public bool printPathfindingTime = false;
        private Grid grid;

        private void Awake()
        {
            grid = GetComponent<Grid>();
        }


        public void FindPath(PathRequest request, Action<PathResult> callback)
        {
            Stopwatch sw = new Stopwatch();
            if (printPathfindingTime)
            {
                sw.Start();
            }


            Vector3[] waypoints = new Vector3[0];
            bool pathSuccess = false;

            Node startNode = grid.NodeFromWorldPoint(request.pathStart);
            Node targetNode = grid.NodeFromWorldPoint(request.pathEnd);


            // if (startNode.walkable && targetNode.walkable)
            if (targetNode.walkable)

            {
                Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
                HashSet<Node> closedSet = new HashSet<Node>();
                openSet.Add(startNode);

                while (openSet.Count > 0)
                {
                    Node currentNode = openSet.RemoveFirst();

                    closedSet.Add(currentNode);

                    if (currentNode == targetNode)
                    {
                        if (printPathfindingTime)
                        {
                            sw.Stop();
                            print("Path found: " + sw.ElapsedMilliseconds + " ms");
                        }

                        pathSuccess = true;

                        break;
                    }

                    foreach (var neighbour in grid.GetNeighbours(currentNode))
                    {
                        if (!neighbour.walkable || closedSet.Contains(neighbour))
                        {
                            continue;
                        }

                        int newMovementCostToNeighbour =
                            currentNode.PathfindingNode.gCost + GetDistance(currentNode, neighbour) +
                            neighbour.PathfindingNode.movementPenalty;
                        if (newMovementCostToNeighbour < neighbour.PathfindingNode.gCost ||
                            !openSet.Contains(neighbour))
                        {
                            neighbour.PathfindingNode.gCost = newMovementCostToNeighbour;
                            neighbour.PathfindingNode.hCost = GetDistance(neighbour, targetNode);
                            neighbour.PathfindingNode.parent = currentNode;
                            if (!openSet.Contains(neighbour))
                            {
                                openSet.Add(neighbour);
                            }
                            else
                            {
                                openSet.UpdateItem(neighbour);
                            }
                        }
                    }
                }
            }

            if (pathSuccess)
            {
                waypoints = RetracePath(startNode, targetNode);
                pathSuccess = waypoints.Length > 0;
            }

            callback(new PathResult(waypoints, pathSuccess, request.callback));
        }

        Vector3[] RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;
            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.PathfindingNode.parent;
            }

            // Vector3[] waypoints = SimplifyPath(path, startNode);
            Vector3[] waypoints = PathToWaypoints(path);
            Array.Reverse(waypoints);
            return waypoints;
        }
        private Vector3[] PathToWaypoints(List<Node> path)
        {
            List<Vector3> waypoints = new List<Vector3>();
            for (int i = 1; i < path.Count; i++)
            {
                waypoints.Add(path[i].worldPosition);
            }

            return waypoints.ToArray();
        }

        private Vector3[] SimplifyPath(List<Node> path, Node startNode)
        {
            List<Vector3> waypoints = new List<Vector3>();
            Vector2 directionOld = Vector2.zero;
            for (int i = 1; i < path.Count; i++)
            {
                Vector2 directionNew =
                    new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
                if (directionNew != directionOld)
                {
                    waypoints.Add(path[i - 1].worldPosition); //Changed from path[i] to path[i-1]
                }

                directionOld = directionNew;
                if (i == path.Count - 1 && directionOld != new Vector2(path[i].gridX, path[i].gridY) -
                    new Vector2(startNode.gridX, startNode.gridY))
                    waypoints.Add(path[path.Count - 1].worldPosition);
            }

            return waypoints.ToArray();
        }

        int GetDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
            int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

            if (dstX > dstY)
            {
                return 14 * dstY + 10 * (dstX - dstY);
            }

            return 14 * dstX + 10 * (dstY - dstX);
        }
    }
}