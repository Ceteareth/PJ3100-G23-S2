using UnityEngine;
using System.Collections;

public class FPCScript : MonoBehaviour
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

        }
        else if (!isPaused)
        {
            Screen.showCursor = false;
            GetComponent<MouseLook>().enabled = true;
        }

    }
}