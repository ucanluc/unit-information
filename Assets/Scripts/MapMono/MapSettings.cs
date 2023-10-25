using System.Collections.Generic;
using UnityEngine;

namespace MapMono
{
    public class MapSettings:MonoBehaviour
    {
        public Vector2Int mapSize;
        public float tileRadius;
        public float tileDiameter;
        public List<TileSettings> tileData;
        public TileSettings unknownTile;
        public Vector3 worldObjectRealOffset = new Vector3(0, -20f, 0);
        public int walkableTilePercentage = 80;

        public MapSettings(Vector2Int mapSize, float tileRadius)
        {
            this.mapSize = mapSize;
            this.tileRadius = tileRadius;
            tileDiameter = tileRadius * 2;
        }

        public GameObject VisibilityMarkerPrefab;
        public float markerYoffset;
        public Material friendlyMarkerMaterial;
        public Material enemyMarkerMaterial;
        public Material friendlyMarkerMaterialHidden;
        public Material enemyMarkerMaterialHidden;
        public Material selectedMarkerMaterial;
    }
}