using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    private static Board instance;
    [SerializeField]private Swappable selected;
    public Grid grid;

    public static Board Instance { get => instance;}

    private void Awake()
    {
        if(!instance)
        {
            instance = this;
            grid = GetComponent<Grid>();
            return;
        }
        Destroy(gameObject);
    }

    public bool SelectSwappable(Swappable swappable)
    {
        if(!selected)
        {
            selected = swappable;
            return true;
        }
        else if(selected != swappable)
        {
            int index = swappable.transform.GetSiblingIndex();
            swappable.MoveTo(selected.transform.position, selected.transform.GetSiblingIndex());
            selected.MoveTo(swappable.transform.position, index);
            selected = null;
            return true;
        }
        return false;
    }
}
