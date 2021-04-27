using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Board))]
public class BoardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Board board = (Board)target;
        if(GUILayout.Button("Align Children To Grid"))
        {
            board.AlignChildrenToGrid();
        }
    }
}
