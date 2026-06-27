using UnityEngine;
using UnityEngine.SceneManagement;

public class ClearUIManager : MonoBehaviour
{
    public void RestartGame()
    {
        Time.timeScale = 1f; // 恢复时间
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // 重载当前场景
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); 
    }
}