using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelector : MonoBehaviour
{
    public string SceneName;
    public void OpenScene()
    {
        SceneManager.LoadScene(SceneName);
    }



}
