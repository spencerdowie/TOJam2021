using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private AnimationCurve menuSpeed;
    [SerializeField] private Vector3 menuUp, menuDown, destination, origin;

    private void Awake()
    {
        menuUp = transform.position;
        menuDown = menuUp;
        menuDown.y -= GetComponent<RectTransform>().rect.height;
        MoveMenu(menuDown, true);
        GameSignals.PauseGame.AddListener(PauseGame);
    }

    public void PauseGame(bool pause)
    {
        MoveMenu(pause ? menuUp : menuDown);
    }

    public void MoveMenu(Vector2 position, bool instant = false)
    {
        if (instant)
        {
            menuPanel.position = position;
            return;
        }

        destination = position;
        origin = menuPanel.position;
        StartCoroutine(AnimateMenu());
    }

    public IEnumerator AnimateMenu()
    {
        bool atDest = false;
        float animTime = 0f;

        while(!atDest)
        {
            animTime += Time.deltaTime;
            float eval = menuSpeed.Evaluate(animTime);

            Vector3 newPosition = Vector2.Lerp(origin, destination, eval);
            menuPanel.position = newPosition;

            float remainingDistance = Vector2.Distance(newPosition, destination);

            if(remainingDistance < 1f)
            {
                menuPanel.position = destination;
                atDest = true;
            }
            yield return null;
        }
    }
}
