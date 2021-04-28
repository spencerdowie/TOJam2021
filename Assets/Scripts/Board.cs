using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    private static Board instance;
    [SerializeField] private Swappable selected;
    private Grid grid;
    public Color[] colors = new Color[4];
    [SerializeField] private Vector2Int max = Vector2Int.zero;
    private Swappable[,] tiles;
    [SerializeField] private GameObject tilePrefab;

    public static Board Instance { get => instance; }
    public Grid Grid
    {
        get
        {
            if (!grid)
                grid = GetComponent<Grid>();
            return grid;
        }
    }

    public Vector2Int Max
    {
        get
        {
            if (max == Vector2Int.zero)
            {
                Rect rect = GetComponent<RectTransform>().rect;
                max = (Vector2Int)Grid.WorldToCell(new Vector3(rect.xMax, rect.yMax));
            }
            return max;
        }
    }

    private void Awake()
    {
        if (!instance)
        {
            instance = this;
            return;
        }
        Destroy(gameObject);
    }

    private void Start()
    {
        tiles = new Swappable[Max.x + 1, Max.y + 1];
        for (int i = 0; i < transform.childCount; ++i)
        {
            Swappable tile = transform.GetChild(i).GetComponent<Swappable>();
            tiles[tile.pos.x, tile.pos.y] = tile;
        }
    }

    public bool SelectSwappable(Swappable swappable)
    {
        if (!selected)
        {
            selected = swappable;
            selected.HighLight();
            return true;
        }
        else if (selected != swappable)
        {
            if (Swappable.IsAdjacent(selected.pos, swappable.pos))
            {
                Coroutine[] coroutines = new Coroutine[2];
                selected.HighLight(false);
                coroutines[0] = StartCoroutine(selected.MoveToCoroutine(swappable.pos));
                coroutines[1] = StartCoroutine(swappable.MoveToCoroutine(selected.pos));

                tiles[selected.pos.x, selected.pos.y] = swappable;
                tiles[swappable.pos.x, swappable.pos.y] = selected;

                StartCoroutine(WaitForMove(new Vector3Int[] { selected.pos, swappable.pos }, coroutines));

                selected = null;
                return true;
            }
        }
        return false;
    }

    public void CheckAdjacent(Vector3Int[] toCheck)
    {
        List<Coroutine> coroutines = new List<Coroutine>();
        List<Vector3Int> tileGroups = new List<Vector3Int>();

        for (int i = 0; i < toCheck.Length; ++i)
        {
            List<Swappable> hMatches = new List<Swappable>();
            List<Swappable> vMatches = new List<Swappable>();
            List<Vector3Int> hTiles = new List<Vector3Int>();
            List<Vector3Int> vTiles = new List<Vector3Int>();

            Swappable tile = tiles[toCheck[i].x, toCheck[i].y];
            //Check Left
            if (toCheck[i].x > 0 && tile.colour == tiles[toCheck[i].x - 1, toCheck[i].y].colour)
            {
                if (toCheck[i].x > 1 && tile.colour == tiles[toCheck[i].x - 2, toCheck[i].y].colour)
                {
                    hMatches.Add(tiles[toCheck[i].x - 2, toCheck[i].y]);
                }
                hMatches.Add(tiles[toCheck[i].x - 1, toCheck[i].y]);
            }
            hMatches.Add(tile);
            //Check Right
            if (toCheck[i].x < Max.x && tile.colour == tiles[toCheck[i].x + 1, toCheck[i].y].colour)
            {
                hMatches.Add(tiles[toCheck[i].x + 1, toCheck[i].y]);
                if (toCheck[i].x < Max.x - 1 && tile.colour == tiles[toCheck[i].x + 2, toCheck[i].y].colour)
                {
                    hMatches.Add(tiles[toCheck[i].x + 2, toCheck[i].y]);
                }
            }
            //Check Down
            if (toCheck[i].y > 0 && tile.colour == tiles[toCheck[i].x, toCheck[i].y - 1].colour)
            {
                if (toCheck[i].y > 1 && tile.colour == tiles[toCheck[i].x, toCheck[i].y - 2].colour)
                {
                    vMatches.Add(tiles[toCheck[i].x, toCheck[i].y - 2]);
                }
                vMatches.Add(tiles[toCheck[i].x, toCheck[i].y - 1]);
            }
            vMatches.Add(tile);
            //Check Up
            if (toCheck[i].y < Max.y && tile.colour == tiles[toCheck[i].x, toCheck[i].y + 1].colour)
            {
                vMatches.Add(tiles[toCheck[i].x, toCheck[i].y + 1]);
                if (toCheck[i].y < Max.y - 1 && tile.colour == tiles[toCheck[i].x, toCheck[i].y + 2].colour)
                {
                    vMatches.Add(tiles[toCheck[i].x, toCheck[i].y + 2]);
                }
            }
            if (hMatches.Count < 3)
            {
                hMatches.Clear();
            }
            else
            {
                for (int j = 0; j < hMatches.Count; ++j)
                {
                    if (tileGroups.Contains(hMatches[j].pos))
                        continue;

                    hMatches[j].HighLight();
                    coroutines.Add(hMatches[j].Clear());
                    hTiles.Add(hMatches[j].pos);
                }
                tileGroups.AddRange(hTiles.ToArray());
            }
            if (vMatches.Count < 3)
            {
                vMatches.Clear();
            }
            else
            {
                for (int j = 0; j < vMatches.Count; ++j)
                {
                    if (tileGroups.Contains(vMatches[j].pos))
                        continue;

                    vMatches[j].HighLight();
                    coroutines.Add(vMatches[j].Clear());
                    vTiles.Add(vMatches[j].pos);
                }
                tileGroups.AddRange(vTiles.ToArray());
            }
        }

        for(int i = 0; i < tileGroups.Count; ++i)
        {
            tiles[tileGroups[i].x, tileGroups[i].y] = null;
        }
        StartCoroutine(WaitForClear(coroutines.ToArray()));
    }

    public void AlignChildrenToGrid()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        int maxX = Grid.WorldToCell(new Vector3(rectTransform.rect.xMax, 0)).x;
        Vector3Int currentPos = new Vector3Int(0, 0, 0);
        for (int i = 0; i < transform.childCount; ++i)
        {
            Transform tile = transform.GetChild(i);
            int colour = tile.GetSiblingIndex() % colors.Length;
            tile.GetComponent<UnityEngine.UI.Image>().color = colors[colour];
            tile.GetComponent<Swappable>().pos = currentPos;
            tile.GetComponent<Swappable>().colour = colour;
            tile.position = Grid.GetCellCenterWorld(currentPos);
            ++currentPos.x;
            if (currentPos.x > maxX)
            {
                currentPos.x = 0;
                ++currentPos.y;
            }
        }
    }

    private IEnumerator WaitForMove(Vector3Int[] tiles, Coroutine[] coroutines)
    {
        for (int i = 0; i < coroutines.Length; ++i)
            yield return coroutines[i];
        CheckAdjacent(tiles);
    }

    private IEnumerator WaitForClear(Coroutine[] coroutines)
    {
        for (int i = 0; i < coroutines.Length; ++i)
        {
            yield return coroutines[i];
        }

        SpawnNewTiles();
    }

    private void SpawnNewTiles()
    {
        List<Coroutine> moveCoroutines = new List<Coroutine>();
        List<Vector3Int> movedTiles = new List<Vector3Int>();

        for (int x = 0; x <= Max.x; ++x)
        {
            int spawnHeight = Max.y + 1;
            for (int y = 0; y <= Max.y; ++y)
            {
                if (tiles[x, y] == null)
                {
                    for (int i = y; i <= Max.y; ++i)
                    {
                        if (tiles[x, i] != null)
                        {
                            moveCoroutines.Add(StartCoroutine(tiles[x, i].MoveToCoroutine(new Vector3Int(x, y, 0))));
                            tiles[x, y] = tiles[x, i];
                            tiles[x, i] = null;
                            break;
                        }
                    }
                    if (tiles[x, y] == null)//If there where no tiles on the board the above for loop will end without assigning a tile to x,y
                    {
                        moveCoroutines.Add(SpawnAndMove(x, y, spawnHeight++));
                    }
                    movedTiles.Add(new Vector3Int(x, y, 0));
                }
            }
        }

        if(movedTiles.Count > 0)
            StartCoroutine(WaitForMove(movedTiles.ToArray(), moveCoroutines.ToArray()));
    }

    private Coroutine SpawnAndMove(int x, int y, int spawnHeight)
    {
        Swappable newTile = Instantiate(tilePrefab, transform).GetComponent<Swappable>();
        newTile.transform.position = Grid.GetCellCenterWorld(new Vector3Int(x, spawnHeight, 0));
        newTile.Setup();
        tiles[x, y] = newTile;
        return StartCoroutine(newTile.MoveToCoroutine(new Vector3Int(x, y, 0)));
    }
}
