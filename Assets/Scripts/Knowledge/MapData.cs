using System;
using System.Collections.Generic;
using MapMono;
using UnityEngine;

namespace Knowledge
{
    public class MapData
    {
        public NodeData[,] mapNodes;
        public MapSettings mapSettings;
        public bool isReal = false;
        public Dictionary<int, Vector2Int> unitPositions = new Dictionary<int, Vector2Int>();
        public Dictionary<int, MovingVirtual> movingVirtuals = new Dictionary<int, MovingVirtual>();
        public Dictionary<int, UnitKnowledge> UnitKnowledges = new Dictionary<int, UnitKnowledge>();
        public Dictionary<int, MovingVirtual> visibleUnits = new Dictionary<int, MovingVirtual>();
        public Dictionary<int, FieldOfView> fieldsOfView = new Dictionary<int, FieldOfView>();
        public Dictionary<int, VisionMemory> VisionMemories = new Dictionary<int, VisionMemory>();
        public Dictionary<int, int> unitsLastSeen = new Dictionary<int, int>();

        public MapData(MapSettings _mapSettings, bool isReal = false)
        {
            mapSettings = _mapSettings;
            mapNodes = new NodeData[_mapSettings.mapSize.x, _mapSettings.mapSize.y];
            this.isReal = isReal;

            for (int x = 0; x < _mapSettings.mapSize.x; x++)
            {
                for (int y = 0; y < _mapSettings.mapSize.y; y++)
                {
                    Vector3 worldPosition = new Vector3(x * _mapSettings.tileDiameter + _mapSettings.tileRadius, 0,
                        y * _mapSettings.tileDiameter + _mapSettings.tileRadius);
                    mapNodes[x, y] = new NodeData(worldPosition, x, y);
                }
            }
        }

        public void DrawWithGizmos()
        {
            foreach (NodeData node in mapNodes)
            {
                Gizmos.color = node.AssignedTile.tileColor;
                Gizmos.DrawCube(node.worldPosition, Vector3.one * (0.9f));
            }
        }

        public void ClearAll()
        {
            foreach (var mapNode in mapNodes)
            {
                mapNode.AssignedTile = mapSettings.tileData[0];
            }
        }

        public void GenerateNewMap(bool isReal = true)
        {
            if (!isReal)
            {
                foreach (var mapNode in mapNodes)
                {
                    mapNode.AssignedTile = mapSettings.tileData[0];
                }

                return;
            }


            // foreach (var mapNode in mapNodes)
            // {
            //     mapNode.AssignedTile = mapSettings.tileData[1];
            // }
            // mapNodes[5, 5].AssignedTile = mapSettings.tileData[2];
            // return;
            foreach (var mapNode in mapNodes)
            {
                if (mapNode.gridPosition.x < 3 || mapNode.gridPosition.x > mapSettings.mapSize.x - 3 ||
                    mapNode.gridPosition.y < 3 || mapNode.gridPosition.y > mapSettings.mapSize.y - 3)
                {
                    mapNode.AssignedTile = mapSettings.tileData[1];
                    continue;
                }
                
                var random = UnityEngine.Random.Range(0, 100);
                if (random < mapSettings.walkableTilePercentage)
                {
                    mapNode.AssignedTile = mapSettings.tileData[1];
                }
                else
                {
                    mapNode.AssignedTile =
                        mapSettings.tileData[UnityEngine.Random.Range(2, mapSettings.tileData.Count)];
                }
            }
        }

        public void UpdateUnitPosition(int uid, Vector2Int gridPosition, MovingVirtual unit, int forcedTick = 0)
        {
            var node = mapNodes[gridPosition.x, gridPosition.y];
            if (unitPositions.ContainsKey(uid))
            {
                var prevPos = unitPositions[uid];
                var prevNode = mapNodes[prevPos.x, prevPos.y];
                prevNode.UnitsOnNode.Remove(uid);

                unitPositions[uid] = gridPosition;
            }
            else
            {
                unitPositions.Add(uid, gridPosition);
                
            }

            if (!movingVirtuals.ContainsKey(uid))
            {
                movingVirtuals.Add(uid, unit);
            }
            
            node.UnitsOnNode.Add(uid);

            if (forcedTick==0)
            {
                unitsLastSeen[uid] = Time.frameCount;
            }
            else
            {
                unitsLastSeen[uid] = forcedTick;
            }
            

            if (isReal && node.UnitsSeeingTile.Count > 0)
            {
                foreach (var seeingUnit in node.UnitsSeeingTile)
                {
                    if (UnitKnowledges.ContainsKey(seeingUnit))
                    {
                        UnitKnowledges[seeingUnit].UpdateUnitMovement(uid, unit, gridPosition);
                    }
                }
            }
        }

        public void AddUnitKnowledge(int visibilityUid, UnitKnowledge unitKnowledge)
        {
            UnitKnowledges.Add(visibilityUid, unitKnowledge);
            
        }

        public Vector3 GetUnitPosition(int uid, MapHandler mapHandler)
        {
            if (visibleUnits.ContainsKey(uid))
            {
                return visibleUnits[uid].transform.position;
            }
            else
            {
                var gridPos = unitPositions[uid];
                return mapHandler.GetWorldPosition(gridPos.x, gridPos.y);
            }
        }

        public bool UnitVisible(int uid)
        {
            return visibleUnits.ContainsKey(uid);
        }

        public void Update()
        {
            if (!isReal)
            {
                return;
            }

            foreach (var uidFov in fieldsOfView)
            {
                var uid = uidFov.Key;
                var fov = uidFov.Value;
                fov.VisibilityUpdate();
            }

            foreach (var uidKnowledge in UnitKnowledges)
            {
                var uid = uidKnowledge.Key;
                var knowledge = uidKnowledge.Value;
                var friends = knowledge.closeFriends;
                var fov = fieldsOfView[uid];
                if (friends.Count > 0)
                {
                    foreach (var friend in friends)
                    {
                        if (friend.visibilityUid == uid)
                        {
                            continue;
                        }

                        if (fieldsOfView.ContainsKey(friend.visibilityUid))
                        {
                            var friendFov = fieldsOfView[friend.visibilityUid];
                            fov.Compose(friendFov);
                        }
                    }
                }
            }

            foreach (var uidFov in fieldsOfView)
            {
                var uid = uidFov.Key;
                var fov = uidFov.Value;
                fov.PushUpdate();
            }
        }

        
    }
}