using UnityEngine;
using System.Collections;

public class MainMenu : MonoBehaviour
{

    void Update()
    {

        if (Input.GetKeyDown("escape"))
        {
            Application.Quit();
        }
    }

    void OnGUI()
    {
            Screen.showCursor = true;

            if (GUI.Button(new Rect(Screen.width/2-150, Screen.height/2-100, 300, 100), "NEW GAME"))
            {
                Application.LoadLevel("first_level");
            }

            if (GUI.Button(new Rect(Screen.width/2-150, Screen.height/2+50, 300, 100), "QUIT"))
            {
                Application.Quit();
            }
    }
}