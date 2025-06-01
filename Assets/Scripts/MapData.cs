using UnityEngine;

namespace CarBlade.Environment
{
    [CreateAssetMenu(fileName = "MapData", menuName = "CarBlade/Map Data", order = 2)]
    public class MapData : ScriptableObject
    {
        public string mapName = "Stadium";
        public string description = "Classic stadium map for vehicle combat";
        public Sprite mapPreview;
        public GameObject mapPrefab;

        [Header("Map Properties")]
        public float stadiumRadius = 50f;
        public int maxPlayers = 20;
        public bool hasJumpRamps = true;
        public bool hasTunnels = true;
        public bool hasObstacles = true;
    }
}