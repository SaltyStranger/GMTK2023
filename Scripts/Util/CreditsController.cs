using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditsController : MonoBehaviour
{
    // Keybinds
    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Back
        controls.MouseKeyboard.pause.started += _ => { backToTitle(); };
        controls.MouseKeyboard.pause.canceled += _ => {};
    }

    private void backToTitle()
    {
        SceneTransferManager.Instance.loadScene("Scenes/TitleScreen");
    }
}
