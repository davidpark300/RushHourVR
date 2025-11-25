using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabLogic : MonoBehaviour
{
    private XRGrabInteractable grab;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    public void OnGrab(SelectEnterEventArgs args)
    {
        Debug.Log("Grabbed");
    }

    public void OnRelease(SelectExitEventArgs args)
    {
        Debug.Log("unGrabbed");
    }
}
