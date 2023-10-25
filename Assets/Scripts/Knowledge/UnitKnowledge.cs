using System;
using System.Collections.Generic;
using System.Linq;
using MapMono;
using Pathfinding;
using UnityEngine;

namespace Knowledge
{
    public class UnitKnowledge : MonoBehaviour
    {
        public MapData mapData;
        public MapSettings mapSettings;


        public bool isToBeDrawn = false;
        public MapHandler mapHandler;


        public VisionMemory visionMemory;
        public GameObject mapObjectsParent;
        public Dictionary<Vector2Int, GameObject> mapObjects = new Dictionary<Vector2Int, GameObject>();
        public int visibilityUid = 0;
        public Dictionary<int, UnitMarker> visibilityMarkers = new Dictionary<int, UnitMarker>();
        public MovingVirtual selfMovingVirtual;
        public List<UnitKnowledge> closeFriends = new List<UnitKnowledge>();
        public Dictionary<int, List<Vector2Int>> unitOrders = new Dictionary<int, List<Vector2Int>>();
        public Dictionary<int, int> unitOrderGivenTick = new Dictionary<int, int>();
        public int lastKnownOrderTick = 0;

        public Unit selfUnit;

        private void Awake()
        {
            visibilityUid = gameObject.GetInstanceID();
        }

        void Start()
        {
            mapData = new MapData(mapSettings);
            mapData.GenerateNewMap(false);
            visibilityUid = gameObject.GetInstanceID();
            mapHandler.mapDataReal.AddUnitKnowledge(visibilityUid, this);

            var possibleSelf = GetComponent<MovingVirtual>();
            if (possibleSelf)
            {
                selfMovingVirtual = possibleSelf;
                visibilityUid = selfMovingVirtual.uid;
            }

            if (isToBeDrawn)
            {
                mapObjectsParent = new GameObject("MapObjects");
                ForceShowMap();
            }
            else
            {
                selfUnit = GetComponent<Unit>();
            }
        }

        public void ForceShowMap()
        {
            ForceHideMap();
            foreach (var node in mapData.mapNodes)
            {
                var obj = Instantiate(node.AssignedTile,
                    node.worldPosition +
                    Vector3.up * node.AssignedTile.transform.position.y, Quaternion.identity,
                    mapObjectsParent.transform);
                mapObjects.Add(node.gridPosition, obj.gameObject);
                obj.gameObject.layer = 9;
            }
        }

        public void ForceHideMap()
        {
            var children = new GameObject[mapObjectsParent.transform.childCount];
            for (int i = 0; i < mapObjectsParent.transform.childCount; i++)
            {
                children[i] = mapObjectsParent.transform.GetChild(i).gameObject;
            }

            for (int i = 0; i < children.Length; i++)
            {
                DestroyImmediate(children[i]);
            }
        }

        public void UpdateKnowledge(List<Vector2Int> firstSeen, List<Vector2Int> newlyVisible,
            List<Vector2Int> lostVisibility, List<Vector2Int> currentlyVisible)
        {
            var realMapData = mapHandler.mapDataReal;

            // update the map data with the new information
            foreach (var tile in firstSeen)
            {
                mapData.mapNodes[tile.x, tile.y].AssignedTile = realMapData.mapNodes[tile.x, tile.y].AssignedTile;
                if (isToBeDrawn)
                {
                    DestroyImmediate(mapObjects[tile]);
                    var newObj = Instantiate(mapData.mapNodes[tile.x, tile.y].AssignedTile,
                        mapData.mapNodes[tile.x, tile.y].worldPosition +
                        Vector3.up * mapData.mapNodes[tile.x, tile.y].AssignedTile.transform.position.y,
                        Quaternion.identity, mapObjectsParent.transform).gameObject;
                    mapObjects[tile] = newObj;
                    newObj.layer = 9;
                }
            }

            foreach (var tile in newlyVisible)
            {
                mapData.mapNodes[tile.x, tile.y].isCurrentlyVisible = true;
                mapData.mapNodes[tile.x, tile.y].lastSeen = Time.frameCount;
                realMapData.mapNodes[tile.x, tile.y].UnitsSeeingTile.Add(visibilityUid);
                if (realMapData.mapNodes[tile.x, tile.y].UnitsOnNode != null)
                {
                    foreach (var uid in realMapData.mapNodes[tile.x, tile.y].UnitsOnNode)
                    {
                        if (uid == visibilityUid)
                        {
                            continue;
                        }

                        var unit = realMapData.movingVirtuals[uid];
                        mapData.UpdateUnitPosition(uid, tile, unit);
                        if (!mapData.visibleUnits.ContainsKey(uid))
                        {
                            mapData.visibleUnits.Add(uid, unit);
                            if (unit.friendly == selfMovingVirtual.friendly)
                            {
                                var friend = mapHandler.mapDataReal.UnitKnowledges[uid];
                                closeFriends.Add(friend);
                                ComposeKnowledge(friend.mapData, friend);
                            }

                            if (isToBeDrawn)
                            {
                                realMapData.movingVirtuals[uid].EnableVisibility();
                            }
                        }
                    }
                }

                if (isToBeDrawn)
                {
                    var obj = mapObjects[tile];
                    obj.GetComponent<MeshRenderer>().material = obj.GetComponent<TileSettings>().standartMaterial;
                }
            }


            foreach (var tile in lostVisibility)
            {
                mapData.mapNodes[tile.x, tile.y].isCurrentlyVisible = false;
                realMapData.mapNodes[tile.x, tile.y].UnitsSeeingTile.Remove(visibilityUid);
                if (realMapData.mapNodes[tile.x, tile.y].UnitsOnNode != null)
                {
                    foreach (var uid in realMapData.mapNodes[tile.x, tile.y].UnitsOnNode)
                    {
                        if (uid == visibilityUid)
                        {
                            continue;
                        }

                        var unit = realMapData.movingVirtuals[uid];
                        if (mapData.visibleUnits.ContainsKey(uid))
                        {
                            mapData.visibleUnits.Remove(uid);
                            if (unit.friendly == selfMovingVirtual.friendly)
                            {
                                closeFriends.Remove(realMapData.UnitKnowledges[uid]);
                            }

                            if (isToBeDrawn)
                            {
                                realMapData.movingVirtuals[uid].DisableVisibility();
                            }
                        }

                        mapData.UpdateUnitPosition(uid, tile, unit);
                    }
                }

                if (isToBeDrawn)
                {
                    var obj = mapObjects[tile];
                    obj.GetComponent<MeshRenderer>().material = obj.GetComponent<TileSettings>().hiddenMaterial;
                }
            }

            var toRemove = new List<int>();
            foreach (var uidVirtual in mapData.visibleUnits)
            {
                var sameSide = selfMovingVirtual.friendly == uidVirtual.Value.friendly;

                

                var uid = uidVirtual.Key;
                var unit = uidVirtual.Value;
                var dst = Vector3.Distance(unit.transform.position, selfMovingVirtual.transform.position);
                if (dst < 2f && !sameSide)
                {
                    var newFriendly = selfMovingVirtual.friendly || unit.friendly;
                    selfMovingVirtual.friendly = newFriendly;
                    unit.friendly = newFriendly;
                    var friend = mapHandler.mapDataReal.UnitKnowledges[uid];

                    foreach (var friendCloseFriend in friend.closeFriends)
                    {
                        friendCloseFriend.closeFriends.Remove(friend);
                    }

                    friend.closeFriends.Clear();

                    closeFriends.Add(friend);
                    ComposeKnowledge(friend.mapData, friend);
                }

                if (dst > 10f)
                {
                    toRemove.Add(uid);
                    if (sameSide && closeFriends.Contains(realMapData.UnitKnowledges[uid]))
                    {
                        closeFriends.Remove(realMapData.UnitKnowledges[uid]);
                    }
                }
            }

            foreach (var toRem in toRemove)
            {
                mapData.visibleUnits.Remove(toRem);
                if (isToBeDrawn)
                {
                    realMapData.movingVirtuals[toRem].DisableVisibility();
                }
            }

            if (isToBeDrawn)
            {
                foreach (var uidVirtualPair in mapData.movingVirtuals)
                {
                    // Debug.Log("updating " + uidVirtualPair.Key);
                    var uid = uidVirtualPair.Key;
                    var unit = uidVirtualPair.Value;

                    if (uid == visibilityUid)
                    {
                        continue;
                    }

                    UnitMarker marker;
                    if (visibilityMarkers.ContainsKey(uid))
                    {
                        marker = visibilityMarkers[uid];
                    }
                    else
                    {
                        marker = Instantiate(mapSettings.VisibilityMarkerPrefab, mapObjectsParent.transform)
                            .GetComponent<UnitMarker>();
                        visibilityMarkers.Add(uid, marker);
                    }

                    marker.UpdateMaterial(unit.friendly, mapData.UnitVisible(uid),
                        mapHandler.selectedUnits.Contains(uid), mapSettings);


                    var pos = mapData.GetUnitPosition(uid, mapHandler);
                    marker.transform.position = new Vector3(pos.x, mapSettings.markerYoffset, pos.z);
                }

                foreach (var uidVisibilityMarker in visibilityMarkers)
                {
                    var uid = uidVisibilityMarker.Key;
                    var marker = uidVisibilityMarker.Value;
                    var unit = mapData.movingVirtuals[uid];
                    marker.UpdateMaterial(unit.friendly, mapData.UnitVisible(uid),
                        mapHandler.selectedUnits.Contains(uid), mapSettings);
                }

                var allOrders = new List<Vector2Int>();
                foreach (var uidOrderList in unitOrders)
                {
                    var uid = uidOrderList.Key;
                    var orders = uidOrderList.Value;
                    allOrders.AddRange(orders);
                }

                // keep only unique orders
                allOrders = allOrders.Distinct().ToList();
                mapHandler.DrawOrders(allOrders);

                UpdateOrdersFrom(mapHandler.unitOrders, mapHandler.unitOrderGivenTick, mapHandler.latestOrderTick);
            }
            else
            {
                foreach (var closeFriend in closeFriends)
                {
                    UpdateOrdersFrom(closeFriend.unitOrders, closeFriend.unitOrderGivenTick,
                        closeFriend.lastKnownOrderTick);
                }

                var enemies = new List<int>();
                foreach (var unit in mapData.visibleUnits)
                {
                    if (unit.Value.friendly == selfMovingVirtual.friendly)
                    {
                        continue;
                    }

                    enemies.Add(unit.Key);
                }

                if (enemies.Count > 0)
                {
                    var enemyVirtuals = new List<MovingVirtual>();
                    foreach (var enemy in enemies)
                    {
                        enemyVirtuals.Add(mapData.movingVirtuals[enemy]);
                    }

                    var order = new List<Vector2Int>();
                    MovingVirtual enemyVirtual = null;
                    Vector3 pos = Vector3.zero;
                    var closest = 1000000f;
                    foreach (var enemy in enemyVirtuals)
                    {
                        var dist = Vector3.Distance(enemy.transform.position, selfMovingVirtual.transform.position);
                        if (dist < closest)
                        {
                            closest = dist;
                            pos = enemy.transform.position;
                            enemyVirtual = enemy;
                        }
                    }

                    pos = mapData.GetUnitPosition(enemyVirtual.uid, mapHandler);
                    if (!selfMovingVirtual.friendly)
                    {
                        if (selfMovingVirtual.transform.position.x > 20 && selfMovingVirtual.transform.position.x < 100
                                                                        && selfMovingVirtual.transform.position.z >
                                                                        20 && selfMovingVirtual.transform.position.z <
                                                                        100)
                        {
                            pos = transform.position -
                                  (enemyVirtual.transform.position - transform.position).normalized * 10;
                        }
                        else
                        {
                            pos = mapHandler.getRandPos();
                        }
                    }

                    var mapLength = mapSettings.mapSize.x * mapSettings.tileDiameter;
                    var mapWidth = mapSettings.mapSize.y * mapSettings.tileDiameter;
                    if (pos.x < 0 || pos.x > mapLength || pos.z < 0 ||
                        pos.z > mapWidth)
                    {
                        pos.x = Mathf.Clamp(pos.x, 0, mapLength);
                        pos.z = Mathf.Clamp(pos.z, 0, mapWidth);
                    }


                    var gridPos = mapHandler.GetGlobalPosAsGridPos(pos);


                    order.Add(gridPos);
                    selfUnit.SetOrders(order, enemyVirtual, selfMovingVirtual.friendly);
                }
                else
                {
                    UpdateSelfOrders();
                }
            }
        }


        private void UpdateOrdersFrom(Dictionary<int, List<Vector2Int>> orders, Dictionary<int, int> orderOnTick,
            int latestTick)
        {
            if (latestTick == lastKnownOrderTick)
            {
                return;
            }
            else if (latestTick > lastKnownOrderTick)
            {
                unitOrders.Clear();
                foreach (var order in orders)
                {
                    List<Vector2Int> clone = new List<Vector2Int>();
                    foreach (var tile in order.Value)
                    {
                        clone.Add(tile);
                    }

                    unitOrders.Add(order.Key, clone);
                }

                lastKnownOrderTick = latestTick;

                foreach (var order in orderOnTick)
                {
                    if (order.Value > lastKnownOrderTick)
                    {
                        lastKnownOrderTick = order.Value;
                    }
                }

                if (!isToBeDrawn)
                {
                    UpdateSelfOrders();
                }
            }
        }

        private void UpdateSelfOrders()
        {
            if (unitOrders.ContainsKey(visibilityUid))
            {
                selfUnit.UpdateOrders(unitOrders[visibilityUid]);
            }
        }

        private void ComposeKnowledge(MapData friendMapData, UnitKnowledge friend)
        {
            if (!mapData.movingVirtuals.ContainsKey(friend.visibilityUid))
            {
                mapData.movingVirtuals.Add(friend.visibilityUid, friend.selfMovingVirtual);
            }

            for (int x = 0; x < mapSettings.mapSize.x; x++)
            {
                for (int y = 0; y < mapSettings.mapSize.y; y++)
                {
                    var node = mapData.mapNodes[x, y];
                    var friendNode = friendMapData.mapNodes[x, y];
                    if (node.lastSeen == friendNode.lastSeen)
                    {
                        continue;
                    }

                    var tile = new Vector2Int(x, y);
                    if (node.AssignedTile != friendNode.AssignedTile)
                    {
                        if (node.lastSeen > friendNode.lastSeen)
                        {
                            friendNode.AssignedTile = node.AssignedTile;
                            friendNode.lastSeen = node.lastSeen;
                        }
                        else if (node.lastSeen < friendNode.lastSeen)
                        {
                            node.AssignedTile = friendNode.AssignedTile;
                            node.lastSeen = friendNode.lastSeen;

                            if (isToBeDrawn)
                            {
                                DestroyImmediate(mapObjects[tile]);
                                var newObj = Instantiate(mapData.mapNodes[tile.x, tile.y].AssignedTile,
                                    mapData.mapNodes[tile.x, tile.y].worldPosition +
                                    Vector3.up * mapData.mapNodes[tile.x, tile.y].AssignedTile.transform.position.y,
                                    Quaternion.identity, mapObjectsParent.transform).gameObject;
                                mapObjects[tile] = newObj;
                                newObj.layer = 9;

                                if (node.isCurrentlyVisible)
                                {
                                    newObj.GetComponent<MeshRenderer>().material =
                                        newObj.GetComponent<TileSettings>().standartMaterial;
                                }
                                else
                                {
                                    newObj.GetComponent<MeshRenderer>().material =
                                        newObj.GetComponent<TileSettings>().hiddenMaterial;
                                }
                            }
                        }
                    }
                }
            }

            var allKnownUnits = new List<int>();
            allKnownUnits.AddRange(mapData.movingVirtuals.Keys);
            allKnownUnits.AddRange(friendMapData.movingVirtuals.Keys);
            allKnownUnits = allKnownUnits.Distinct().ToList();
            foreach (var knownUnit in allKnownUnits)
            {
                if (!mapData.movingVirtuals.ContainsKey(knownUnit) &&
                    friendMapData.movingVirtuals.ContainsKey(knownUnit))
                {
                    mapData.UpdateUnitPosition(knownUnit, friendMapData.unitPositions[knownUnit],
                        mapHandler.mapDataReal.movingVirtuals[knownUnit], friendMapData.unitsLastSeen[knownUnit]);
                }
                else if (mapData.movingVirtuals.ContainsKey(knownUnit) &&
                         !friendMapData.movingVirtuals.ContainsKey(knownUnit))
                {
                    friendMapData.UpdateUnitPosition(knownUnit, mapData.unitPositions[knownUnit],
                        mapHandler.mapDataReal.movingVirtuals[knownUnit], mapData.unitsLastSeen[knownUnit]);
                }
                else if (mapData.unitsLastSeen[knownUnit] > friendMapData.unitsLastSeen[knownUnit])

                {
                    friendMapData.UpdateUnitPosition(knownUnit, mapData.unitPositions[knownUnit],
                        mapHandler.mapDataReal.movingVirtuals[knownUnit], mapData.unitsLastSeen[knownUnit]);
                }
                else if (mapData.unitsLastSeen[knownUnit] < friendMapData.unitsLastSeen[knownUnit])
                {
                    mapData.UpdateUnitPosition(knownUnit, friendMapData.unitPositions[knownUnit],
                        mapHandler.mapDataReal.movingVirtuals[knownUnit], friendMapData.unitsLastSeen[knownUnit]);
                }
            }
        }

        public void UpdateUnitMovement(int uid, MovingVirtual unit, Vector2Int gridPosition)
        {
            if (uid == visibilityUid)
            {
                return;
            }

            var node = mapData.mapNodes[gridPosition.x, gridPosition.y];

            // a seen unit moved
            mapData.UpdateUnitPosition(uid, gridPosition, unit);

            if (mapData.mapNodes[gridPosition.x, gridPosition.y].isCurrentlyVisible)
            {
                if (!mapData.visibleUnits.ContainsKey(uid))
                {
                    mapData.visibleUnits.Add(uid, unit);
                    if (unit.friendly == selfMovingVirtual.friendly)
                    {
                        var friend = mapHandler.mapDataReal.UnitKnowledges[uid];
                        closeFriends.Add(friend);
                        ComposeKnowledge(friend.mapData, friend);
                    }

                    if (isToBeDrawn)
                    {
                        unit.EnableVisibility();
                    }
                }
            }
            else
            {
                if (mapData.visibleUnits.ContainsKey(uid))
                {
                    mapData.visibleUnits.Remove(uid);
                    if (unit.friendly == selfMovingVirtual.friendly)
                    {
                        closeFriends.Remove(mapHandler.mapDataReal.UnitKnowledges[uid]);
                    }

                    if (isToBeDrawn)
                    {
                        unit.DisableVisibility();
                    }
                }
            }
        }
    }
}