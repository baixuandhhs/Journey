using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("=== 菜单面板 ===")]
    public GameObject pauseMenu;
    public GameObject settingsPanel;

    [Header("=== 音量控制 ===")]
    public AudioMixer audioMixer;
    public Slider masterSlider, musicSlider, sfxSlider;

    private bool isPaused = false;

    void Start()
    {
        // 游戏启动时加载保存的音量
        ApplyVolume("MasterVol", masterSlider);
        ApplyVolume("MusicVol", musicSlider);
        ApplyVolume("SFXVol", sfxSlider);
    }

    private void ApplyVolume(string name, Slider slider)
    {
        float val = PlayerPrefs.GetFloat(name, 0.75f);
        if (slider != null) slider.value = val;
        audioMixer.SetFloat(name, Mathf.Log10(Mathf.Max(0.0001f, val)) * 20f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    // --- 菜单功能 ---
    public void Pause()
    {
        pauseMenu.SetActive(true);
        settingsPanel.SetActive(false);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void OpenSettings()
    {
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void BackToPause()
    {
        settingsPanel.SetActive(false);
        pauseMenu.SetActive(true);
    }

    public void QuitToMain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); // 确保主菜单场景在 Build Settings 中索引为 0
    }

    // --- 音量逻辑 (供 Slider 调用) ---
    public void SetMasterVol(float val) => SetVol("MasterVol", val);
    public void SetMusicVol(float val) => SetVol("MusicVol", val);
    public void SetSFXVol(float val) => SetVol("SFXVol", val);

    private void SetVol(string name, float value)
    {
        audioMixer.SetFloat(name, Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f);
        PlayerPrefs.SetFloat(name, value);
    }
}