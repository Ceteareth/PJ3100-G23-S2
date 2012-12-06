using UnityEngine;
using System.Collections;

public class PauseMenu : MonoBehaviour
{

    bool isPaused = false;
    
    
    void Update()
    {
        
        if (Input.GetKeyDown("escape") && !isPaused)
        {
            
            Time.timeScale = 0.0f;
            isPaused = true;
            
        }
        else if (Input.GetKeyDown("escape") && isPaused)
         
        {
               
            Time.timeScale = 1.0f;
            isPaused = false;
        }
    }

    void OnGUI()
    {

        if (isPaused)
        {
            Screen.showCursor = true;
            GetComponent<MouseLook>().enabled = false;
            GameObject.Find("First Person Controller").GetComponent<MouseLook>().enabled = false;

           
            if (GUI.Button(new Rect (Screen.width/2, 120, 100, 56), "Quit"))
            {
                Application.Quit();
            }
            if (GUI.Button(new Rect (Screen.width/2, 220, 100, 56), "Restart"))
            {
                
                //Application.LoadLevel("test_project"); 
                Time.timeScale = 1.0f;
                isPaused = false;
            }
            if (GUI.Button(new Rect (Screen.width/2, 320, 100, 56), "Continue"))
            {
                Time.timeScale = 1.0f;
                isPaused = false;
            }
        }
        else if (!isPaused)
        {
            Screen.showCursor = false;
            GetComponent<MouseLook>().enabled = true;
            GameObject.Find("First Person Controller").GetComponent<MouseLook>().enabled = true;
        }

    }
}