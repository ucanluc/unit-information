using System;
using System.Collections.Generic;
using Knowledge;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MapMono
{
    public class MapHandler : MonoBehaviour
    {
        public MapSettings mapSettings;
        public MapData mapDataReal;

        public bool drawGizmos = true;
        public bool PauseGame = false;

        public List<int> selectedUnits = new List<int>();
        public Dictionary<int, List<Vector2Int>> unitOrders = new Dictionary<int, List<Vector2Int>>();
        public Dictionary<int, int> unitOrderGivenTick = new Dictionary<int, int>();
        public int latestOrderTick;
        public List<GameObject> orderMarkers = new List<GameObject>();


        private void OnDrawGizmos()
        {
            if (drawGizmos && mapDataReal != null)
            {
                mapDataReal.DrawWithGizmos();
            }
        }

        public void GenerateMap()
        {
            mapDataReal = new MapData(mapSettings, true);
            mapDataReal.GenerateNewMap();
            ForceShowMap();
            // create 50 order markers
            for (int i = 0; i < 50; i++)
            {
                GameObject orderMarker = Instantiate(mapSettings.VisibilityMarkerPrefab, transform);
                orderMarker.SetActive(false);
                orderMarkers.Add(orderMarker);
                orderMarker.GetComponent<Renderer>().material.color = Color.yellow;
            }
        }

        public void Update()
        {
            mapDataReal.Update();
        }

        public void ClearMap()
        {
            mapDataReal.ClearAll();
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3((x + mapSettings.tileRadius) * mapSettings.tileDiameter, 0f,
                (y + mapSettings.tileRadius) * mapSettings.tileDiameter);
        }

        public Vector2Int GetGlobalPosAsGridPos(Vector3 transformPosition)
        {

            
            int x = Mathf.RoundToInt(transformPosition.x - mapSettings.tileRadius / mapSettings.tileDiameter);
            int y = Mathf.RoundToInt(transformPosition.z - mapSettings.tileRadius / mapSettings.tileDiameter);
            return new Vector2Int(x, y);
        }

        public bool IsInBounds(Vector2Int coordinates)
        {
            return coordinates.x >= 0 && coordinates.x < mapSettings.mapSize.x && coordinates.y >= 0 &&
                   coordinates.y < mapSettings.mapSize.y;
        }

        public bool BlocksVision(Vector2Int pos)
        {
            var node = mapDataReal.mapNodes[pos.x, pos.y];
            return !node.AssignedTile.seeThrough;
        }

        public void ForceShowMap()
        {
            ForceHideMap();
            foreach (var node in mapDataReal.mapNodes)
            {
                Instantiate(node.AssignedTile,
                    node.worldPosition + mapSettings.worldObjectRealOffset +
                    Vector3.up * node.AssignedTile.transform.position.y, Quaternion.identity, transform);
            }
        }

        public void ForceHideMap()
        {
            var children = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                children[i] = transform.GetChild(i).gameObject;
            }

            for (int i = 0; i < children.Length; i++)
            {
                DestroyImmediate(children[i]);
            }
        }

        public bool IsWalkable(Vector3 worldPoint)
        {
            Vector2Int gridPos = GetGlobalPosAsGridPos(worldPoint);
            if (IsInBounds(gridPos))
            {
                return mapDataReal.mapNodes[gridPos.x, gridPos.y].AssignedTile.walkable;
            }
            else
            {
                return false;
            }
        }

        public int GetMovementPenalty(Vector3 worldPoint)
        {
            Vector2Int gridPos = GetGlobalPosAsGridPos(worldPoint);
            if (IsInBounds(gridPos))
            {
                return mapDataReal.mapNodes[gridPos.x, gridPos.y].AssignedTile.movementPenalty;
            }
            else
            {
                return 0;
            }
        }

        public bool IsWalkableGrid(int x, int y)
        {
            if (IsInBounds(new Vector2Int(x, y)))
            {
                return mapDataReal.mapNodes[x, y].AssignedTile.walkable;
            }
            else
            {
                return false;
            }
        }

        public int GetMovementPenaltyGrid(int x, int y)
        {
            if (IsInBounds(new Vector2Int(x, y)))
            {
                return mapDataReal.mapNodes[x, y].AssignedTile.movementPenalty;
            }
            else
            {
                return 0;
            }
        }

        public void HandlePause(bool pauseGame)
        {
            PauseGame = pauseGame;
        }

        public void HandleMouseClick(Vector3 dragStartPosition)
        {
            var gridPos = GetGlobalPosAsGridPos(dragStartPosition);
            if (gridPos.x < 0 || gridPos.x >= mapSettings.mapSize.x || gridPos.y < 0 ||
                gridPos.y >= mapSettings.mapSize.y)
            {
                return;
            }

            var gridNode = mapDataReal.mapNodes[gridPos.x, gridPos.y];

            var gridUnits = gridNode.UnitsOnNode;

            var isShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var isControlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (gridUnits.Count == 0)
            {
                if (isShiftDown || isControlDown)
                {
                    return;
                }
                else
                {
                    selectedUnits.Clear();
                }
            }

            foreach (var unit in gridUnits)
            {
                if (!mapDataReal.movingVirtuals[unit].friendly)
                {
                    continue;
                }
                
                if (isControlDown)
                {
                    if (selectedUnits.Contains(unit))
                    {
                        selectedUnits.Remove(unit);
                    }
                    else
                    {
                        selectedUnits.Add(unit);
                    }
                }

                else if (isShiftDown)
                {
                    selectedUnits.Add(unit);
                }
                else
                {
                    selectedUnits.Clear();
                    selectedUnits.Add(unit);
                }
            }
        }

        public void HandleMouseRightClick(Vector3 dragCurrentPosition)
        {
            var gridPos = GetGlobalPosAsGridPos(dragCurrentPosition);

            if (gridPos.x < 0 || gridPos.x >= mapSettings.mapSize.x || gridPos.y < 0 ||
                gridPos.y >= mapSettings.mapSize.y)
            {
                return;
            }

            var isShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var isControlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            foreach (var selectedUnit in selectedUnits)
            {
                // set the order given tick
                if (unitOrderGivenTick.ContainsKey(selectedUnit))
                {
                    unitOrderGivenTick[selectedUnit] = Time.frameCount;
                }
                else
                {
                    unitOrderGivenTick.Add(selectedUnit, Time.frameCount);
                }

                if (isControlDown)
                {
                    if (unitOrders.ContainsKey(selectedUnit))
                    {
                        unitOrders[selectedUnit].Add(gridPos);
                    }
                    else
                    {
                        unitOrders.Add(selectedUnit, new List<Vector2Int> { gridPos });
                    }
                }

                else if (isShiftDown)
                {
                    if (unitOrders.ContainsKey(selectedUnit))
                    {
                        unitOrders[selectedUnit].Add(gridPos);
                    }
                    else
                    {
                        unitOrders.Add(selectedUnit, new List<Vector2Int> { gridPos });
                    }
                }
                else
                {
                    if (unitOrders.ContainsKey(selectedUnit))
                    {
                        unitOrders[selectedUnit].Clear();
                        unitOrders[selectedUnit].Add(gridPos);
                    }
                    else
                    {
                        unitOrders.Add(selectedUnit, new List<Vector2Int> { gridPos });
                    }
                }
            }

            latestOrderTick = Time.frameCount;
        }

        public void DrawOrders(List<Vector2Int> allOrders)
        {
            // disable all order markers
            foreach (var orderMarker in orderMarkers)
            {
                orderMarker.SetActive(false);
            }

            for (var index = 0; index < allOrders.Count; index++)
            {
                var order = allOrders[index];
                var worldPos = GetWorldPosition(order.x, order.y);
                var marker = orderMarkers[index];
                marker.SetActive(true);
                marker.transform.position = worldPos + Vector3.up * 0.1f;
            }
        }

        public void UpdateFriendlyness(int uid, bool newFriendly)
        {
            return;
        }

        public Vector3 getRandPos()
        {
            var randX = Random.Range(3, mapSettings.mapSize.x-3);
            var randY = Random.Range(3, mapSettings.mapSize.y-3);
            var randPos = GetWorldPosition(randX, randY);
            return randPos;
        }
    }
}