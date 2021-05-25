using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    public Vector3Int pos = new Vector3Int();
    private int colour;
    private GameObject highlight;
    [SerializeField] private AnimationCurve swapCurve, fallInCurve;

    public int Colour
    {
        get => colour;
        set
        {
            colour = value;
            GetComponent<Image>().overrideSprite = Board.Instance.Sprites[colour];
        }
    }

    public void Setup(bool rand = true)
    {
        pos = Board.Instance.Grid.WorldToCell(transform.position);
        GetComponent<RectTransform>().sizeDelta = Board.Instance.Grid.cellSize;
        Colour = rand ? Random.Range(0, Board.Instance.colors.Length) : transform.GetSiblingIndex() % 4;
        highlight = transform.GetChild(0).gameObject;
        name = $"Tile[{pos.x},{pos.y}]";
    }

    public void OnPointerClick(PointerEventData e)
    {
        Board.Instance.SelectTile(this);
    }

    public void HighLight(bool enabled = true)
    {
        highlight.SetActive(enabled);
    }

    public IEnumerator MoveToCoroutine(Vector3Int newPos)
    {
        Vector3 origin = Board.Instance.Grid.GetCellCenterWorld(pos);
        Vector3 destination = Board.Instance.Grid.GetCellCenterWorld(newPos);
        pos = newPos;
        name = $"Tile[{pos.x},{pos.y}]";
        float animTime = 0f;
        float curveEnd = swapCurve.keys[swapCurve.length-1].time;
        while (animTime < curveEnd)
        {
            animTime += Time.deltaTime;
            transform.position = Vector3.Lerp(origin, destination, swapCurve.Evaluate(animTime));

            yield return null;
        }
        transform.position = destination;
    }

    public IEnumerator FallInCoroutine(Vector3Int newPos)
    {
        yield return new WaitForSeconds(0.3f);
        Vector3 origin = Board.Instance.Grid.GetCellCenterWorld(pos);
        Vector3 destination = Board.Instance.Grid.GetCellCenterWorld(newPos);
        pos = newPos;
        name = $"Tile[{pos.x},{pos.y}]";
        float animTime = 0f;
        float curveEnd = fallInCurve.keys[fallInCurve.length - 1].time;
        while (animTime < curveEnd)
        {
            animTime += Time.deltaTime;
            transform.position = Vector3.Lerp(origin, destination, fallInCurve.Evaluate(animTime));

            yield return null;
        }
        transform.position = destination;
    }

    private IEnumerator Clear(float t)
    {
        yield return new WaitForSeconds(t);
        Destroy(gameObject);
    }

    public virtual Coroutine Clear()
    {
        return StartCoroutine(Clear(0.5f));
    }

    public static bool IsAdjacent(Vector3Int posA, Vector3Int posB)
    {
        int posAVal = posA.x + posA.y;
        int posBVal = posB.x + posB.y;
        int posABDiff = posAVal - posBVal;

        return Mathf.Abs(posABDiff) == 1;
    }

    //public static List<Vector3Int> GetAdjacent(Vector3Int pos)
    //{
    //    List<Vector3Int> tiles = new List<Vector3Int>();
    //
    //
    //
    //    return tiles;
    //}

    //public static Vector3Int GetAdjacent(Vector3Int pos, int direction)
    //{
    //    Vector3Int adjacent = pos;
    //    switch(direction)
    //    {
    //        case 0:
    //            adjacent.y += 1;
    //            break;
    //        case 1:
    //            adjacent.x += 1;
    //            break;
    //        case 2:
    //            adjacent.y -= 1;
    //            break;
    //        case 3:
    //            adjacent.x -= 1;
    //            break;
    //        default:
    //            Debug.LogError("Not a valid direction");
    //            break;
    //    }
    //
    //    return adjacent;
    //}
}
