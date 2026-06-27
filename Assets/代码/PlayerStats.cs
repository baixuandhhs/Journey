using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    [Header("=== 基础属性 ===")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float maxMana = 100f;
    public float currentMana;

    [Header("=== HUD UI 引用 ===")]
    public Image healthFill;
    public Image manaFill;
    public TextMeshProUGUI hudRedText;
    public TextMeshProUGUI hudBlueText;

    [Header("=== 死亡面板 ===")]
    public GameObject gameOverPanel;

    [Header("=== 特效与设置 ===")]
    public float lerpSpeed = 10f;
    public ParticleSystem healParticlePrefab;
    public ParticleSystem manaParticlePrefab;
    public float iFrameDuration = 1.0f;
    private bool isInvincible = false;
    private SpriteRenderer sr;

    [Header("=== 音频 ===")]
    public AudioSource sfxSource;
    public AudioClip hurtSound;

    void Start()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
        sr = GetComponent<SpriteRenderer>();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        UpdateHealthUI();
    }

    void Update()
    {
        // 血条和蓝条的平滑过渡
        UpdateHealthBars();
    }

    /// <summary>平滑更新血条和蓝条的 fillAmount</summary>
    private void UpdateHealthBars()
    {
        if (healthFill) healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, currentHealth / maxHealth, Time.deltaTime * lerpSpeed);
        if (manaFill) manaFill.fillAmount = Mathf.Lerp(manaFill.fillAmount, currentMana / maxMana, Time.deltaTime * lerpSpeed);
    }

    /// <summary>强制立即刷新 HUD（外部调用）</summary>
    public void UpdateHealthUI()
    {
        if (healthFill) healthFill.fillAmount = currentHealth / maxHealth;
        if (manaFill) manaFill.fillAmount = currentMana / maxMana;
    }

    /// <summary>更新 HUD 快捷栏药水数量文本（由 InventoryManager 调用）</summary>
    public void UpdatePotionHUD(int redCount, int blueCount)
    {
        if (hudRedText) hudRedText.text = "x" + redCount;
        if (hudBlueText) hudBlueText.text = "x" + blueCount;
    }

    // ==================== 受伤与死亡 ====================

    public void TakeDamage(float amount)
    {
        if (isInvincible) return;
        if (sfxSource && hurtSound) sfxSource.PlayOneShot(hurtSound);

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateHealthUI();

        if (currentHealth > 0) StartCoroutine(BecomeInvincible());
        else Die();
    }

    void Die()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnClick_Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnClick_QuitToMain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    // ==================== 恢复逻辑 ====================

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthUI();
        if (healParticlePrefab) PlayEffect(healParticlePrefab);
    }

    public void RestoreMana(float amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        UpdateHealthUI();
        if (manaParticlePrefab) PlayEffect(manaParticlePrefab);
    }

    public bool ConsumeMana(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            UpdateHealthUI();
            return true;
        }
        return false;
    }

    // ==================== 无敌帧 ====================

    private IEnumerator BecomeInvincible()
    {
        isInvincible = true;
        for (int i = 0; i < 5; i++)
        {
            if (sr) sr.color = new Color(1, 1, 1, 0.2f);
            yield return new WaitForSeconds(0.1f);
            if (sr) sr.color = new Color(1, 1, 1, 1f);
            yield return new WaitForSeconds(0.1f);
        }
        isInvincible = false;
    }

    // ==================== 工具方法 ====================

    void PlayEffect(ParticleSystem prefab)
    {
        ParticleSystem p = Instantiate(prefab, transform.position, Quaternion.identity);
        Destroy(p.gameObject, 1f);
    }

    public bool GetInvincible() => isInvincible;
}
