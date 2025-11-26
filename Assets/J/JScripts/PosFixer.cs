using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PosFixer: MonoBehaviour
{
    public Transform originPos;
    private void Update()
    {
        transform.position = originPos.position;
    }
}
