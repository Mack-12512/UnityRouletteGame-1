using UnityEngine;
using UnityEngine.SceneManagement;

public class DifficultyManager : MonoBehaviour
{
    public void StartGame(int difficulty)
    {
        PlayerPrefs.SetInt("Difficulty", difficulty);
        SceneManager.LoadScene("Battle");
    }
}