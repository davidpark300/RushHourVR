using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

/*
 * 작성자 : 최성준
 * 파일명 : LevelLoader.cs
 * 
 * 게임 오브젝트를 중심으로 게임판을 생성하는 컴포넌트입니다.
 */

// ShipData
//   우주선의 초기 정보를 담는 클래스입니다.
[Serializable]
public class ShipData
{
    public Vector3Int origin; // 우주선의 시작 좌표
    public string direction; // 우주선의 진행 방향 (X, Y, Z)
    public int length; // 우주선의 길이
}

// LevelData
//   게임 판의 정보를 담는 클래스입니다.
[Serializable]
public class LevelData
{
    public Vector3Int boardSize; // 게임판의 크기
                                                  // 이때, 게임판의 각 길이(XYZ)는 이 값의 2배가 됩니다. 예를 들어, boardSize가 (3,4,5)라면 게임판의 실제 크기는 (6,8,10)이 됩니다.
                                                  // 게임 오브젝트를 중심으로 +- 방향으로 각 길이만큼의 크기로 게임판을 생성합니다.
    public Vector3Int exit; // 출구의 좌표
    public ShipData hero; // 주인공 우주선의 정보
    public ShipData[] ships; // 다른 우주선들의 정보
}

// LevelLoader
//   게임판을 생성하고 윤곽선을 그리는 컴포넌트입니다.
//   이때, 윤곽선은 GL 클래스를 사용하여 그립니다.
//   총 8개의 큐브로 게임판의 윤곽선을 그리며, 각 큐브는 게임판을 각 축에 평행한 방향으로 동일하게 8등분한 위치에 배치됩니다.
public class LevelLoader : MonoBehaviour
{
    [SerializeField]
    private TextAsset jsonFile; // 레벨 데이터가 담긴 JSON 파일
    [SerializeField]
    private float unitSize = 3f; // 게임판의 단위 크기
    [SerializeField]
    private Material lineMaterial; // 게임판의 선을 그리기 위한 Material
    [SerializeField]
    private GameObject explosionParticle; // 우주선 충돌 파티클 프리팹
    [SerializeField]
    private float clearSpeed = 3f; // 클리어 후 우주선이 움직이는 속도
    [SerializeField]
    private GameObject mainCamera; // 메인 카메라
    [SerializeField]
    private float chaseTime = 3f; // 클리어 후 카메라가 우주선에 붙을 때 까지 걸리는 시간
    [SerializeField]
    private float endTime = 2f; // 클리어 후 카메라가 멈춰있는 시간

    [SerializeField]
    private GameObject leftNFI; // 왼손 NFI
    [SerializeField]
    private GameObject rightNFI; // 오른손 NFI
    [SerializeField]
    private float rayLength = 1000f; // NFI 레이 길이
    [SerializeField]
    private float rayAngle = 1f; // NFI 레이 각도

    [SerializeField]
    private GameObject wallPrefab; // 벽 프리팹
    [SerializeField]
    private GameObject exitPrefab; // 출구 프리팹
    [SerializeField]
    private GameObject heroShip; // 주인공 우주선 프리팹
    [SerializeField]
    private GameObject spaceShip2; // 길이 2 우주선 프리팹
    [SerializeField]
    private GameObject spaceShip3; // 길이 3 우주선 프리팹

    private LevelData data; // 레벨 데이터


    // 큐브의 모서리 인덱스 배열
    int[,] cubeEdges;
    private Vector3[] cubeVertices; // 큐브의 정점 위치 배열

    // Start
    //   게임판을 생성합니다.
    void Start()
    {
        // JSON 파일에서 레벨 데이터를 읽어옵니다.
        data = JsonUtility.FromJson<LevelData>(jsonFile.text);
        // 게임판의 윤곽선을 그리기 위한 데이터를 초기화합니다.
        InitCubeGeometry();
        // NFI의 레이 캐스트 설정을 초기화합니다.
        leftNFI.GetComponent<CurveInteractionCaster>().castDistance = rayLength;
        leftNFI.GetComponent<CurveInteractionCaster>().coneCastAngle = rayAngle;
        rightNFI.GetComponent<CurveInteractionCaster>().castDistance = rayLength;
        rightNFI.GetComponent<CurveInteractionCaster>().coneCastAngle = rayAngle;
        // 게임판의 벽을 생성합니다.
        BuildWalls();
        // 출구를 배치합니다.
        PlaceExit();
        // 우주선들을 배치합니다.
        PlaceShip();
    }

    // InitCubeGeometry
    //   큐브의 정점 위치 배열을 초기화합니다.
    //   큐브의 크기는 unitSize에 따라 결정되며, 중심이 원점에 위치하도록 설정됩니다.
    void InitCubeGeometry()
    {
        // 큐브의 인덱스(i in [0,7])에 대해서 12개 모서리의 정점 쌍을 정의합니다.
        cubeEdges = new int[,]
        {
            {0,1}, {1,2}, {2,3}, {3,0},
            {4,5}, {5,6}, {6,7}, {7,4},
            {0,4}, {1,5}, {2,6}, {3,7}
        };

        // 큐브의 정점 위치를 정의합니다.
        // 위 인덱스에 대응되는 점이며, 중심으로부터 떨어진 상대 위치입니다.
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

    // BuildWalls
    //   게임판의 벽을 생성합니다.
    //   벽은 게임판의 크기에 맞게 직육면체 모양의 게임판을 감싸는 형태로 배치되며, 두께는 unitSize와 동일하게 설정됩니다.
    void BuildWalls()
    {
        float width = data.boardSize.x * unitSize * 2;
        float height = data.boardSize.y * unitSize * 2;
        float depth = data.boardSize.z * unitSize * 2;

        float thickness = unitSize;

        // CreateWall
        //   벽을 생성하는 내부 함수입니다.
        //   localPos: 벽의 로컬 위치
        //   localScale: 벽의 로컬 크기
        //   회전은 기본값(Quaternion.identity)으로 설정됩니다.
        void CreateWall(Vector3 localPos, Vector3 localScale)
        {
            GameObject wall = Instantiate(wallPrefab, transform);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = localScale;
            wall.transform.localRotation = Quaternion.identity;
        }

        // YZ
        CreateWall(
            new Vector3(-width * 0.5f - thickness * 0.5f, 0f, 0f),
            new Vector3(thickness, height, depth)
        );
        CreateWall(
            new Vector3(width * 0.5f + thickness * 0.5f, 0f, 0f),
            new Vector3(thickness, height, depth)
        );

        // XZ
        CreateWall(
            new Vector3(0f, -height * 0.5f - thickness * 0.5f, 0f),
            new Vector3(width, thickness, depth)
        );
        CreateWall(
            new Vector3(0f, height * 0.5f + thickness * 0.5f, 0f),
            new Vector3(width, thickness, depth)
        );

        // XY
        CreateWall(
            new Vector3(0f, 0f, -depth * 0.5f - thickness * 0.5f),
            new Vector3(width, height, thickness)
        );
        CreateWall(
            new Vector3(0f, 0f, depth * 0.5f + thickness * 0.5f),
            new Vector3(width, height, thickness)
        );
    }

    // PlaceExit
    //   출구를 게임판에 배치합니다.
    void PlaceExit()
    {
        // 출구의 로컬 위치를 계산합니다.
        Vector3 localPos = new Vector3(
            (data.exit.x - 0.5f) * unitSize,
            (data.exit.y - 0.5f) * unitSize,
            (data.exit.z - 0.5f) * unitSize
        );

        // 출구 프리팹을 생성하고 위치, 크기, 회전을 설정합니다.
        // 회전은 기본값(Quaternion.identity)으로 설정됩니다.
        GameObject exit = Instantiate(exitPrefab, transform);
        exit.transform.localPosition = localPos;
        exit.transform.localScale = Vector3.one * unitSize / 2;
        exit.transform.localRotation = Quaternion.identity;
    }

    // PlaceShip
    //   우주선들을 게임판에 배치합니다.
    void PlaceShip()
    {
        // CreateShip
        //  우주선을 생성하는 내부 함수입니다.
        //  shipData: 우주선의 초기 정보
        //  prefab: 우주선 프리팹
        void CreateShip(ShipData shipData, GameObject prefab)
        {
            // 우주선 프리팹을 생성하고 Rigidbody 컴포넌트를 가져옵니다.
            GameObject shipPart = Instantiate(prefab, transform);
            Rigidbody rb = shipPart.GetComponent<Rigidbody>();

            // 우주선의 진행 방향을 얻고, 해당 축을 제외한 나머지 축의 이동을 고정합니다.
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

            // 우주선의 로컬 위치, 크기, 회전을 설정합니다.
            Vector3 localPos = new Vector3(
                (shipData.origin.x - 0.5f) * unitSize,
                (shipData.origin.y - 0.5f) * unitSize,
                (shipData.origin.z - 0.5f) * unitSize
            );
            shipPart.transform.localPosition = localPos;
            shipPart.transform.localScale *= unitSize;
            shipPart.transform.rotation = Quaternion.LookRotation(direction);

            // SpaceShip 컴포넌트를 추가하고 초기화합니다.
            SpaceShip ship = shipPart.AddComponent<SpaceShip>();
            ship.Init(
                shipData == data.hero,
                direction,
                unitSize,
                explosionParticle,
                clearSpeed,
                mainCamera,
                chaseTime,
                endTime
            );
        }

        // 주인공 우주선과 다른 우주선들을 생성합니다.
        CreateShip(data.hero, heroShip);
        foreach (var ship in data.ships)
        {
            GameObject prefab = ship.length == 2 ? spaceShip2 : spaceShip3;
            CreateShip(ship, prefab);
        }
    }

    // OnRenderObject
    //   유니티의 렌더링 파이프라인에서 호출되는 콜백 함수입니다.
    //   게임판의 윤곽선을 그립니다.
    void OnRenderObject()
    {
        // 이전 모델 행렬을 스택에 저장합니다.
        GL.PushMatrix();
        // 스택의 탑 행렬에 현재 게임 오브젝트의 로컬 모델 행렬을 곱해 사용합니다.
        GL.MultMatrix(transform.localToWorldMatrix);

        // 설정한 선 Material의 첫 번째 패스(컬러 등)을 사용합니다.
        lineMaterial.SetPass(0);

        // 선 그리기를 시작합니다.
        GL.Begin(GL.LINES);

        // 게임판의 크기의 절판(각 큐브의 모서리 크기)를 구합니다.
        float halfX = data.boardSize.x * unitSize / 2f;
        float halfY = data.boardSize.y * unitSize / 2f;
        float halfZ = data.boardSize.z * unitSize / 2f;

        // 8개의 큐브에 대해 윤곽선을 그립니다.
        // 각 큐브의 중심 위치에 대하여 그리기를 수행합니다.
        for (int ix = 0; ix < 2; ix++) {
            for (int iy = 0; iy < 2; iy++) {
                for (int iz = 0; iz < 2; iz++) {
                    Vector3 center = new Vector3(
                        halfX * (1 - 2 * ix),
                        halfY * (1 - 2 * iy),
                        halfZ * (1 - 2 * iz)
                     );

                    // 각 큐브의 모서리에 대해 선을 그립니다.
                    for (int e = 0; e < cubeEdges.GetLength(0); e++) {
                        // 각 모서리를 구성하는 두 정점의 인덱스를 얻습니다.
                        int i0 = cubeEdges[e, 0];
                        int i1 = cubeEdges[e, 1];

                        // 각 정점의 로컬 위치를 인덱스로 받아옵니다.
                        Vector3 base0 = cubeVertices[i0];
                        Vector3 base1 = cubeVertices[i1];

                        // 보드 크기에 따라 정점의 로컬 위치를 실제 크기로 변환합니다.
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

                        // 실제 위치를 계산합니다.
                        Vector3 v0 = center + v0Local;
                        Vector3 v1 = center + v1Local;

                        // 선을 그립니다.
                        GL.Vertex(v0);
                        GL.Vertex(v1);
                    }
                }
            }
        }

        // 선 그리기를 종료합니다.
        GL.End();
        // 이 뒤로 이전 모델을 유지하기 위해 스택에서 현재 게임 오브젝트의 모델을 제거합니다.
        GL.PopMatrix();
    }
}
