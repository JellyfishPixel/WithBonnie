using UnityEngine;

public class GameAudioInit : MonoBehaviour
{
    [Header("UI Sounds")]
    public AudioClip uiClick;


    [Header("SFX Sounds")]
    public AudioClip hit;
    public AudioClip pickup;

    void Start()
    {
        var audio = AudioManager.Instance;

        audio.RegisterSFX("ui_click", uiClick);
        audio.RegisterSFX("hit", hit);
        audio.RegisterSFX("pickup", pickup);
    }
}
