using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneLoad : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void planetGrabbed(string loadSceneName)
    {
        Debug.Log("진입하는 행성 : " + loadSceneName);
        SceneManager.LoadScene(loadSceneName);
    }

    public void planetExited(bool val)
    {
        Debug.Log("Exit : " + val);
    }
}
