using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PowerUpTile : Tile
{
    private IEnumerator Clear(float t)
    {
        Tile[] sameColourTiles = Board.Instance.FindTilesOfColour(Colour);
        Board.Instance.ClearTiles(sameColourTiles);
        yield return new WaitForSeconds(t);
        Destroy(gameObject);
    }

    public override Coroutine Clear()
    {
        return StartCoroutine(Clear(0.5f));
    }
}
