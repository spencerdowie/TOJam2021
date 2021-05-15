using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ClearLinesTile : Tile
{
    private IEnumerator Clear(float t)
    {
        Tile[] row = Board.Instance.GetRow(pos.y);
        Tile[] column = Board.Instance.GetColumn(pos.x);
        Board.Instance.ClearTiles(row);
        Board.Instance.ClearTiles(column);
        yield return new WaitForSeconds(t);
        Destroy(gameObject);
    }

    public override Coroutine Clear()
    {
        return StartCoroutine(Clear(0.5f));
    }
}
