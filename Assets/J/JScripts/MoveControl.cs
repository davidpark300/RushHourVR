using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MoveControl : MonoBehaviour
{
    // 카메라
    [SerializeField]
    private GameObject myHead;

    // 탑승하고 있는 우주선 이동 방향
    Vector3 moveDircetion;

    // 우주선 이동 속력
    [SerializeField]
    float movespeed = 4f;

    // 마우스 감도
    [SerializeField]
    public float mouseSensitivity = 100f;

    // 카메라의 상하 시야각 제한을 위한 변수
    private float xRotation = 0f;

    private Vector2 lookInput;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(moveDircetion.normalized * movespeed * Time.deltaTime, Space.World);
        //if (moveDircetion.magnitude > 0.1f)
        //{
        //    Quaternion q = Quaternion.LookRotation(moveDircetion);
        //    transform.rotation = Quaternion.Lerp(transform.rotation, q, 0.1f);
        //}

        // 마우스 좌우 이동
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        // 마우스 상하 이동
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;

        // 시야각 제한 (Look Limit). 과도하게 고개를 숙이거나 젖히지 않도록 합니다.
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // 카메라에 회전 적용 (카메라가 상하로 회전)
        // Quaternion.Euler는 오일러 각을 쿼터니언으로 변환합니다.
        myHead.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    public void OnLook(InputValue value)
    {
        // 마우스 델타 값 (Vector2)을 직접 읽어옵니다.
        lookInput = value.Get<Vector2>();
        Debug.Log("마우스 델타 값 : " + lookInput);
    }

    public void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        if (input != null)
        {
            moveDircetion = new Vector3(input.x, 0f, input.y);
            Debug.Log($"vector : {input.magnitude}");
        }

    }
}
