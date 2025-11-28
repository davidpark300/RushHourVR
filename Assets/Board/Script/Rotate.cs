using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 작성자 : 최성준
 * 파일명 : Rotate.cs
 * 
 * y축을 기준으로 오브젝트를 회전시키는 스크립트입니다.
 */
public class Rotate : MonoBehaviour
{
    [SerializeField]
    private float rotationSpeed = 90f;
    void Update()
    {
        gameObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
