using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RControllerSecondButton : MonoBehaviour
{
    // InputAction 설정한 것 저장할 변수
    public InputActionReference secondaryButtonAction;

    // 하강 속력
    public float descentSpeed = 1.5f;

    // XR Origin에 있는 Character Controller 저장할 변수
    public CharacterController characterController;

    private void Awake()
    {
        if (characterController == null)
        {
            Debug.LogError("CharacterController 컴포넌트 등록");
        }
    }

    private void Update()
    {
        float inputValue = secondaryButtonAction.action.ReadValue<float>();

        if (inputValue > 0.1f)
        {
            // 하강 방향 벡터 생성 (Y축 마이너스 방향)
            Vector3 descentVector = Vector3.down * descentSpeed * Time.deltaTime;

            characterController.Move(descentVector);
        }
    }

    private void OnEnable()
    {
        // 스크립트가 활성화될 때 액션을 활성화
        secondaryButtonAction.action.Enable();

        // 버튼이 눌렸을 때 호출될 메서드를 등록
        secondaryButtonAction.action.performed += OnSecondaryButtonPerformed;
    }

    private void OnDisable()
    {
        // 스크립트가 비활성화될 때 액션을 비활성화
        secondaryButtonAction.action.performed -= OnSecondaryButtonPerformed;
        secondaryButtonAction.action.Disable();
    }

    // Secondary Button이 눌렸을 때 호출되는 메서드
    private void OnSecondaryButtonPerformed(InputAction.CallbackContext context)
    {
        // 버튼이 실제로 눌렸는지 확인
        if (context.performed)
        {
            Debug.Log("Right Controller Secondary Button (B/Y) Pressed!");
        }

        
    }
}
