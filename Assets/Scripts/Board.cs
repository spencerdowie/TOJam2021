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

    public static Vector3Int DirectionIntoOffset(int dir)
    {
        Vector3Int vecDir = Vector3Int.zero;
        switch(dir)
        {
            case 0:
                vecDir.y += 1;
                break;
            case 1:
                vecDir.x += 1;
                break;
            case 2:
                vecDir.y -= 1;
                break;
            case 3:
                vecDir.x -= 1;
                break;
        }
        return vecDir;
    }

    public Vector3Int GetRandAdjacentVec3(Vector3Int pos)
    {
        List<int> directions = new List<int>(new int[]{ 0,1,2,3});
        if (pos.x == 0)
            directions.Remove(3);
        else if (pos.x == Max.x)
            directions.Remove(1);

        if (pos.y == 0)
            directions.Remove(2);
        else if (pos.y == Max.y)
            directions.Remove(0);

        return pos + DirectionIntoOffset(directions[Random.Range(0, directions.Count)]);
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
        Tile[] allTiles = new Tile[(max.x + 1) * (max.y + 1)];
        for (int i = 0; i < tiles.Length; ++i)
            allTiles[i] = tiles[i % (max.x + 1), i / (max.x + 1)];

        Tile[] matches = GetAdjacentMatches(allTiles);
        int count = 0, maxCount = 50;
        while (matches.Length > 0 && count < maxCount)
        {
            ++count;
            int matchLength = 1;
            for (int i = 0; i < matches.Length; ++i)
            {
                if (i + 1 < matches.Length && matches[i].Colour == matches[i + 1].Colour)
                {
                    ++matchLength;
                }
                else
                {
                    int randTile = i - Random.Range(0, matchLength);
                    Vector3Int adjVec3 = GetRandAdjacentVec3(matches[randTile].pos);
                    Tile adjTile = tiles[adjVec3.x, adjVec3.y];

                    //Debug.Log($"Swap {matches[randTile].pos} and {adjTile.pos}: {matches[randTile].Colour} and {adjTile.Colour}");

                    int swapColour = matches[randTile].Colour;
                    matches[randTile].Colour = adjTile.Colour;
                    adjTile.Colour = swapColour;

                    matchLength = 1;
                }
            }
            matches = GetAdjacentMatches(allTiles);
        }
        if(count == maxCount)
            Debug.LogError($"Over count");
    }

    public void SelectTile(Tile tile)
    {
        if (!selected)
        {
            selected = tile;
            selected.HighLight();
        }
        else if (selected != tile)
        {
            selected.HighLight(false);
            if (Tile.IsAdjacent(selected.pos, tile.pos))
            {
                Vector3Int posA = selected.pos;
                Vector3Int posB = tile.pos;

                tiles[posA.x, posA.y] = tile;
                tiles[posB.x, posB.y] = selected;

                Coroutine[] coroutines = new Coroutine[2];
                coroutines[0] = StartCoroutine(selected.MoveToCoroutine(posB));
                coroutines[1] = StartCoroutine(tile.MoveToCoroutine(posA));

                Tile[] adjTiles = GetAdjacentMatches(new Tile[] { selected, tile });
                
                if (adjTiles.Length > 0)
                {
                    StartCoroutine(WaitForCoroutines(coroutines, () =>
                    {
                        Coroutine[] clearCoroutines = ClearTiles(adjTiles);
                        StartCoroutine(WaitForCoroutines(clearCoroutines, FillEmptyTiles));
                    }));
                }
                else
                {
                    tiles[posA.x, posA.y] = selected;
                    tiles[posB.x, posB.y] = tile;

                    StartCoroutine(WaitForCoroutines(coroutines, () =>
                    {
                        StartCoroutine(tiles[posA.x, posA.y].MoveToCoroutine(posA));
                        StartCoroutine(tiles[posB.x, posB.y].MoveToCoroutine(posB));
                    }));
                    
                
                }
            }
            selected = null;
        }
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
            tile.GetComponent<Tile>().Colour = colour;
            tile.position = Grid.GetCellCenterWorld(currentPos);
            ++currentPos.x;
            if (currentPos.x > maxX)
            {
                currentPos.x = 0;
                ++currentPos.y;
            }
        }
    }

    private IEnumerator WaitForCoroutines(Coroutine[] coroutines, System.Action action = null)
    {
        for (int i = 0; i < coroutines.Length; ++i)
            yield return coroutines[i];
        action?.Invoke();
    }

    private void FillEmptyTiles()
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

        if (movedTiles.Count > 0)
        {
            Tile[] adjTiles = GetAdjacentMatches(movedTiles.ToArray());
            if(adjTiles.Length > 0)
            {
                StartCoroutine(WaitForCoroutines(moveCoroutines.ToArray(), () =>
                {
                    Coroutine[] clearCoroutines = ClearTiles(adjTiles);
                    StartCoroutine(WaitForCoroutines(clearCoroutines, FillEmptyTiles));
                }));
            }
        }
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
