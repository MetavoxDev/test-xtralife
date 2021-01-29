/* Copyright 2021, Allan Arnaudin, All rights reserved. */
using CotcSdk;
using System;
using UnityEngine;

//Layer to manipulate Leaderboard with Cloud
public class LeaderboardsHolder
{
    private const string NAME_GLOBAL_BOARD = "global_scores";
    private CloudManager master = null;

    public LeaderboardsHolder(CloudManager _master)
    {
        master = _master;
    }

    //SCORES
    public void PostScore(int _newScore, Action OnResult = null)
    {
        if (!master.Profile.IsLogged)
        {
            Debug.LogError("Not logged");
            return;
        }

        master.Profile.LoggedGamer.Scores.Domain("private").Post(_newScore, NAME_GLOBAL_BOARD, ScoreOrder.HighToLow,
        "", false)
        .Done(postScoreRes =>
        {
            OnResult?.Invoke();
        }, ex =>
        {
            CotcException error = (CotcException)ex;
            Debug.LogError("Could not post score: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }
    public void BestHighScores(int _numberLineDisplayed, Action<PagedList<Score>> OnResult)
    {
        if (!master.Profile.IsLogged)
        {
            Debug.LogError("Not logged");
            return;
        }

        master.Profile.LoggedGamer.Scores.Domain("private").BestHighScores(NAME_GLOBAL_BOARD, _numberLineDisplayed, 1)
        .Done(bestHighScoresRes =>
        {
            OnResult.Invoke(bestHighScoresRes);
        }, ex =>
        {
            // The exception should always be CotcException
            CotcException error = (CotcException)ex;
            Debug.LogError("Could not get best high scores: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }

    //USERS
    public void GetUser(string _id, Action<UserInfo> OnResult = null, Action OnFailed = null)
    {
        master.Cloud.ListUsers(_id, 1, 0)
        .Done(listUsersRes =>
        {
            if (listUsersRes.Count > 0)
                OnResult?.Invoke(listUsersRes[0]);
            else
                OnFailed?.Invoke();
        }, ex =>
        {
            OnFailed?.Invoke();

            CotcException error = (CotcException)ex;
            Debug.LogError("Failed to list users: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }
    public void GetUserWithTag(string _id, int _tag, Action<int, UserInfo> OnResult = null, Action OnFailed = null)
    {
        master.Cloud.ListUsers(_id, 1, 0)
        .Done(listUsersRes =>
        {
            if (listUsersRes.Count > 0)
                OnResult?.Invoke(_tag, listUsersRes[0]);
            else
                OnFailed?.Invoke();
        }, ex =>
        {
            OnFailed?.Invoke();

            CotcException error = (CotcException)ex;
            Debug.LogError("Failed to list users: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }
    public void GetListUsers(string _matchPattern, int _number, Action<PagedList<UserInfo>> OnResult = null, Action OnFailed = null, int _startIndex = 0)
    {
        master.Cloud.ListUsers(_matchPattern, _number, _startIndex)
        .Done(listUsersRes =>
        {
            OnResult?.Invoke(listUsersRes);
        }, ex =>
        {
            OnFailed?.Invoke();
            CotcException error = (CotcException)ex;
            Debug.LogError("Failed to list users: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }
}
