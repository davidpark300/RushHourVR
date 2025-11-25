using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

public enum Axis
{
    X, Y, Z
}

[Serializable]
public class ShipData
{
    public Vector3Int origin;
    public Axis direction;
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
                case Axis.X:
                    direction.x = 1f;
                    rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
                    break;
                case Axis.Y:
                    direction.y = 1f;
                    rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                    break;
                case Axis.Z:
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
        if (data == null) return;

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);

        lineMaterial.SetPass(0);

        GL.Begin(GL.LINES);
        GL.Color(Color.white);

        int countX = data.boardSize.x * 2;
        int countY = data.boardSize.y * 2;
        int countZ = data.boardSize.z * 2;

        float startX = -(countX - 1) * 0.5f * unitSize;
        float startY = -(countY - 1) * 0.5f * unitSize;
        float startZ = -(countZ - 1) * 0.5f * unitSize;

        for (int ix = 0; ix < countX; ix++)
        {
            float x = startX + ix * unitSize;
            for (int iy = 0; iy < countY; iy++)
            {
                float y = startY + iy * unitSize;
                for (int iz = 0; iz < countZ; iz++)
                {
                    float z = startZ + iz * unitSize;

                    Vector3 center = new Vector3(x, y, z);
                    for (int e = 0; e < cubeEdges.GetLength(0); e++)
                    {
                        int i0 = cubeEdges[e, 0];
                        int i1 = cubeEdges[e, 1];

                        Vector3 v0 = center + cubeVertices[i0];
                        Vector3 v1 = center + cubeVertices[i1];

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
