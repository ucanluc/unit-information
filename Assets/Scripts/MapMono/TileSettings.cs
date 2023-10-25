using UnityEngine;

namespace MapMono
{
    public class TileSettings:MonoBehaviour
    {
        public bool walkable = true;
        public bool seeThrough = true;
        public Color tileColor;
        public int movementPenalty = 0;
        public Material standartMaterial;
        public Material hiddenMaterial;
    }
}