using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    private struct SpawnInfo
    {
        public int numToSpawn;
        public int startHeight;
    }
    private static Board instance;
    [SerializeField] private Swappable selected;
    private Grid grid;
    public Color[] colors = new Color[4];
    [SerializeField] private Vector2Int max = Vector2Int.zero;
    private Swappable[,] tiles;
    private SpawnInfo[] spawners;
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

        spawners = new SpawnInfo[Max.x + 1];
        for (int i = 0; i < spawners.Length; ++i)
        {
            spawners[i] = new SpawnInfo() { numToSpawn = 0, startHeight = -1 };
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
        List<Vector3Int[]> tileGroups = new List<Vector3Int[]>();

        for (int i = 0; i < toCheck.Length; ++i)
        {
            List<Swappable> hMatches = new List<Swappable>();
            List<Swappable> vMatches = new List<Swappable>();
            List<Vector3Int> hTiles = new List<Vector3Int>();
            List<Vector3Int> vTiles = new List<Vector3Int>();
            //Check Left
            if (toCheck[i].x > 0 && tiles[toCheck[i].x, toCheck[i].y].colour == tiles[toCheck[i].x - 1, toCheck[i].y].colour)
            {
                if (toCheck[i].x > 1 && tiles[toCheck[i].x, toCheck[i].y].colour == tiles[toCheck[i].x - 2, toCheck[i].y].colour)
                {
                    hMatches.Add(tiles[toCheck[i].x - 2, toCheck[i].y]);
                }
                hMatches.Add(tiles[toCheck[i].x - 1, toCheck[i].y]);
            }
            hMatches.Add(tiles[toCheck[i].x, toCheck[i].y]);
            //Check Right
            if (toCheck[i].x < Max.x && tiles[toCheck[i].x, toCheck[i].y].colour == tiles[toCheck[i].x + 1, toCheck[i].y].colour)
            {
                hMatches.Add(tiles[toCheck[i].x + 1, toCheck[i].y]);
                if (toCheck[i].x < Max.x - 1 && tiles[toCheck[i].x, toCheck[i].y].colour == tiles[toCheck[i].x + 2, toCheck[i].y].colour)
                {
                    hMatches.Add(tiles[toCheck[i].x + 2, toCheck[i].y]);
                }
            }
            //Check Down
            if (toCheck[i].y > 0 && tiles[toCheck[i].x, toCheck[i].y].colour == tiles[toCheck[i].x, toCheck[i].y - 1].colour)
            {
                if (toCheck[i].y > 1 && tiles[toCheck[i].x, toCheck[i].y].colour == tiles[toCheck[i].x, toCheck[i].y - 2].colour)
                {
                    vMatches.Add(tiles[toCheck[i].x, toCheck[i].y - 2]);
                }
                vMatches.Add(tiles[toCheck[i].x, toCheck[i].y - 1]);
            }
            vMatches.Add(tiles[toCheck[i].x, toCheck[i].y]);
            //Check Up
            if (toCheck[i].y < Max.y && tiles[toCheck[i].x, toCheck[i].y].colour == tiles[toCheck[i].x, toCheck[i].y + 1].colour)
            {
                vMatches.Add(tiles[toCheck[i].x, toCheck[i].y + 1]);
                if (toCheck[i].y < Max.y - 1 && tiles[toCheck[i].x, toCheck[i].y].colour == tiles[toCheck[i].x, toCheck[i].y + 2].colour)
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
                    hMatches[j].HighLight();
                    coroutines.Add(hMatches[j].Clear());
                    hTiles.Add(hMatches[j].pos);
                }
                tileGroups.Add(hTiles.ToArray());
            }
            if (vMatches.Count < 3)
            {
                vMatches.Clear();
            }
            else
            {
                for (int j = 0; j < vMatches.Count; ++j)
                {
                    if (hTiles.Contains(vMatches[j].pos))
                        continue;

                    vMatches[j].HighLight();
                    coroutines.Add(vMatches[j].Clear());
                    vTiles.Add(vMatches[j].pos);
                }
                tileGroups.Add(vTiles.ToArray());
            }
        }
        StartCoroutine(WaitForClear(tileGroups.ToArray(), coroutines.ToArray()));
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

    private IEnumerator WaitForClear(Vector3Int[][] tileGroups, Coroutine[] coroutines)
    {
        for (int i = 0; i < coroutines.Length; ++i)
        {
            yield return coroutines[i];
        }

        for (int i = 0; i < tileGroups.Length; ++i)
        {
            for (int j = 0; j < tileGroups[i].Length; ++j)
            {
                Vector2Int pos = (Vector2Int)tileGroups[i][j];
                if (spawners[pos.x].numToSpawn == 0 || spawners[pos.x].startHeight > pos.y)
                {
                    spawners[pos.x].startHeight = pos.y;
                }
                ++spawners[pos.x].numToSpawn;
            }
        }

        SpawnFromQueues();
    }

    private void SpawnFromQueues()
    {
        List<Vector3Int> movedTiles = new List<Vector3Int>();
        List<Coroutine> moveCoroutines = new List<Coroutine>();

        for (int x = 0; x < spawners.Length; ++x)
        {
            if (spawners[x].numToSpawn <= 0)
                continue;

            for (int y = spawners[x].startHeight; y <= Max.y; ++y)
            {
                movedTiles.Add(new Vector3Int(x, y, 0));

                int yOffset = y + spawners[x].numToSpawn;
                if (yOffset <= Max.y)
                {
                    moveCoroutines.Add(StartCoroutine(tiles[x, yOffset].MoveToCoroutine(new Vector3Int(x, y, 0))));
                    tiles[x, y] = tiles[x, yOffset];
                }
                else
                {
                    Swappable newTile = Instantiate(tilePrefab, transform).GetComponent<Swappable>();
                    newTile.transform.position = Grid.GetCellCenterWorld(new Vector3Int(x, yOffset, 0));
                    newTile.Setup();
                    moveCoroutines.Add(StartCoroutine(newTile.MoveToCoroutine(new Vector3Int(x, y, 0))));
                    tiles[x, y] = newTile;
                }
            }
            spawners[x].numToSpawn = 0;
            spawners[x].startHeight = -1;
        }

        //if(movedTiles.Count > 0)
        //    StartCoroutine(WaitForMove(movedTiles.ToArray(), moveCoroutines.ToArray()));
    }
}
