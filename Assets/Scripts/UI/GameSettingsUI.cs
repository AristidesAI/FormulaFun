using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSettingsUI : MonoBehaviour
{
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] TMP_Dropdown qualityDropdown;
    [SerializeField] Button closeButton;

    const string MusicVolKey = "MusicVolume";
    const string SfxVolKey = "SfxVolume";
    const string QualityKey = "QualityLevel";

    void Start()
    {
        // Load saved settings
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = PlayerPrefs.GetFloat(MusicVolKey, 0.8f);
            musicVolumeSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(MusicVolKey, v));
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat(SfxVolKey, 1f);
            sfxVolumeSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(SfxVolKey, v));
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
            qualityDropdown.value = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt(QualityKey, index);
    }

    void OnDisable()
    {
        PlayerPrefs.Save();
    }
}
