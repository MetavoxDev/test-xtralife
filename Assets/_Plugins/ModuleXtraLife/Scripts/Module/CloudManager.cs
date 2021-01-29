/* Copyright 2021, Allan Arnaudin, All rights reserved. */
using CotcSdk;
using UnityEngine;

//Layer to manipulate Cloud
public class CloudManager : MonoBehaviour 
{
    //Usually I use my kit which includes Singleton/Persistent Singleton for a simple inheritance
    #region Singleton
    private static CloudManager instance;

    public static CloudManager Instance
    {
        get { return instance; }
    }
    public static bool HasInstance
    {
        get { return (instance); }
    }
    private void Awake()
    {
        if (instance)
        {
            Debug.LogError("Already a Singleton ! " + typeof(CloudManager).ToString() + " in " + this.gameObject.name + " and i am in " + instance.gameObject.name);
            Destroy(this.gameObject);
            return;
        }

        instance = (CloudManager)this;
        profile = new ProfileHolder(this);
        leaderboards = new LeaderboardsHolder(this);
    }
    #endregion

    [SerializeField] private CotcGameObject cotcGO = null;

    private Cloud cloud = null;
    public Cloud Cloud
    {
        get
        {
            return cloud;
        }
    }

    private ProfileHolder profile = null;
    public ProfileHolder Profile
    {
        get
        {
            return profile;
        }
    }

    private LeaderboardsHolder leaderboards = null;
    public LeaderboardsHolder Leaderboards
    {
        get
        {
            return leaderboards;
        }
    }

    private void Start()
    {
        cotcGO.GetCloud()
        .Catch(retry => 
        {
            Debug.LogError("Can't connect to the cloud");
        })
        .Done(_cloud =>
        {
            cloud = _cloud;
            Profile.StartLogin();
            SetupHttpHandler();
        });
    }


    //Utilities
    private void SetupHttpHandler()
    {
        int[] RetryTimes = { 100 /* ms */, 5000 /* ms */};
        cloud.HttpRequestFailedHandler = (HttpRequestFailedEventArgs e) =>
        {
            // Store retry count in UserData field (persisted among retries of a given request)
            int retryCount = e.UserData != null ? (int)e.UserData : 0;
            e.UserData = retryCount + 1;
            if (retryCount >= RetryTimes.Length)
                e.Abort();
            else
                e.RetryIn(RetryTimes[retryCount]);
        };
    }
}
