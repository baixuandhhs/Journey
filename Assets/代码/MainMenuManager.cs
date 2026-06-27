using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("=== Panels ===")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("=== Audio ===")]
    public AudioMixer audioMixer;
    public Slider masterSlider, musicSlider, sfxSlider;

    [Header("=== Resolution ===")]
    public TMP_Dropdown resDropdown;
    Resolution[] resolutions;

    void Start()
    {
        // ���ز���ʼ������
        LoadVolume("MasterVol", masterSlider);
        LoadVolume("MusicVol", musicSlider);
        LoadVolume("SFXVol", sfxSlider);

        // ��ʼ���ֱ���
        InitResolution();
    }

    private void LoadVolume(string name, Slider slider)
    {
        // �� PlayerPrefs ��ȡ��Ĭ��ֵΪ 0.75
        float val = PlayerPrefs.GetFloat(name, 0.75f);
        slider.value = val;
        // Ӧ�õ� Mixer
        audioMixer.SetFloat(name, Mathf.Log10(Mathf.Max(0.0001f, val)) * 20f);
    }

    public void StartGame()
    {
        SaveManager.DeleteSave();
        SceneManager.LoadScene(1);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenSettings() { mainPanel.SetActive(false); settingsPanel.SetActive(true); }
    public void CloseSettings() { mainPanel.SetActive(true); settingsPanel.SetActive(false); }

    // --- �������� ---
    public void SetMasterVol(float value) { SetVol("MasterVol", value); }
    public void SetMusicVol(float value) { SetVol("MusicVol", value); }
    public void SetSFXVol(float value) { SetVol("SFXVol", value); }

    private void SetVol(string name, float value)
    {
        float db = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f;
        audioMixer.SetFloat(name, db);
        PlayerPrefs.SetFloat(name, value);
    }

    // --- �ֱ��ʿ��� ---
    void InitResolution()
    {
        resolutions = Screen.resolutions;
        resDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentResIndex = i;
        }
        resDropdown.AddOptions(options);
        resDropdown.value = currentResIndex;
        resDropdown.RefreshShownValue();
    }

    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }
}
