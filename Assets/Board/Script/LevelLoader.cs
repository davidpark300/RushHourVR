using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;


[Serializable]
public class ShipData
{
    public Vector3Int origin;
    public string direction;
    public int length;
}

[Serializable]
public class LevelData
{
    public Vector3Int boardSize;
    public Vector3Int exit;
    public ShipData hero;
    public ShipData[] ships;
}

public class LevelLoader : MonoBehaviour
{
    [SerializeField]
    private TextAsset jsonFile;
    [SerializeField]
    private float unitSize = 10.0f;
    [SerializeField]
    private Material lineMaterial;

    [SerializeField]
    private GameObject leftNFI;
    [SerializeField]
    private GameObject rightNFI;
    [SerializeField]
    private float rayLength = 1000f;
    [SerializeField]
    private float rayAngle = 1f;

    [SerializeField]
    private GameObject wallPrefab;
    [SerializeField]
    private GameObject exitPrefab;
    [SerializeField]
    private GameObject heroShip;
    [SerializeField]
    private GameObject spaceShip2;
    [SerializeField]
    private GameObject spaceShip3;

    private LevelData data;

    private Vector3[] cubeVertices;
    private int[,] cubeEdges = new int[,]
    {
        {0,1}, {1,2}, {2,3}, {3,0},
        {4,5}, {5,6}, {6,7}, {7,4},
        {0,4}, {1,5}, {2,6}, {3,7}
    };

    void Start()
    {
        data = JsonUtility.FromJson<LevelData>(jsonFile.text);
        InitCubeGeometry();
        leftNFI.GetComponent<CurveInteractionCaster>().castDistance = rayLength;
        leftNFI.GetComponent<CurveInteractionCaster>().coneCastAngle = rayAngle;
        rightNFI.GetComponent<CurveInteractionCaster>().castDistance = rayLength;
        rightNFI.GetComponent<CurveInteractionCaster>().coneCastAngle = rayAngle;
        BuildWalls();
        PlaceExit();
        PlaceShip();
    }

    void InitCubeGeometry()
    {
        float h = unitSize * 0.5f;

        cubeVertices = new Vector3[]
        {
            new Vector3(-h, -h, -h),
            new Vector3( h, -h, -h),
            new Vector3( h,  h, -h),
            new Vector3(-h,  h, -h),
            new Vector3(-h, -h,  h),
            new Vector3( h, -h,  h),
            new Vector3( h,  h,  h),
            new Vector3(-h,  h,  h),
        };
    }

    // ====== 벽 6개 생성 ======
    void BuildWalls()
    {
        if (wallPrefab == null || data == null) return;

        int countX = data.boardSize.x * 2;
        int countY = data.boardSize.y * 2;
        int countZ = data.boardSize.z * 2;

        float width = countX * unitSize;
        float height = countY * unitSize;
        float depth = countZ * unitSize;

        float thickness = unitSize;

        void CreateWall(Vector3 localPos, Vector3 localScale)
        {
            GameObject wall = Instantiate(wallPrefab, transform);
            wall.transform.localPosition = localPos;
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale = localScale;
        }

        // 좌 / 우 벽 (YZ 평면)
        CreateWall(
            new Vector3(-width * 0.5f - thickness * 0.5f, 0f, 0f),
            new Vector3(thickness, height, depth)
        );
        CreateWall(
            new Vector3(width * 0.5f + thickness * 0.5f, 0f, 0f),
            new Vector3(thickness, height, depth)
        );

        // 아래 / 위 벽 (XZ 평면)
        CreateWall(
            new Vector3(0f, -height * 0.5f - thickness * 0.5f, 0f),
            new Vector3(width, thickness, depth)
        );
        CreateWall(
            new Vector3(0f, height * 0.5f + thickness * 0.5f, 0f),
            new Vector3(width, thickness, depth)
        );

        // 앞 / 뒤 벽 (XY 평면)
        CreateWall(
            new Vector3(0f, 0f, -depth * 0.5f - thickness * 0.5f),
            new Vector3(width, height, thickness)
        );
        CreateWall(
            new Vector3(0f, 0f, depth * 0.5f + thickness * 0.5f),
            new Vector3(width, height, thickness)
        );
    }

    // ====== Exit 큐브 배치 ======
    void PlaceExit()
    {
        Vector3 localPos = new Vector3(
            (data.exit.x - 0.5f) * unitSize,
            (data.exit.y - 0.5f) * unitSize,
            (data.exit.z - 0.5f) * unitSize
        );

        GameObject exit = Instantiate(exitPrefab, transform);
        exit.transform.localPosition = localPos;
        exit.transform.localRotation = Quaternion.identity;
        exit.transform.localScale = Vector3.one * unitSize / 2;
    }

    // ====== 우주선 배치 ======
    void PlaceShip()
    {
        void CreateShip(ShipData shipData, GameObject prefab)
        {
            GameObject shipPart = Instantiate(prefab, transform);
            Rigidbody rb = shipPart.GetComponent<Rigidbody>();
            Vector3 direction = Vector3.zero;
            switch (shipData.direction)
            {
                case "X":
                    direction.x = 1f;
                    rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
                    break;
                case "Y":
                    direction.y = 1f;
                    rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                    break;
                case "Z":
                    direction.z = 1f;
                    rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY;
                    break;
            }
            Vector3 localPos = new Vector3(
                (shipData.origin.x - 0.5f) * unitSize,
                (shipData.origin.y - 0.5f) * unitSize,
                (shipData.origin.z - 0.5f) * unitSize
            );
            shipPart.transform.localPosition = localPos;
            shipPart.transform.localScale *= unitSize;
            shipPart.transform.rotation = Quaternion.LookRotation(direction);
            SpaceShip ship = shipPart.AddComponent<SpaceShip>();
            ship.Init(
                shipData == data.hero,
                direction,
                unitSize
            );
        }

        CreateShip(data.hero, heroShip);
        foreach (var ship in data.ships)
        {
            GameObject prefab = ship.length == 2 ? spaceShip2 : spaceShip3;
            CreateShip(ship, prefab);
        }
    }

    void OnRenderObject()
    {
        if (data == null || cubeVertices == null) return;

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);

        lineMaterial.SetPass(0);

        GL.Begin(GL.LINES);

        // 전체 보드를 감싸는 박스의 "반" 크기 (unitSize 기준)
        // 예: boardSize.x = 3이면 X축 전체 길이 = 3 * 2 * unitSize
        //     그 반 = 3 * unitSize
        float halfX = unitSize * data.boardSize.x;
        float halfY = unitSize * data.boardSize.y;
        float halfZ = unitSize * data.boardSize.z;

        // 8등분된 각 작은 박스의 중심은
        // -half/2, +half/2 위치
        // => ±(half / 2) = ±(unitSize * boardSize / 2)
        float centerOffsetX = halfX * 0.5f;
        float centerOffsetY = halfY * 0.5f;
        float centerOffsetZ = halfZ * 0.5f;

        // cubeVertices는 현재 unitSize 기준 큐브(한 변 unitSize)라서
        // boardSize.x, y, z 배수로 늘려서
        // 큰 박스를 2x2x2로 나눈 크기에 맞춰줌
        //  (증명: scaleX = boardSize.x 하면 정확히 8등분된 크기가 됨)

        for (int ix = 0; ix < 2; ix++)
        {
            float cx = (ix == 0 ? -centerOffsetX : centerOffsetX);

            for (int iy = 0; iy < 2; iy++)
            {
                float cy = (iy == 0 ? -centerOffsetY : centerOffsetY);

                for (int iz = 0; iz < 2; iz++)
                {
                    float cz = (iz == 0 ? -centerOffsetZ : centerOffsetZ);

                    Vector3 center = new Vector3(cx, cy, cz);

                    for (int e = 0; e < cubeEdges.GetLength(0); e++)
                    {
                        int i0 = cubeEdges[e, 0];
                        int i1 = cubeEdges[e, 1];

                        // 원래 unitSize 기준 (-h~+h)인 버텍스를
                        // boardSize 배 만큼 스케일해서
                        // 전체 박스를 2x2x2로 나눈 큐브로 맞춰줌
                        Vector3 base0 = cubeVertices[i0];
                        Vector3 base1 = cubeVertices[i1];

                        Vector3 v0Local = new Vector3(
                            base0.x * data.boardSize.x,
                            base0.y * data.boardSize.y,
                            base0.z * data.boardSize.z
                        );
                        Vector3 v1Local = new Vector3(
                            base1.x * data.boardSize.x,
                            base1.y * data.boardSize.y,
                            base1.z * data.boardSize.z
                        );

                        Vector3 v0 = center + v0Local;
                        Vector3 v1 = center + v1Local;

                        GL.Vertex(v0);
                        GL.Vertex(v1);
                    }
                }
            }
        }

        GL.End();
        GL.PopMatrix();
    }
}
