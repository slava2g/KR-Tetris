using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuControl : MonoBehaviour
{
    public void LoadGame()
    {
        SceneManager.LoadScene("Tetris");
    }

    public void Exit()
    {
        Application.Quit();
    }
}
