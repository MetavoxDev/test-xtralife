/* Copyright 2021, Allan Arnaudin, All rights reserved. */
using UnityEngine;
using TMPro;

//Give the possibility to the Player to change his username
public class UIChangeUsername : MonoBehaviour 
{
    [SerializeField] private TMP_InputField inputField = null;

    private void Start()
    {
        CloudManager.Instance.Profile.OnLogged += TryRefreshField;
    }
    private void OnDestroy()
    {
        CloudManager.Instance.Profile.OnLogged -= TryRefreshField;
    }
    private void OnEnable()
    {
        inputField.onEndEdit.AddListener(OnEndEditUsername);
    }
    private void OnDisable()
    {
        inputField.onEndEdit.RemoveListener(OnEndEditUsername);
        CloudManager.Instance.Profile.OnLogged -= TryRefreshField;
    }

    private void OnEndEditUsername(string _result)
    {
        CloudManager.Instance.Profile.ChangeUsername(_result, TryRefreshField);
    }

    private void TryRefreshField()
    {
        CloudManager.Instance.Profile.GetUsername(RefreshField);
    }
    private void RefreshField(string _refreshedUsername)
    {
        inputField.text = _refreshedUsername;
    }
}
