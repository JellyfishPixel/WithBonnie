using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider ambientSlider;
    public Slider uiSlider;

    bool isOpen;

    void Start()
    {
        if (panel) panel.SetActive(false);

        var audio = AudioManager.Instance;

        masterSlider.SetValueWithoutNotify(audio.masterVolume);
        musicSlider.SetValueWithoutNotify(audio.musicVolume);
        sfxSlider.SetValueWithoutNotify(audio.sfxVolume);
        ambientSlider.SetValueWithoutNotify(audio.ambientVolume);
        uiSlider.SetValueWithoutNotify(audio.uiVolume);

       
        masterSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
        ambientSlider.onValueChanged.RemoveAllListeners();
        uiSlider.onValueChanged.RemoveAllListeners();

        masterSlider.onValueChanged.AddListener(audio.SetMaster);
        musicSlider.onValueChanged.AddListener(audio.SetMusic);
        sfxSlider.onValueChanged.AddListener(audio.SetSFX);
        ambientSlider.onValueChanged.AddListener(audio.SetAmbient);
        uiSlider.onValueChanged.AddListener(audio.SetUI);

    }


    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        panel.SetActive(true);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        panel.SetActive(false);
    }
}
