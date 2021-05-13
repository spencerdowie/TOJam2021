using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    private static Board instance;
    [SerializeField] private Tile selected;
    private Grid grid;
    public Color[] colors = new Color[4];
    [SerializeField] private Vector2Int max = Vector2Int.zero;
    private Tile[,] tiles;
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
        tiles = new Tile[Max.x + 1, Max.y + 1];
        for (int i = 0; i < transform.childCount; ++i)
        {
            Tile tile = transform.GetChild(i).GetComponent<Tile>();
            tile.Setup();
            tiles[tile.pos.x, tile.pos.y] = tile;
        }
        SetupBoard();
    }

    public void SetupBoard()
    {

    }

    public bool SelectSwappable(Tile swappable)
    {
        if (!selected)
        {
            selected = swappable;
            selected.HighLight();
            return true;
        }
        else if (selected != swappable)
        {
            if (Tile.IsAdjacent(selected.pos, swappable.pos))
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

    public Tile[] GetAdjacentMatches(Tile[] toCheck)
    {
        List<Tile> adjTiles = new List<Tile>();

        for (int i = 0; i < toCheck.Length; ++i)
        {
            List<Tile> hMatches = new List<Tile>();
            List<Tile> vMatches = new List<Tile>();

            Vector3Int pos = toCheck[i].pos;
            //Check Left
            if (pos.x > 0 && toCheck[i].Colour == tiles[pos.x - 1, pos.y].Colour)
            {
                if (pos.x > 1 && toCheck[i].Colour == tiles[pos.x - 2, pos.y].Colour)
                {
                    hMatches.Add(tiles[pos.x - 2, pos.y]);
                }
                hMatches.Add(tiles[pos.x - 1, pos.y]);
            }
            hMatches.Add(toCheck[i]);
            //Check Right
            if (pos.x < Max.x && toCheck[i].Colour == tiles[pos.x + 1, pos.y].Colour)
            {
                hMatches.Add(tiles[pos.x + 1, pos.y]);
                if (pos.x < Max.x - 1 && toCheck[i].Colour == tiles[pos.x + 2, pos.y].Colour)
                {
                    hMatches.Add(tiles[pos.x + 2, pos.y]);
                }
            }
            //Check Down
            if (pos.y > 0 && toCheck[i].Colour == tiles[pos.x, pos.y - 1].Colour)
            {
                if (pos.y > 1 && toCheck[i].Colour == tiles[pos.x, pos.y - 2].Colour)
                {
                    vMatches.Add(tiles[pos.x, pos.y - 2]);
                }
                vMatches.Add(tiles[pos.x, pos.y - 1]);
            }
            vMatches.Add(toCheck[i]);
            //Check Up
            if (pos.y < Max.y && toCheck[i].Colour == tiles[pos.x, pos.y + 1].Colour)
            {
                vMatches.Add(tiles[pos.x, pos.y + 1]);
                if (pos.y < Max.y - 1 && toCheck[i].Colour == tiles[pos.x, pos.y + 2].Colour)
                {
                    vMatches.Add(tiles[pos.x, pos.y + 2]);
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
                    if (adjTiles.Contains(hMatches[j]))
                        continue;

                    adjTiles.Add(hMatches[j]);
                }
            }
            if (vMatches.Count < 3)
            {
                vMatches.Clear();
            }
            else
            {
                for (int j = 0; j < vMatches.Count; ++j)
                {
                    if (adjTiles.Contains(vMatches[j]))
                        continue;

                    adjTiles.Add(vMatches[j]);
                }
            }
        }

        return adjTiles.ToArray();
    }

    public Coroutine[] ClearTiles(Tile[] toClear)
    {
        List<Coroutine> coroutines = new List<Coroutine>();
        for(int i = 0; i <  toClear.Length; ++i)
        {
            toClear[i].HighLight();
            coroutines.Add(toClear[i].Clear());
            Vector3Int pos = toClear[i].pos;
            tiles[pos.x, pos.y] = null;
        }
        return coroutines.ToArray();
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
            tile.GetComponent<Tile>().pos = currentPos;
            tile.GetComponent<Tile>().colour = colour;
            tile.position = Grid.GetCellCenterWorld(currentPos);
            ++currentPos.x;
            if (currentPos.x > maxX)
            {
                currentPos.x = 0;
                ++currentPos.y;
            }
        }
    }

    private IEnumerator WaitForMove(Tile[] tiles, Coroutine[] coroutines)
    {
        for (int i = 0; i < coroutines.Length; ++i)
            yield return coroutines[i];
        Tile[] adjTiles = GetAdjacentMatches(tiles);
        Coroutine[] clrCoroutines = ClearTiles(adjTiles);
        StartCoroutine(WaitForClear(clrCoroutines));
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
        List<Tile> movedTiles = new List<Tile>();

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
                    movedTiles.Add(tiles[x,y]);
                }
            }
        }

        if(movedTiles.Count > 0)
            StartCoroutine(WaitForMove(movedTiles.ToArray(), moveCoroutines.ToArray()));
    }

    private Coroutine SpawnAndMove(int x, int y, int spawnHeight)
    {
        Tile newTile = Instantiate(tilePrefab, transform).GetComponent<Tile>();
        newTile.transform.position = Grid.GetCellCenterWorld(new Vector3Int(x, spawnHeight, 0));
        newTile.Setup();
        tiles[x, y] = newTile;
        return StartCoroutine(newTile.MoveToCoroutine(new Vector3Int(x, y, 0)));
    }
}
