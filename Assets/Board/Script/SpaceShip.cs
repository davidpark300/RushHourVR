using System;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.XR;

/*
 * 작성자 : 최성준
 * 파일명 : SpaceShip.cs
 * 
 * 우주선의 로직을 담은 컴포넌트입니다.
 */

// SpaceShip
//   우주선 오브젝트에 부착되는 컴포넌트입니다.
public class SpaceShip : MonoBehaviour
{
    private bool isHero; // 주인공 우주선 여부
    private Vector3 direction; // 이동 방향
    private float unitSize; // 게임판의 단위 크기
    private GameObject particle; // 충돌 파티클 프리팹
    private float clearSpeed; // 클리어 후 우주선이 움직이는 속도
    private GameObject mainCamera; // 메인 카메라
    private float chaseTime; // 클리어 후 카메라가 우주선에 붙을 때 까지 걸리는 시간
    private float endTime; // 클리어 후 카메라가 멈춰있는 시간

    private float endTimer = 0f; // 종료 로직에서 사용할 타이머

    private Rigidbody rb; // 우주선 리지드바디
    private XRGrabInteractable grab; // 우주선 XR 그랩 인터랙터블

    private Transform trackingTarget; // 트래커의 Transform
    private Vector3 lastTrackerPosition; // 이전 프레임의 트래커 위치
    private Quaternion lastTrackerRotation; // 이전 프레임의 트래커 회전(방향)

    // 아래 두 변수는 제가 개인적으로 진행하던 프로젝트에서 사용하던 커맨드 패턴 관련 변수들입니다.
    //   정확한 커맨드 패턴의 형태를 따르지는 않을 수 있습니다.
    // 소스코드는 포함되어 있으나 해당 구현에는 주석을 적지 않았습니다.
    // 여기서는 간단한 동작만 설명하겠습니다.
    private SimpleInvoker invoker; // 우주선 커맨드 인보커
                                                      // 받은 명령을 실행하는 컴포넌트입니다.
                                                      // 기본적으로 한 프레임에 하나의 명령만을 실행하며 우선순위 큐의 형태입니다.
    private Token token; // 토큰
                                      // 외부에서 명령의 실행을 제어할 수 있는 토큰입니다.
                                      // 문자열로 특정 토큰이 존재하는지 확인할 수 있습니다.

    // Init
    //  우주선 컴포넌트를 초기화합니다.
    public void Init(bool isHero, Vector3 direction, float unitSize, GameObject particle, float clearSpeed, GameObject mainCamera, float chaseTime, float endTime)
    {
        this.isHero = isHero;
        this.direction = direction;
        this.unitSize = unitSize;
        this.particle = particle;
        this.clearSpeed = clearSpeed;
        this.mainCamera = mainCamera;
        this.chaseTime = chaseTime;
        this.endTime = endTime;

        rb = GetComponent<Rigidbody>();
        // XRGrabInteractable 컴포넌트를 받아와 이벤트 리스너를 등록합니다.
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);

        // 우주선의 회전을 고정합니다.
        rb.constraints |= RigidbodyConstraints.FreezeRotation;

        // 커맨드 패턴 관련 컴포넌트를 초기화합니다.
        invoker = GetComponent<SimpleInvoker>();
        token = new Token();
    }

    // Update
    //   그랩을 억지로 해제할 때, XRGrabInteractable를 비활성화했다가 다시 활성화합니다.
    void Update()
    {
        if (grab.enabled == false) grab.enabled = true;
    }

    // OnTriggerEnter
    //   우주선이 트리거 콜라이더에 진입했을 때 호출됩니다.
    //   여기서는 주인공 우주선의 승리 조건만을 처리합니다.
    private void OnTriggerEnter(Collider collider)
    {
        // 주인공 우주선이 "Exit" 태그의 콜라이더에 진입했을 때
        if (isHero == true && collider.gameObject.tag == "Exit")
        {
            // 먼저 트래킹 명령을 종료하고 주인공 우주선의 콜라이더를 비활성화한 후 출구 오브젝트를 제거합니다.
            token.Release("trackingCommand");
            GetComponent<BoxCollider>().enabled = false;
            GameObject.Destroy(collider.gameObject);

            // 카메라를 이동시키기 위해 트래킹 타입을 회전 전용으로 변경합니다.
            mainCamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            
            // 우주선과 카메라를 이동시키는 커맨드를 등록합니다.
            // chaseTime초 동안 우주선은 일정 속도로 이동하고 카메라는 우주선을 따라가도록 합니다.
            ICommand chaseCommand = new VectorSettingCommand("chaseCommand",
                new List<float> { 0f }, new(),
                new List<float> { 1f }, (context, param) =>
                {
                    transform.position += direction * Time.deltaTime * clearSpeed;
                    mainCamera.transform.position += (transform.position + transform.up * unitSize * 0.5f - mainCamera.transform.position) * param[0] * Time.deltaTime;
                    return CommandQueue.PROCESS;
                },
                new StaticClock(chaseTime, (param) => param)
            );
            invoker.Do(chaseCommand, 1, CommandQueue.PRIORITY_HIGH);

            // chaseTime초가 지난 후에는 endTime초 동안 우주선은 계속 이동하고 카메라는 우주선에 고정되도록 하는 커맨드를 등록합니다.
            // endTime초가 지나면, 카메라 트래커 설정을 원래대로 돌리고 시작 씬으로 돌아갑니다.
            ICommand endCommand = new VectorSettingCommand("endCommand",
                new List<float> { 0f }, new(),
                new List<float> { 0f }, (context, param) =>
                {
                    transform.position += direction * Time.deltaTime * clearSpeed;
                    mainCamera.transform.position = transform.position + transform.up * unitSize * 0.5f;
                    endTimer += Time.deltaTime;
                    if (endTimer >= endTime)
                    {
                        mainCamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
                        mainCamera.transform.position = new Vector3(0f, 0f, 0f);
                        SceneManager.LoadScene("StartScene");
                        return CommandQueue.END;
                    }
                    return CommandQueue.PROCESS;
                },
                new StaticClock(1f, (param) => param)
            );
            invoker.Do(endCommand, CommandQueue.INF, CommandQueue.PRIORITY_HIGH);
        }
    }

    // OnCollisionEnter
    //   우주선이 벽("Wall" 태그를 가진 게임 오브젝트)과 충돌했을 때 호출됩니다.
    //   여기서 벽은 우주선이 충돌 시 튕겨나가는 효과가 나오는 물체를 의미합니다.
    private void OnCollisionEnter(Collision collision)
    {
        // 그랩된 상태에서 벽과 충돌했을 때
        if (grab.isSelected && grab.firstInteractorSelecting != null && collision.gameObject.tag == "Wall")
        {
            // 이 프레임에서 XRGrabInteractable를 비활성화하여 그랩을 강제로 해제합니다.
            grab.enabled = false;
            // 트래킹 명령을 종료합니다.
            token.Release("trackingCommand");

            // 충돌 위치와 방향을 얻습니다.
            ContactPoint contact = collision.contacts[0];
            Vector3 normal = contact.normal;

            // 충돌 위치에 파티클을 생성하고 1초 후에 제거합니다.
            GameObject newParticle =  Instantiate(particle, contact.point, Quaternion.Euler(normal));
            GameObject.Destroy(newParticle, 1f);

            // 충돌 후 우주선이 조금 후퇴할 위치를 계산합니다. 단위 크기의 0.4배 만큼 후퇴합니다.
            Vector3 dest = transform.position + normal * unitSize * 0.4f;

            // 우주선을 부드럽게 후퇴시키는 커맨드를 등록합니다.
            ICommand backCommand = new VectorSettingCommand("backCommand",
                new List<float> { transform.position.x, transform.position.y, transform.position.z }, new(),
                new List<float> { dest.x, dest.y, dest.z }, (context, param) =>
                {
                    transform.position = new Vector3(param[0], param[1], param[2]);
                    return CommandQueue.PROCESS;
                },
                new StaticClock(0.75f, (param) => param)
            );
            invoker.Do(backCommand, 1, CommandQueue.PRIORITY_NORMAL);
        }
    }

    // OnGrab
    //   우주선이 그랩되면 트래킹 명령을 시작합니다.
    //   트래킹 명령은 기본적으로 매 프레임마다 트래커의 이동과 회전의 변화량에 따라 우주선을 이동시킵니다.
    //   따라서 이전 프레임의 트래커 위치와 회전을 저장하는 변수가 필요합니다.
    private void OnGrab(SelectEnterEventArgs args)
    {
        // 트래커 인터랙터를 얻습니다.
        IXRSelectInteractor interactor = args.interactorObject;

        // 이전 프레임의 트래커 정보를 저장하는 변수를 초기화합니다.
        trackingTarget = interactor.transform;
        lastTrackerPosition = trackingTarget.position;
        lastTrackerRotation = trackingTarget.rotation;

        // 트래킹 명령을 등록하고 토큰을 설정합니다.
        token.Set("trackingCommand");
        ICommand trackingCommand = new VectorSettingCommand("trackingCommand",
            new List<float> { 1f }, new(),
            new List<float> { 1f }, (context, param) =>
            {
                // 토큰이 해제되었으면 명령을 종료합니다.
                if (token.Get("trackingCommand") == false) return CommandQueue.END;

                // 물체 위치 P
                Vector3 P = transform.position;

                // 1. 트래커의 위치 변화에 따른 우주선 이동 처리

                // 현재 트래커 위치와 위치 변화량을 구합니다.
                Vector3 currentTrackerPos = trackingTarget.position;
                Vector3 trackerDelta = currentTrackerPos - lastTrackerPosition;

                if (trackerDelta.sqrMagnitude > Mathf.Epsilon)
                {
                    // 트래커의 위치 변화량(trackerDelta)과 우주선의 움직임 방향(direction)의 내적(dot product)을 구합니다.
                    float moveWeight = Vector3.Dot(trackerDelta, direction);

                    // 카메라-우주선 거리와 카메라-트래커 거리를 구합니다.
                    float distCamToShip = Vector3.Distance(mainCamera.transform.position, P);
                    float distCamToTracker = Vector3.Distance(mainCamera.transform.position, currentTrackerPos);

                    // 카메라-트래커가 거의 같은 위치에 있지 않은 경우에만(나눗셈에서 사용하기 때문에) 우주선을 이동시킵니다.
                    if (distCamToTracker > 0.001f)
                    {
                        // 실제 물체까지의 거리와 트래커까지의 거리 비율에 따라 이동량을 조절합니다.
                        // 멀리서 조작할 때 더 많이 이동시키도록 하기 위함입니다.
                        float scaledMove = moveWeight * (distCamToShip / distCamToTracker);
                        rb.MovePosition(P + direction * scaledMove);
                    }
                }

                // 2. 트래커의 회전 변화에 따른 우주선 이동 처리

                // 현재 트래커 방향을 구합니다.
                Quaternion currentTrackerRot = trackingTarget.rotation;

                // 현재(이동 후)의 트래커에서 물체로 향하는 벡터
                Vector3 trackerToObj = P - currentTrackerPos;
                // 거리가 너무 가까우면 각도 이동에 대한 처리가 모호해지므로 무시
                if (trackerToObj.sqrMagnitude < 0.001f)
                {
                    return CommandQueue.PROCESS;
                }
                // 트래커에서 물체로 향하는 벡터의 단위 벡터(평면의 법선 벡터로 사용합니다.)
                Vector3 planeNormal = trackerToObj.normalized;

                // 이전 프레임과 현재 프레임의 트래커가 가리키는 전방 벡터
                Vector3 prevForward = lastTrackerRotation * Vector3.forward;
                Vector3 currForward = currentTrackerRot * Vector3.forward;

                // 위에서 구한 평면과 두 개의 전방 벡터의 교차점을 구합니다.
                // RayPlaneIntersection는 레이(시작점과 방향 벡터)와 평면의 교차점을 계산하는 함수입니다.
                // 교차점이 없다고 판단할 경우 false를 반환합니다.
                Vector3 Q_prev, Q_curr;
                bool hitPrev = RayPlaneIntersection(lastTrackerPosition, prevForward, P, planeNormal, out Q_prev);
                bool hitCurr = RayPlaneIntersection(currentTrackerPos, currForward, P, planeNormal, out Q_curr);

                // 두 교차점이 모두 존재할 때만 우주선을 이동시킵니다.
                if (hitPrev && hitCurr)
                {
                    // 트래커의 레이가 물체를 지나는 평면 위에서 움직인 벡터(변하량)를 구합니다.
                    Vector3 deltaQ = Q_curr - Q_prev;

                    // 너무 작은 변화량은 적용하지 않습니다.
                    if (deltaQ.sqrMagnitude > 0.00000001f)
                    {
                        // 트래커의 각도 변화량(deltaQ)과 우주선의 움직임 방향(direction)의 내적(dot product)을 구합니다.
                        float moveWeight = Vector3.Dot(deltaQ, direction);

                        // 내적 결과(해당 방향으로 실제로 이동하는거리)가 거의 0이 아닐 때만 우주선을 이동시킵니다.
                        if (Mathf.Abs(moveWeight) > 0.0001f)
                        {
                            rb.MovePosition(P + direction * moveWeight);
                        }
                    }
                }

                // 다음 프레임에서의 계산을 위해 현재 트래커 정보를 저장합니다.
                lastTrackerPosition = currentTrackerPos;
                lastTrackerRotation = currentTrackerRot;

                return CommandQueue.PROCESS;
            },
            new StaticClock(0f, (param) => param)
        );
        invoker.Do(trackingCommand, CommandQueue.INF, CommandQueue.PRIORITY_LOW);
    }

    // RayPlaneIntersection
    //   레이(시작점(트래커)로부터 평면으로 향하는 반직선(시작점과 벡터를 가짐))와 평면의 교차점을 계산합니다.
    //   rayOrigin : 레이의 시작점(트래커 위치)
    //   rayDir : 레이의 방향 (정규화) 벡터
    //   planePoint : 평면 위의 한 점
    //   planeNormal : 평면의 법선 (정규화) 벡터
    //   hitPoint : 교차점 (출력 변수)
    bool RayPlaneIntersection(
    Vector3 rayOrigin, Vector3 rayDir,
    Vector3 planePoint, Vector3 planeNormal,
    out Vector3 hitPoint)
    {
        // 레이의 방향과 평면의 법선 벡터의 내적입니다.
        float denom = Vector3.Dot(rayDir, planeNormal);
        // 내적의 결과값이 너무 작을 경우(거의 수직이거나 두 벡터가 매우 작을 경우)
        if (Mathf.Abs(denom) < 0.001f)
        {
            hitPoint = Vector3.zero;
            // 교차점이 없다고 판단합니다.
            return false;
        }

        // t는 레이의 시작점에서 교차점까지의 거리입니다.
        //   hitPoint = rayOrigin + rayDir * t 로 교차점을 계산합니다.
        //   이 점이 평면 위에 있어야 하므로 평면 위 임의의 점과 교차점의 벡터(hitPoint - planePoint) V와
        //   평면의 법선 벡터(planeNormal)의 내적이 0이 되어야 합니다. (hitPoint - planePoint) dot  V = 0
        //   위의 교차점을 대입하면 (rayOrigin + rayDir * t - planePoint) dot  V = 0 입니다.
        //   내적은 합연산에 대해 분배법칙이 성립하고, t는 스칼라곱이기 때문에 정리하면 다음과 같습니다.
        //   (rayOrigin - planePoint) dot V + (rayDir dot V) * t = 0
        //   따라서 t에 대해 정리면 t = (rayOrigin - planePoint) dot V / (rayDir dot V) 가 됩니다.
        float t = Vector3.Dot(planePoint - rayOrigin, planeNormal) / denom;

        // 교차점이 레이의 시작점 뒤에 있을 경우
        if (t < 0f)
        {
            hitPoint = Vector3.zero;
            // 교차점이 없다고 판단합니다.
            return false;
        }

        // 레이의 시작점(트레커 위치)에서 레이의 방향으로 t만큼 이동한 위치가 교차점입니다.
        hitPoint = rayOrigin + rayDir * t;
        return true;
    }

    // OnRelease
    //   우주선이 그랩이 해제되면 트래킹 명령을 종료합니다.
    private void OnRelease(SelectExitEventArgs args)
    {
        token.Release("trackingCommand");
    }
}
