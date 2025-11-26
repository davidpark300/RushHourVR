using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LControllerPrimaryButton : MonoBehaviour
{
    public GameObject myHUD;
    public InputActionReference primaryButtonAction;
    private Boolean isActivated;

    private void Awake()
    {
        isActivated = true;
    }
    private void OnEnable()
    {
        // 스크립트가 활성화될 때 액션을 활성화
        primaryButtonAction.action.Enable();

        // 버튼이 눌렸을 때 호출될 메서드 등록
        primaryButtonAction.action.performed += OnPrimaryButtonPerformed;
    }

    private void OnDisable()
    {
        // 스크립트가 비활성화될 때 액션을 비활성화
        primaryButtonAction.action.performed -= OnPrimaryButtonPerformed;
        primaryButtonAction.action.Disable();
    }

    // Primary Button이 눌렸을 때 호출되는 메서드
    private void OnPrimaryButtonPerformed(InputAction.CallbackContext context)
    {
        // 버튼이 실제로 눌렸는지 확인
        if (context.performed)
        {
            Debug.Log("Left Controller Primary Button (B/Y) Pressed!");
        }
        isActivated = !isActivated;
        myHUD.SetActive(isActivated);

    }
}
