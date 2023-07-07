using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleButtonFunctions : MonoBehaviour
{
    [SerializeField]
    private PlayerStats stats;

/*    public void newGame()
    {
        SceneTransferManager.Instance.loadScene("Scenes/Game/Level1", 0, -14, 0, stats);
    }*/

    public void play()
    {
        SceneTransferManager.Instance.loadScene("Scenes/Dev/MMTAssetTesting");
    }

    public void credits()
    {
        SceneTransferManager.Instance.loadScene("Scenes/Credits");
    }

    public void quit()
    {
        Debug.Log("Exited the game (this only works in the distributed version. Trust me, it's implemented.)");
        Application.Quit();
    }
}