using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButtonController : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 1f;

    public void OnPlay()
    {
        StartCoroutine(LoadIntroScene());
    }

    IEnumerator LoadIntroScene()
    {
        // Play animation
        transition.SetTrigger("Start");

        // Wait till done
        yield return new WaitForSeconds(transitionTime);

        // Load scene
        SceneManager.LoadScene("IntroScene");
    }

    public void OnSettings()
    {

    }

    public void OnCredits()
    {

    }

    public void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
