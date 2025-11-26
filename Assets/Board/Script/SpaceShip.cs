using System;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SpaceShip : MonoBehaviour
{
    private bool isHero = false;
    private Vector3 direction;
    private float unitSize;

    private Rigidbody rb;
    private XRGrabInteractable grab;

    private SimpleInvoker invoker;
    private Token token;

    // 여기에 트래킹 버퍼 데이터 추가
    private Transform trackingTarget;        // 어떤 트래커가 이 우주선을 잡았는지
    private Vector3 lastTrackerPosition;     // 직전 프레임의 트래커 위치
    private Quaternion lastTrackerRotation;  // 직전 프레임의 트래커 회전
    private Transform playerCamera;          // 플레이어 카메라 (거리 비율 계산용)
    public void Init(bool isHero, Vector3 direction, float unitSize)
    {
        this.isHero = isHero;
        this.direction = direction;
        this.unitSize = unitSize;

        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);

        invoker = GetComponent<SimpleInvoker>();
        token = new Token();

        rb.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        if (grab.enabled == false) grab.enabled = true;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (isHero == true && collider.gameObject.tag == "Exit")
        {
            Debug.Log("You Win!");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (grab.isSelected && grab.firstInteractorSelecting != null && collision.gameObject.tag == "Wall")
        {
            var interactor = grab.firstInteractorSelecting;
            grab.interactionManager.SelectExit(interactor, grab);
            grab.enabled = false;

            token.Release("trackingCommand");

            ContactPoint contact = collision.contacts[0];
            Vector3 normal = contact.normal;

            Vector3 dest = transform.position + normal * unitSize * 0.2f;
            ICommand backCommand = new VectorSettingCommand("backCommand",
                new List<float> { transform.position.x, transform.position.y, transform.position.z }, new(),
                new List<float> { dest.x, dest.y, dest.z }, (context, param) =>
                {
                    transform.position = new Vector3(param[0], param[1], param[2]);
                    return CommandQueue.PROCESS;
                },
                new StaticClock(0.4f, (param) => param)
            );
            invoker.Do(backCommand, 1, CommandQueue.PRIORITY_HIGH);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        IXRSelectInteractor interactor = args.interactorObject;
        // 여기서 트래킹 버퍼 데이터 초기화
        trackingTarget = interactor.transform;
        lastTrackerPosition = trackingTarget.position;
        lastTrackerRotation = trackingTarget.rotation;
        playerCamera = Camera.main != null ? Camera.main.transform : null;

        token.Set("trackingCommand");
        ICommand trackingCommand = new VectorSettingCommand("trackingCommand",
            new List<float> { 1f }, new(),
            new List<float> { 1f }, (context, param) =>
            {
                // 여기에 트래킹 로직 작성
                if (token.Get("trackingCommand") == false) return CommandQueue.END;
                if (trackingTarget == null || playerCamera == null) return CommandQueue.PROCESS;

                // ----- 1. 트래커의 선형 이동 -> direction 방향 이동 -----
                Vector3 currentTrackerPos = trackingTarget.position;
                Quaternion currentTrackerRot = trackingTarget.rotation;

                Vector3 trackerDelta = currentTrackerPos - lastTrackerPosition;

                // direction이 0벡터면 안전하게 transform.forward를 사용
                Vector3 dir = direction.sqrMagnitude > 0f ? direction.normalized : transform.forward;

                if (trackerDelta.sqrMagnitude > Mathf.Epsilon)
                {
                    // 트래커 이동을 direction 축으로 투영
                    float moveAlongDirOnTracker = Vector3.Dot(trackerDelta, dir);

                    float distCamToShip = Vector3.Distance(playerCamera.position, transform.position);
                    float distCamToTracker = Vector3.Distance(playerCamera.position, currentTrackerPos);

                    if (distCamToTracker > 0.001f)
                    {
                        // 조건 2: (direction과 내적한 값) * (거리비율)
                        float scaledMove = moveAlongDirOnTracker * (distCamToShip / distCamToTracker);
                        rb.MovePosition(transform.position + dir * scaledMove);
                    }
                }

                // ----- 2. 트래커 회전 -> 평면 위 위치 Q 변화량을 direction으로 투영 -----

                // 물체 위치 P
                Vector3 P = transform.position;

                // 현재 프레임 기준 트래커 위치
                Vector3 T_prev = lastTrackerPosition;
                Vector3 T_curr = currentTrackerPos;

                // "트래커 -> 물체" 방향과 평행한 법선을 가지는 평면 (P를 지남)
                //   planeNormal 은 트래커-물체 선분과 평행한 벡터 (법선)
                Vector3 trackerToObj = P - T_curr;                // (물체 - 트래커)
                if (trackerToObj.sqrMagnitude < 1e-6f)
                {
                    // 트래커와 물체가 거의 같은 위치면 계산이 불안정해지므로 스킵
                    return CommandQueue.PROCESS;
                }
                Vector3 planeNormal = trackerToObj.normalized;
                Vector3 planePoint = P;

                // 이전/현재 트래커 forward
                Vector3 prevForward = lastTrackerRotation * Vector3.forward;
                Vector3 currForward = currentTrackerRot * Vector3.forward;

                // 평면 위에서 트래커가 "가리키는 점" Q_prev, Q_curr를 구함
                Vector3 Q_prev, Q_curr;
                bool hitPrev = RayPlaneIntersection(T_prev, prevForward, planePoint, planeNormal, out Q_prev);
                bool hitCurr = RayPlaneIntersection(T_curr, currForward, planePoint, planeNormal, out Q_curr);

                if (hitPrev && hitCurr)
                {
                    // 평면 위에서 Q가 얼마나 움직였는지
                    Vector3 deltaQ = Q_curr - Q_prev;

                    if (deltaQ.sqrMagnitude > 1e-8f)
                    {
                        // 물체가 움직일 방향(dir)에 투영한 스칼라만 사용
                        // dir이 정규화되어 있다고 가정 (아니면 normalized 써도 됨)
                        float moveAmount = Vector3.Dot(deltaQ, dir);

                        if (Mathf.Abs(moveAmount) > 1e-4f)
                        {
                            rb.MovePosition(P + dir * moveAmount);
                        }
                    }
                }

                // ----- 3. 다음 프레임을 위한 버퍼 갱신 -----
                lastTrackerPosition = currentTrackerPos;
                lastTrackerRotation = currentTrackerRot;

                // 계속 트래킹
                return CommandQueue.PROCESS;
            },
            new StaticClock(0f, (param) => param)
        );
        invoker.Do(trackingCommand, CommandQueue.INF, CommandQueue.PRIORITY_NORMAL);
    }

    bool RayPlaneIntersection(
    Vector3 rayOrigin, Vector3 rayDir,
    Vector3 planePoint, Vector3 planeNormal,
    out Vector3 hitPoint)
    {
        float denom = Vector3.Dot(rayDir, planeNormal);

        // 레이가 평면과 거의 평행한 경우
        if (Mathf.Abs(denom) < 1e-4f)
        {
            hitPoint = Vector3.zero;
            return false;
        }

        float t = Vector3.Dot(planePoint - rayOrigin, planeNormal) / denom;

        // t < 0이면 평면이 레이의 "뒤쪽"에 있음
        if (t < 0f)
        {
            hitPoint = Vector3.zero;
            return false;
        }

        hitPoint = rayOrigin + rayDir * t;
        return true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        token.Release("trackingCommand");
    }
}
