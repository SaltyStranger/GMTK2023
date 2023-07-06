using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// UTILITY CLASS for functions related to inputs from the player not tied to the base movement controller.
public class PlayerBehavior : MonoBehaviour
{
    public GameObject pauseMenu;

    public void pauseGame()
    {
        GamestateHandler.Instance.setIsGamePaused(true);
        //pauseMenu.SetActive(true);
        Time.timeScale = 0;
    }

    public void resumeGame()
    {
        GamestateHandler.Instance.setIsGamePaused(false);
        //pauseMenu.SetActive(false);
        Time.timeScale = 1;
    }
}
