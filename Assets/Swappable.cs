using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Swappable : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Color[] colors;
    public Vector3Int pos;
    private void Awake()
    {
        GetComponent<Image>().color = colors[transform.GetSiblingIndex() % colors.Length];
    }

    private void Start()
    {
        pos = Board.Instance.grid.WorldToCell(transform.position);
        transform.position = Board.Instance.grid.CellToWorld(pos);
    }

    public void OnPointerClick(PointerEventData e)
    {
        Board.Instance.SelectSwappable(this);
    }

    public void MoveTo(Vector3 newPos, int newIndex)
    {
        transform.SetAsLastSibling();
        StartCoroutine(MoveToCoroutine(newPos, newIndex));
    }

    private IEnumerator MoveToCoroutine(Vector3 newPos, int newIndex)
    {
        Vector3 origin = transform.position;
        Vector3 destination = newPos;
        float animTime = 0f;

        while (Vector3.Distance(transform.position, destination) > 5f)
        {
            animTime += Time.deltaTime;
            transform.position = Vector3.Lerp(origin, destination, animTime);

            yield return null;
        }
        transform.position = destination;
        pos = Board.Instance.grid.WorldToCell(transform.position);
        transform.SetSiblingIndex(newIndex);
    }
}
