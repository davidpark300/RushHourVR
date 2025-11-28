using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneLoad : MonoBehaviour
{
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
