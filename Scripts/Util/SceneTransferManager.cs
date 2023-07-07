using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransferManager : CustomSingleton<SceneTransferManager>
{
    // (Optional) Prevent non-singleton constructor use.
    protected SceneTransferManager() { }

    // Set once this has *ever* been specified. Should NEVER become false after being set to true. Used internally for debugging to feel more natural.
    private bool setupManually = false;

    private float targetX, targetY = 0.0f;
    private int targetLayer = 0;
    private STMPacket transferStats = new STMPacket();

    public void loadScene(string destScene, float targetX, float targetY, int targetLayer, PlayerStats transferStats)
    {
        this.targetX = targetX;
        this.targetY = targetY;
        this.targetLayer = targetLayer;
        this.transferStats = transferStats.packet();
        this.setupManually = true;

        StartCoroutine(loadLevel(destScene));
    }

    public void loadScene(string destScene)
    {
        StartCoroutine(loadLevel(destScene));
    }

    private IEnumerator loadLevel(string destScene)
    {
        yield return new WaitForSeconds(0.3f);

        SceneManager.LoadScene(destScene);
    }

    public bool getIsSetup()
    {
        return setupManually;
    }

    public float[] getTargetCoords()
    {
        float[] f = { this.targetX, this.targetY };
        return f;
    }

    public int getTargetLayer()
    {
        return targetLayer;
    }

    public STMPacket getStats()
    {
        return transferStats;
    }
}
