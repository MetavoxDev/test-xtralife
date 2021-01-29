/* Copyright 2021, Allan Arnaudin, All rights reserved. */
using CotcSdk;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

//Layer to manipulate Profile & Account with Cloud
//To enhance the profile, we could add all networks in more for one email. CUrrently it manages only the email.
public class ProfileHolder 
{
    public const string USERNAME_DEFAULT_PREFIX = "Guest_";

    public const string KEY_NETWORK = "GamerNetwork";
    public const string KEY_ID = "GamerId";
    public const string KEY_PWD = "GamerSecret";

    private CloudManager master = null;
    private Gamer loggedGamer = null;
    public Gamer LoggedGamer
    {
        get
        {
            return loggedGamer;
        }
    }

    public bool IsLogged
    {
        get { return (loggedGamer != null); }
    }
    public bool IsRegisteredEmail
    {
        get { return (PlayerPrefs.GetInt(KEY_NETWORK, 0) == 1); }
    }

    public UnityAction OnLogged;

    public ProfileHolder(CloudManager _master)
    {
        master = _master;
    }

    //----------------------------------------ACCOUNT (We send OnFailed too to keep the possibility to identify an error on the user-side. We could upgrade this part)
    public void StartLogin()
    {
        if (!PlayerPrefs.HasKey(KEY_ID) || !PlayerPrefs.HasKey(KEY_PWD) || !ProfileHolder.IsEmail(PlayerPrefs.GetString(KEY_ID)) || !IsRegisteredEmail)
        {
            LoginAnonymously();
        }
        else
        {
            SignIn(PlayerPrefs.GetString(KEY_ID), PlayerPrefs.GetString(KEY_PWD));
        }
    }
    public void LoginAnonymously(bool _launchLogEvent = true, Action<string, string> OnResult = null, Action<string, string> OnFailed = null)
    {
        master.Cloud.LoginAnonymously()
        .Catch(ex =>
        {
            Debug.LogError("Login failed: " + ex.ToString());
        })
        .Done(gamer =>
        {
            PlayerPrefs.SetString(KEY_ID, gamer.GamerId);
            PlayerPrefs.SetString(KEY_PWD, gamer.GamerSecret);
            PlayerPrefs.SetInt(KEY_NETWORK, 0);
            PlayerPrefs.Save();

            loggedGamer = gamer;

            if(_launchLogEvent)
                ChangeUsername("Unknown_" + gamer.GamerId.ToString(), OnLoggedAnonymously);
            else
                ChangeUsername("Unknown_" + gamer.GamerId.ToString());
        });
    }
    private void OnLoggedAnonymously()
    {
        OnLogged?.Invoke();
    }
    
    public void SignIn(string _id, string _pwd, Action<string, string> OnResult = null, Action<string, string> OnFailed = null)
    {
        master.Cloud.ResumeSession(
            gamerId: _id,
            gamerSecret: _pwd)
        .Done(gamer =>
        {
            if(IsEmail(_id))
                PlayerPrefs.SetInt(KEY_NETWORK, 1);
            else
                PlayerPrefs.SetInt(KEY_NETWORK, 0);

            PlayerPrefs.SetString(KEY_ID, _id);
            PlayerPrefs.SetString(KEY_PWD, _pwd);

            PlayerPrefs.Save();

            loggedGamer = gamer;

            OnResult?.Invoke(_id, _pwd);
            OnLogged?.Invoke();
        }, ex => 
        {
            Debug.LogError("Login failed: " + ex.ToString());
            LoginAnonymously();
            OnFailed?.Invoke(_id, _pwd);

            CotcException error = (CotcException)ex;
            Debug.LogError("Failed to login: " + error.ErrorCode + " (" + error.HttpStatusCode + ")");
        });
    }
    public void SignInOrCreate(string _mail, string _pwd, Action<string, string> OnResult = null, Action<string, string> OnFailed = null)
    {
        master.Cloud.Login(
            network: LoginNetwork.Email.Describe(),
            networkId: _mail,
            networkSecret: _pwd)
        .Catch(ex =>
        {
            Debug.LogError("Login failed: " + ex.ToString());
            LoginAnonymously();
            OnFailed?.Invoke(_mail, _pwd);
        })
        .Done(gamer =>
        {
            PlayerPrefs.SetInt(KEY_NETWORK, 1);
            PlayerPrefs.Save();

            loggedGamer = gamer;

            OnResult?.Invoke(_mail, _pwd);
            OnLogged?.Invoke();
        });
    }

    //MAIL FEATURES
    //public void SignUp(string _mail, string _password, Action<string, string> OnResult = null, Action OnFailed = null)
    //{
    //    CheckAndSignUp(_mail, _password, OnResult, OnFailed);
    //}
    //private void CheckAndSignUp(string _mail, string _password, Action<string, string> OnResult = null, Action OnFailed = null)
    //{
    //    master.Cloud.UserExists("email", _mail)
    //    .Done(userExistsRes =>
    //    {
    //        OnFailed?.Invoke();

    //    }, ex =>
    //    {
    //        RegisterMail(_mail, _password, OnResult); //It doesn't exists so we register
    //        OnResult?.Invoke(_mail, _password);

    //        //CotcException error = (CotcException)ex;
    //        //Debug.LogError("Failed to check user: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
    //    });
    //}
    public void SignUp(string _mail, string _password, Action<string, string> OnResult = null, Action OnFailed = null)
    {
        if (!IsLogged) 
        {
            Debug.LogError("Not logged");
            return;
        }

        loggedGamer.Account.Convert(LoginNetwork.Email.Describe(), _mail, _password)
        .Done(convertRes =>
        {
            PlayerPrefs.SetString(KEY_ID, _mail);
            PlayerPrefs.SetString(KEY_PWD, _password);
            PlayerPrefs.SetInt(KEY_NETWORK, 1);
            PlayerPrefs.Save();
            OnResult?.Invoke(_mail, _password);
        }, ex =>
        {
            OnFailed?.Invoke();

            CotcException error = (CotcException)ex;
            Debug.LogError("Failed to convert: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }
    
    public void CheckExistingMail(string _mail, Action<bool> OnResult = null, Action OnFailed = null)
    {
        master.Cloud.UserExists("email", _mail)
        .Done(userExistsRes =>
        {
            OnResult?.Invoke(userExistsRes.Successful);
        }, ex =>
        {
            OnFailed?.Invoke();

            CotcException error = (CotcException)ex;
            Debug.LogError("Failed to check user: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }

    public void ChangeEmail(string _newMail, Action<string> OnResult = null, Action OnFailed = null)
    {
        if (!IsLogged)
        {
            Debug.LogError("Not logged");
            return;
        }

        if (!IsRegisteredEmail)
        {
            Debug.LogError("Not registered email");
            return;
        }

        loggedGamer.Account.ChangeEmailAddress(_newMail)
        .Done(changeEmailRes =>
        {
            PlayerPrefs.SetString(KEY_ID, _newMail);
            PlayerPrefs.Save();
            OnResult?.Invoke(_newMail);
        }, ex =>
        {
            OnFailed?.Invoke();

            CotcException error = (CotcException)ex;
            Debug.LogError("Failed to change e-mail: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }

    //PWD
    public void ChangePassword(string _newPwd, Action<string> OnResult = null, Action OnFailed = null)
    {
        if (!IsLogged)
        {
            Debug.LogError("Not logged");
            return;
        }

        loggedGamer.Account.ChangePassword(_newPwd)
        .Done(changePasswordRes =>
        {
            PlayerPrefs.SetString(KEY_PWD, _newPwd);
            PlayerPrefs.Save();
            OnResult?.Invoke(_newPwd);
        }, ex =>
        {
            OnFailed?.Invoke();

            CotcException error = (CotcException)ex;
            Debug.LogError("Failed to change password: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }
    public void ResetPassword(string _mail, Action OnResult = null, Action OnFailed = null)
    {
        //Email infos
        string from = "support@xtralife.com";
        string title = "Reset your password";
        string body = "You can login with this shortcode: [[SHORTCODE]]";

        master.Cloud.SendResetPasswordEmail(_mail, from, title, body)
        .Done(resetPasswordRes =>
        {
            OnResult?.Invoke();
        }, ex =>
        {
            OnFailed?.Invoke();

            CotcException error = (CotcException)ex;
            Debug.LogError("Short code sending failed due to error: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }

    //----------------------------------------PROFILE    //displayName, lang, avatar, email, firstName, lastName, addr1, addr2, addr3
    public void ChangeUsername(string _newUsername, Action OnResult = null, Action OnFailed = null)
    {
        if (!IsLogged)
        {
            Debug.LogError("Not logged");
            return;
        }

        Bundle profileUpdates = Bundle.CreateObject();
        profileUpdates["displayName"] = new Bundle(_newUsername);

        loggedGamer.Profile.Set(profileUpdates)
        .Done(profileRes =>
        {
            OnResult?.Invoke();
        }, ex =>
        {
            OnFailed?.Invoke();

            CotcException error = (CotcException)ex;
            Debug.LogError("Could not set profile data due to error: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }
    public void GetUsername(Action<string> OnResult = null, Action OnFailed = null)
    {
        if (!IsLogged)
        {
            Debug.LogError("Not logged");
            return;
        }

        loggedGamer.Profile.Get()
        .Done(gamerProfile =>
        {
            OnResult?.Invoke(gamerProfile["displayName"]);  
        }, ex =>
        {
            OnFailed?.Invoke();

            CotcException error = (CotcException)ex;
            Debug.LogError("Could not get profile data due to error: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }


    //----------------------------------------Miscellaneous

    //Utilities for account
    public const string MatchEmailPattern =
    @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
    + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
      + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
    + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$";

    public static bool IsEmail(string _email)
    {
        if (_email != null) return Regex.IsMatch(_email, MatchEmailPattern);
        else return false;
    }

    //We check only for digits, no need more here
    public static bool IsPassword(string password)
    {
        const int MIN_LENGTH = 6;
        //const int MAX_LENGTH = 15;

        if (password == null) throw new ArgumentNullException();

        bool meetsLengthRequirements = password.Length >= MIN_LENGTH/* && password.Length <= MAX_LENGTH*/;
        //bool hasUpperCaseLetter = false;
        //bool hasLowerCaseLetter = false;
        bool hasDecimalDigit = false;

        if (meetsLengthRequirements)
        {
            foreach (char c in password)
            {
                //if (char.IsUpper(c)) hasUpperCaseLetter = true;
                //else if (char.IsLower(c)) hasLowerCaseLetter = true;
                //else 
                if (char.IsDigit(c))
                {
                    hasDecimalDigit = true;
                    break;
                }
            }
        }

        bool isValid = meetsLengthRequirements
                    //&& hasUpperCaseLetter
                    //&& hasLowerCaseLetter
                    && hasDecimalDigit
                    ;
        return isValid;

    }

    //Test Events
    //private DomainEventLoop loopEvents = null;
    //private void DidLogin(Gamer newGamer)
    //{
    //    // Another loop was running; unless you want to keep multiple users active, stop the previous
    //    if (loopEvents != null)
    //        loopEvents.Stop();
    //    loopEvents = newGamer.StartEventLoop();
    //    loopEvents.ReceivedEvent += Loop_ReceivedEvent;
    //}
    //private void Loop_ReceivedEvent(DomainEventLoop sender, EventLoopArgs e)
    //{
    //    Debug.Log("Received event of type " + e.Message.Type + ": " + e.Message.ToJson());
    //}
}
