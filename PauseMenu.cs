using UnityEngine;
using System.Collections;

public class PauseMenu : MonoBehaviour
{

    bool isPaused = false;


    void Update()
    {

        if (Input.GetKeyDown("escape") && !isPaused)
        {
            GetComponent<MouseLook>().enabled = false;
            Screen.showCursor = true;
            Time.timeScale = 0.0f;
            isPaused = true;
        }
        else if (Input.GetKeyDown("escape") && isPaused)
        {
            GetComponent<MouseLook>().enabled = true;
            Screen.showCursor = false;
            Time.timeScale = 1.0f;
            isPaused = false;
        }
    }

    void OnGUI()
    {

        if (isPaused)
        {
           
            if (GUI.Button(new Rect (Screen.width/2, 120, 100, 56), "Quit"))
            {
                Application.Quit();
            }
            if (GUI.Button(new Rect (Screen.width/2, 220, 100, 56), "Restart"))
            {
                
               // Application.LoadLevel("SomeLevelHere");
                GetComponent<MouseLook>().enabled = true;
                Time.timeScale = 1.0f;
                isPaused = false;
            }
            if (GUI.Button(new Rect (Screen.width/2, 320, 100, 56), "Continue"))
            {
                GetComponent<MouseLook>().enabled = true;
                Screen.showCursor = false;
                Time.timeScale = 1.0f;
                isPaused = false;
            }
        }

    }
}