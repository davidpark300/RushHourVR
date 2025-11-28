using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Teleport : MonoBehaviour
{
    [SerializeField]
    private GameObject mySpaceShip;
    
    public void teleportSelected(GameObject planet)
    {
        mySpaceShip.transform.position = planet.transform.position;
        mySpaceShip.transform.rotation = planet.transform.rotation;
    }
}
