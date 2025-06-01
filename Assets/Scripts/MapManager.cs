using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CarBlade.Environment
{
    public class MapManager : MonoBehaviour, IMapManager
    {
        private static MapManager _instance;
        public static MapManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<MapManager>();
                }
                return _instance;
            }
        }

        [Header("Map Configuration")]
        [SerializeField] private MapData mapData;
        [SerializeField] private Transform mapCenter;
        [SerializeField] private float stadiumRadius = 50f;
        [SerializeField] private float innerRingRadius = 30f;
        [SerializeField] private float outerRingRadius = 45f;

        [Header("Spawn System")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnPointRadius = 3f;
        [SerializeField] private LayerMask vehicleLayer;
        private List<Transform> availableSpawnPoints = new List<Transform>();

        [Header("Map Elements")]
        [SerializeField] private Transform[] jumpRamps;
        [SerializeField] private Transform[] obstacles;
        [SerializeField] private Transform[] tunnels;

        [Header("Boundaries")]
        [SerializeField] private float boundaryHeight = 10f;
        [SerializeField] private GameObject boundaryPrefab;

        // 인터랙터블 관리
        private List<IInteractable> interactables = new List<IInteractable>();

        // 맵 영역 정의
        public enum MapZone
        {
            CentralField,    // 중앙 필드
            InnerRing,      // 내부 링
            OuterRing,      // 외곽 링
            Tunnel,         // 터널/언더패스
            JumpZone        // 점프대 영역
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            InitializeMap();
        }

        private void InitializeMap()
        {
            // 스폰 포인트 초기화
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                GenerateSpawnPoints();
            }
            else
            {
                availableSpawnPoints.AddRange(spawnPoints);
            }

            // 맵 요소 등록
            RegisterMapElements();

            // 경계 생성
            if (boundaryPrefab != null)
            {
                CreateBoundaries();
            }
        }

        // IMapManager 구현
        public Vector3 GetRandomSpawnPoint()
        {
            // 사용 가능한 스폰 포인트 찾기
            var validPoints = availableSpawnPoints.Where(sp => IsSpawnPointClear(sp.position)).ToList();

            if (validPoints.Count > 0)
            {
                int randomIndex = Random.Range(0, validPoints.Count);
                return validPoints[randomIndex].position;
            }

            // 모든 포인트가 막혀있으면 랜덤 위치 생성
            return GenerateRandomPosition(MapZone.CentralField);
        }

        public List<Transform> GetJumpRamps()
        {
            return jumpRamps?.ToList() ?? new List<Transform>();
        }

        public void RegisterInteractableObject(IInteractable obj)
        {
            if (!interactables.Contains(obj))
            {
                interactables.Add(obj);
            }
        }

        // 스폰 포인트가 비어있는지 확인
        private bool IsSpawnPointClear(Vector3 position)
        {
            Collider[] colliders = UnityEngine.Physics.OverlapSphere(position, spawnPointRadius, vehicleLayer);
            return colliders.Length == 0;
        }

        // 자동 스폰 포인트 생성
        private void GenerateSpawnPoints()
        {
            int spawnCount = 20; // 최대 플레이어 수만큼
            spawnPoints = new Transform[spawnCount];

            for (int i = 0; i < spawnCount; i++)
            {
                GameObject spawnPoint = new GameObject($"SpawnPoint_{i}");
                spawnPoint.transform.parent = transform;

                // 원형으로 배치
                float angle = (360f / spawnCount) * i * Mathf.Deg2Rad;
                float radius = innerRingRadius + Random.Range(-5f, 5f);

                Vector3 position = new Vector3(
                    mapCenter.position.x + Mathf.Sin(angle) * radius,
                    mapCenter.position.y + 1f,
                    mapCenter.position.z + Mathf.Cos(angle) * radius
                );

                spawnPoint.transform.position = position;
                spawnPoint.transform.rotation = Quaternion.LookRotation(-position.normalized);

                spawnPoints[i] = spawnPoint.transform;
                availableSpawnPoints.Add(spawnPoint.transform);
            }
        }

        // 맵 요소 등록
        private void RegisterMapElements()
        {
            // 점프대 등록
            if (jumpRamps != null)
            {
                foreach (var ramp in jumpRamps)
                {
                    var jumpRampComponent = ramp.GetComponent<JumpRamp>();
                    if (jumpRampComponent == null)
                    {
                        jumpRampComponent = ramp.gameObject.AddComponent<JumpRamp>();
                    }
                    RegisterInteractableObject(jumpRampComponent);
                }
            }

            // 터널 등록
            if (tunnels != null)
            {
                foreach (var tunnel in tunnels)
                {
                    var tunnelComponent = tunnel.GetComponent<TunnelZone>();
                    if (tunnelComponent == null)
                    {
                        tunnelComponent = tunnel.gameObject.AddComponent<TunnelZone>();
                    }
                    RegisterInteractableObject(tunnelComponent);
                }
            }
        }

        // 경계 생성
        private void CreateBoundaries()
        {
            int segments = 32;
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float nextAngle = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector3 pos1 = new Vector3(
                    Mathf.Sin(angle) * stadiumRadius,
                    boundaryHeight / 2f,
                    Mathf.Cos(angle) * stadiumRadius
                );

                Vector3 pos2 = new Vector3(
                    Mathf.Sin(nextAngle) * stadiumRadius,
                    boundaryHeight / 2f,
                    Mathf.Cos(nextAngle) * stadiumRadius
                );

                CreateBoundarySegment(pos1, pos2);
            }
        }

        // 경계 세그먼트 생성
        private void CreateBoundarySegment(Vector3 start, Vector3 end)
        {
            GameObject boundary = Instantiate(boundaryPrefab, transform);
            boundary.transform.position = (start + end) / 2f;

            Vector3 direction = end - start;
            boundary.transform.rotation = Quaternion.LookRotation(direction);
            boundary.transform.localScale = new Vector3(1f, boundaryHeight, direction.magnitude);
        }

        // 맵 영역별 랜덤 위치 생성
        public Vector3 GenerateRandomPosition(MapZone zone)
        {
            Vector3 position = Vector3.zero;

            switch (zone)
            {
                case MapZone.CentralField:
                    float centralRadius = Random.Range(0f, innerRingRadius);
                    float centralAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    position = new Vector3(
                        Mathf.Sin(centralAngle) * centralRadius,
                        1f,
                        Mathf.Cos(centralAngle) * centralRadius
                    );
                    break;

                case MapZone.InnerRing:
                    float innerRadius = Random.Range(innerRingRadius, outerRingRadius);
                    float innerAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    position = new Vector3(
                        Mathf.Sin(innerAngle) * innerRadius,
                        1f,
                        Mathf.Cos(innerAngle) * innerRadius
                    );
                    break;

                case MapZone.OuterRing:
                    float outerRadius = Random.Range(outerRingRadius, stadiumRadius - 5f);
                    float outerAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    position = new Vector3(
                        Mathf.Sin(outerAngle) * outerRadius,
                        1f,
                        Mathf.Cos(outerAngle) * outerRadius
                    );
                    break;
            }

            return mapCenter.position + position;
        }

        // 현재 맵 영역 확인
        public MapZone GetMapZone(Vector3 position)
        {
            float distance = Vector3.Distance(new Vector3(position.x, 0, position.z),
                                            new Vector3(mapCenter.position.x, 0, mapCenter.position.z));

            if (distance < innerRingRadius)
                return MapZone.CentralField;
            else if (distance < outerRingRadius)
                return MapZone.InnerRing;
            else
                return MapZone.OuterRing;
        }

        // 가장 가까운 점프대 찾기
        public Transform GetNearestJumpRamp(Vector3 position)
        {
            if (jumpRamps == null || jumpRamps.Length == 0)
                return null;

            Transform nearest = null;
            float minDistance = float.MaxValue;

            foreach (var ramp in jumpRamps)
            {
                if (ramp != null)
                {
                    float distance = Vector3.Distance(position, ramp.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = ramp;
                    }
                }
            }

            return nearest;
        }

        // 디버그 표시
        private void OnDrawGizmos()
        {
            if (mapCenter == null) return;

            // 중앙 필드
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            DrawCircle(mapCenter.position, innerRingRadius, 32);

            // 내부 링
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            DrawCircle(mapCenter.position, outerRingRadius, 32);

            // 외곽 링
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            DrawCircle(mapCenter.position, stadiumRadius, 32);

            // 스폰 포인트
            Gizmos.color = Color.cyan;
            if (spawnPoints != null)
            {
                foreach (var spawn in spawnPoints)
                {
                    if (spawn != null)
                    {
                        Gizmos.DrawWireSphere(spawn.position, spawnPointRadius);
                        Gizmos.DrawRay(spawn.position, spawn.forward * 3f);
                    }
                }
            }
        }

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 point = center + new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
    }
}