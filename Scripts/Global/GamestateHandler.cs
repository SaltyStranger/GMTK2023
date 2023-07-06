using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamestateHandler : CustomSingleton<GamestateHandler>
{
    private bool isGamePaused = false;
    protected GamestateHandler() { }

    // Get the Paused State of the game. Should be accessed from anywhere applicable.
    public bool getIsGamePaused() { return isGamePaused; }

    // Set the pause-state of the game. This is totally dirty global-variable-ing, but this should *only* be accessed from the Pause Menu controller. 
    // Violators of this will be prosecuted and subjected to reading no more than two chapers of Leo Tolstoy's "War and Peace". I'm not joking.
    public void setIsGamePaused(bool val) { isGamePaused = val; }
}
