using UnityEngine;
using UnityEngine.SceneManagement;
public class LevelManager : MonoBehaviour
{
    // Reinicia la escena actual
    public void RestartLevel(float f)
    {
        // Obtiene el nombre de la escena actual
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }
}
