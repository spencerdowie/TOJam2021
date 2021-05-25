using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroButtonController : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 1f;
    public string sceneName;

    public void OnPlay()
    {
        StartCoroutine(LoadPartyPlannerScene());
    }

    IEnumerator LoadPartyPlannerScene()
    {
        // Play animation
        transition.SetTrigger("Start");

        // Wait till done
        yield return new WaitForSeconds(transitionTime);

        // Load scene
        SceneManager.LoadScene(sceneName);
    }
}
