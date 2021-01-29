/* Copyright 2021, Allan Arnaudin, All rights reserved. */
using UnityEngine;
using TMPro;

//Manipulate infos on a line in the Leaderboard
public class UILeaderboardLine : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rank = null;
    [SerializeField] private TextMeshProUGUI username = null;
    [SerializeField] private TextMeshProUGUI score = null;

    public void SetRank(int _rank)
    {
        rank.text = "#" + _rank.ToString();
    }
    public void SetUsername(string _username)
    {
        username.text = _username;
    }
    public void SetScore(int _score)
    {
        if(_score == -1)
            score.text = "-";
        else
            score.text = _score.ToString() + "m";
    }
}
