using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetMovement : MonoBehaviour
{
    // 초당 자전 속력
    public float rotationSpeed = 30f;

    // 초당 공전 속력
    public float revolutionSpeed = 10f;

    // 공전 축 설정
    public Vector3 revolutionAxis = Vector3.up;

    // 자전 축 설정
    public Vector3 rotationAxis = Vector3.up;

    private void Update()
    {
        // 자전
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);

        // 공전
        if (transform.parent != null)
        {
            transform.parent.Rotate(revolutionAxis, revolutionSpeed * Time.deltaTime, Space.Self);
        }
    }
}
