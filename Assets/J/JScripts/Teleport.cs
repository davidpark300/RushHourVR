using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Teleport : MonoBehaviour
{
    [SerializeField]
    private GameObject mySpaceShip;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void teleportSelected(GameObject planet)
    {
        mySpaceShip.transform.position = planet.transform.position;
        mySpaceShip.transform.rotation = planet.transform.rotation;
    }
}
