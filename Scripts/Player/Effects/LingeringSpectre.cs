using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LingeringSpectre : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer sr;
    float alpha = 1.0f;
    float fadePerFrame = 0.0f;

    // Holds the amount the (R, G, B) values should be adjusted each frame.
    Vector3 colorChangePerFrame = new Vector3 (0.0f, 0.0f, 0.0f);


    private void FixedUpdate()
    {
        if(alpha <= 0.0f)
        {
            Destroy(gameObject);
        }
        else
        {
            alpha -= fadePerFrame;
        }
        // Color update per frame
        sr.color = new Color(sr.color.r + colorChangePerFrame.x, 
                             sr.color.g + colorChangePerFrame.y, 
                             sr.color.b + colorChangePerFrame.z, 
                             alpha);
    }

    public void setSprite(Sprite sprite)
    {
        sr.sprite = sprite;
    }

    public void setFadeVars(float initialAlpha, int fadeTime)
    {
        alpha = initialAlpha;
        fadePerFrame = initialAlpha / ((float)fadeTime);
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
    }

    public void setColorVars(Color startColor, Color endColor, float fadeTime)
    {
        colorChangePerFrame.x = (endColor.r - startColor.r) / fadeTime;
        colorChangePerFrame.y = (endColor.g - startColor.g) / fadeTime;
        colorChangePerFrame.z = (endColor.b - startColor.b) / fadeTime;
        sr.color = new Color(startColor.r, startColor.g, startColor.b);
    }

    //                                         \\
    // --------------------------------------- \\
    // Global Helper Methods for instantiation \\
    // --------------------------------------- \\
    //                                         \\

    // Create a Spectre parameterized where only the position matters
    public static void createSpectre(float initialAlpha, int fadeTime, GameObject spectrePrefab, Sprite sprite, Vector3 position)
    {
        createSpectre(initialAlpha, fadeTime, new Color(1.0f, 1.0f, 1.0f), new Color(1.0f, 1.0f, 1.0f), spectrePrefab, sprite, position, Quaternion.identity, new Vector3(1.0f, 1.0f, 1.0f));
    }

    // Create a Spectre with position, rotation, and scale variables.
    public static void createSpectre(float initialAlpha, int fadeTime, GameObject spectrePrefab, Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        createSpectre(initialAlpha, fadeTime, new Color(1.0f, 1.0f, 1.0f), new Color(1.0f, 1.0f, 1.0f), spectrePrefab, sprite, position, rotation, scale);
    }

    // Create a Spectre with a custom color set for the sprite. Note that color ignores alpha.
    public static void createSpectre(float initialAlpha, int fadeTime, Color color, GameObject spectrePrefab, Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        createSpectre(initialAlpha, fadeTime, color, color, spectrePrefab, sprite, position, Quaternion.identity, new Vector3(1.0f, 1.0f, 1.0f));
    }

    // Create a Spectre with a color change throughout its lifetime. Note that color ignores alpha.
    public static void createSpectre(float initialAlpha, int fadeTime, Color startColor, Color endColor, GameObject spectrePrefab, Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject spectre = Instantiate(spectrePrefab);
        spectre.transform.position = position;
        spectre.transform.rotation = rotation;
        spectre.transform.localScale = scale;

        LingeringSpectre ls = spectre.GetComponent<LingeringSpectre>();
        ls.setSprite(sprite);
        ls.setFadeVars(initialAlpha, fadeTime);
        ls.setColorVars(startColor, endColor, fadeTime);
    }
}
