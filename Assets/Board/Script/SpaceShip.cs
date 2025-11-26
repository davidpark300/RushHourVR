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

        //rb.constraints |= RigidbodyConstraints.FreezeRotation;
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

                // ----- 2. 트래커 회전 -> direction 축을 따라 전/후 이동 -----
                // 직전/현재 forward 벡터 사이의 회전을, 카메라의 right 축 기준 '피치'로 사용
                Vector3 prevForward = lastTrackerRotation * Vector3.forward;
                Vector3 currForward = currentTrackerRot * Vector3.forward;

                // 카메라의 오른쪽 축을 기준으로 얼마나 고개를 숙였는지/들었는지(피치) 각도
                float angleAroundCamRight = Vector3.SignedAngle(prevForward, currForward, playerCamera.right);

                if (Mathf.Abs(angleAroundCamRight) > 0.1f)
                {
                    float distCamToShip = Vector3.Distance(playerCamera.position, transform.position);
                    float distCamToTracker = Vector3.Distance(playerCamera.position, currentTrackerPos);

                    if (distCamToTracker > 0.001f)
                    {
                        // 회전량(도)을 라디안으로 바꾸고, 거리 비율을 곱해서 이동량으로 사용
                        float rotationInput = angleAroundCamRight * Mathf.Deg2Rad;
                        float scaledMoveFromRot = rotationInput * (distCamToShip / distCamToTracker);

                        // 위로 들면(+각도) 앞으로, 내리면(-각도) 뒤로
                        rb.MovePosition(transform.position + dir * scaledMoveFromRot);
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

    private void OnRelease(SelectExitEventArgs args)
    {
        token.Release("trackingCommand");
    }
}
