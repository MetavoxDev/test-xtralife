/* Copyright 2021, Allan Arnaudin, All rights reserved. */
using CotcSdk;
using RedRunner.UI;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

//Used to control leaderboard with UILeaderboardLine(s)
public class UILeaderboard : MonoBehaviour
{
    #if UNITY_EDITOR
    public Transform ParentLinesEditor
    {
        get
        {
            return parentLines;
        }
    }
    public GameObject LinePrefabEditor
    {
        get
        {
            return linePrefab;
        }
    }
    public UILeaderboardLine[] LinesEditor
    {
        get
        {
            return lines;
        }
        set
        {
            lines = value;
        }
    }
#endif

    [Header("Refs")]
    [SerializeField] private UIWindow window = null;
    [SerializeField] private ScrollRect scrollRect = null;
    [Space()]
    [Header("Refs for lines")]
    [SerializeField] private Transform parentLines = null;
    [SerializeField] private GameObject linePrefab = null;
    [SerializeField] private UILeaderboardLine[] lines = null;

    private void Start()
    {
        LockScroll(false);
        CleanLeaderboard();

        window.OnPreOpen += OnPreOpenWindow;
        window.OnPreClose += OnPreCloseWindow;

        window.OnPostOpen += OnPostOpenWindow;
        window.OnPostClose += OnPostCloseWindow;
    }
    private void OnDestroy()
    {
        window.OnPreOpen -= OnPreOpenWindow;
        window.OnPreClose -= OnPreCloseWindow;

        window.OnPostOpen -= OnPostOpenWindow;
        window.OnPostClose -= OnPostCloseWindow;
    }

    private void OnPreOpenWindow()
    {
        CloudManager.Instance.Leaderboards.BestHighScores(100, OnDisplayLeaderboard);
    }
    private void OnPreCloseWindow()
    {
        LockScroll(false);
    }

    private void OnPostOpenWindow()
    {
        LockScroll(true);
    }
    private void OnPostCloseWindow()
    {
        CleanLeaderboard();
    }

    //Utilities
    private void CleanLeaderboard()
    {
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i].SetRank(i + 1);
            lines[i].SetUsername("-");
            lines[i].SetScore(-1);
        }
    }
    private void OnDisplayLeaderboard(PagedList<Score> _scores)
    {
        for (int i = 0; i < _scores.Count; i++)
        {
            lines[i].SetRank(_scores[i].Rank);
            lines[i].SetUsername(_scores[i].GamerInfo["profile"]["displayName"]);
            lines[i].SetScore((int)_scores[i].Value);

            //Update Username later async
            //CloudManager.Instance.Leaderboards.GetUserWithTag(_scores[i].GamerInfo.GamerId, i, RefreshUsernameAsync);
        }

        //int startIndexVoidLines = Mathf.Clamp(lines.Length - _scores.Count, 0, lines.Length);
        //for (int i = startIndexVoidLines-1; i < lines.Length; i++)
        //{

        //}
    }

    private void LockScroll(bool _interactable)
    {
        scrollRect.velocity = Vector2.zero;
        scrollRect.StopMovement();
        scrollRect.verticalScrollbar.value = 1;
        //scrollRect.enabled = _interactable;
    }

    //private void RefreshUsernameAsync(int _index, UserInfo _userInfos)
    //{
    //    if (_index >= 0 && _index < lines.Length)
    //    {
    //        lines[_index].SetUsername(_userInfos["profile"]["displayName"]);
    //    }
    //}
}

//Small editor script to be faster (We could upgrade this, but for the test it's useless)
#if UNITY_EDITOR
[CustomEditor(typeof(UILeaderboard))]
public class UILeaderboardEditor : Editor
{
    private UILeaderboard castedTarget = null;
    private void OnEnable()
    {
        castedTarget = (UILeaderboard)target;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        LoadLinesUI();
    }

    private void LoadLinesUI()
    {
        EditorGUI.BeginDisabledGroup((castedTarget.ParentLinesEditor == null));

        if (GUILayout.Button(new GUIContent("Load Lines References")))
        {
            //Undo.IncrementCurrentGroup();
            List<UILeaderboardLine> refsLines = new List<UILeaderboardLine>();
            for (int i = 0; i < castedTarget.ParentLinesEditor.transform.childCount; i++)
            {
                UILeaderboardLine currentLine = castedTarget.ParentLinesEditor.transform.GetChild(i).GetComponent<UILeaderboardLine>();
                if (currentLine)
                {
                    //Undo.RecordObject(currentLine, "Set Rank");

                    //currentLine.SetRank(i + 1);

                    //if(PrefabUtility.IsPartOfPrefabInstance(currentLine.gameObject))
                    //    PrefabUtility.RecordPrefabInstancePropertyModifications(currentLine);

                    //EditorUtility.SetDirty(currentLine);
                    refsLines.Add(currentLine);
                }
            }
            //Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            castedTarget.LinesEditor = refsLines.ToArray();
            EditorUtility.SetDirty(castedTarget);
        }

        EditorGUI.EndDisabledGroup();
    }
}
#endif
