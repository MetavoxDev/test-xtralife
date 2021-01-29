/* Copyright 2021, Allan Arnaudin, All rights reserved. */
using RedRunner.UI;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Controls the account interface for the player
public class UIAccountWindow : MonoBehaviour 
{
    public enum WindowState
    {
        SignIn,
        SignUp,
        Connected
    }
    [SerializeField] private UIWindow window = null;

    [Header("Refs form")]
    [SerializeField] private RectTransform rectMainBlock = null;
    [SerializeField] private TextMeshProUGUI title = null;
    [SerializeField] private TMP_InputField emailInput = null;
    [SerializeField] private TMP_InputField passwordInput = null;

    [Header("Refs buttons")]
    [SerializeField] private CanvasGroup bottomButtonsCanvas = null;
    [SerializeField] private CanvasGroup validationCanvas = null;
    [SerializeField] private Button cancelButton = null;
    [SerializeField] private Button validationButton = null;
    [Space()]
    [SerializeField] private Button closeButton = null; //When we are connected, useful to close the window (UX: The user will understand this will close the window and not cancel his connection)
    [SerializeField] private Button recoverPasswordButton = null;
    [Space()]
    [SerializeField] private Button goToSignUpButton = null;
    [SerializeField] private Button goToSignInButton = null;
    [SerializeField] private Button disconnectButton = null;
    [SerializeField] private CanvasGroup goToSignUpCanvas = null;
    [SerializeField] private CanvasGroup goToSignInCanvas = null;
    [SerializeField] private CanvasGroup disconnectCanvas = null;
    [Space()]
    [SerializeField] private GameObject[] editableSigns = null;

    [Header("Refs signs")]
    [SerializeField] private Sprite disconnectedMailSprite = null;
    [SerializeField] private Color disconnectedColorSprite = new Color(0.43f, 0.47f, 0.5f);
    [SerializeField] private Sprite connectMailSprite = null;
    [SerializeField] private Color connectedColorSprite = new Color(0.495f, 1f, 0.5f);

    [Space()]

    [SerializeField] private Image[] signsConnectionMail = null;

    [Space()]
    [SerializeField] private Image emailButton = null;
    [SerializeField] private Sprite emailConnectedButtonPicture = null;
    [SerializeField] private Sprite emailDisconnectedButtonPicture = null;
    [Space()]
    [SerializeField] private Image profileNameButton = null;
    [SerializeField] private Sprite profileConnectedButtonPicture = null;
    [SerializeField] private Sprite profileDisconnectedButtonPicture = null;
    [Space()]
    [SerializeField] private Image editProfileNameButton = null;
    [SerializeField] private Sprite editProfileNameConnectedButtonPicture = null;
    [SerializeField] private Sprite editProfileNameDisconnectedButtonPicture = null;

    private WindowState currentState = WindowState.SignUp;

    private string cachedMail = "";
    private string cachedPass = "";

    private void Start()
    {
        InitShakingFeature();
        InitHintFeature();

        SetSignsConnected(false);
        CloudManager.Instance.Profile.OnLogged += OnLogged;
    }
    private void OnEnable()
    {
        validationButton.onClick.AddListener(OnValidation);
        cancelButton.onClick.AddListener(OnCancel);
        closeButton.onClick.AddListener(OnCancel);

        recoverPasswordButton.onClick.AddListener(OnRecover);

        goToSignUpButton.onClick.AddListener(GoToSignUp);
        goToSignInButton.onClick.AddListener(GoToSignIn);
        disconnectButton.onClick.AddListener(Disconnect);
    }
    private void OnDisable()
    {
        goToSignUpButton.onClick.RemoveListener(GoToSignUp);
        goToSignInButton.onClick.RemoveListener(GoToSignIn);
        disconnectButton.onClick.RemoveListener(Disconnect);

        recoverPasswordButton.onClick.RemoveListener(OnRecover);

        validationButton.onClick.RemoveListener(OnValidation);
        CloudManager.Instance.Profile.OnLogged -= OnLogged;
    }

    private void OnLogged()
    {
        if (CloudManager.Instance.Profile.IsRegisteredEmail)
        {
            cachedMail = emailInput.text = PlayerPrefs.GetString(ProfileHolder.KEY_ID);
            cachedPass = passwordInput.text = PlayerPrefs.GetString(ProfileHolder.KEY_PWD);

            ChangeState(WindowState.Connected);
        }
        else
        {
            cachedMail = emailInput.text = "";
            cachedPass = passwordInput.text = "";

            ChangeState(WindowState.SignIn);
        }
    }
    private void OnValidation()
    {
        if (!ProfileHolder.IsEmail(emailInput.text)) 
        {
            HintItIsNotEmail();
            ShakeCourriel();
            return; 
        }

        switch (currentState)
        {
            case WindowState.SignIn:

                if (passwordInput.text != "" && ProfileHolder.IsPassword(passwordInput.text))
                {
                    CloudManager.Instance.Profile.SignIn(emailInput.text, passwordInput.text, OnSuccessSignIn, FailedSignIn);
                }
                else
                {
                    CloudManager.Instance.Profile.SignIn(emailInput.text, PlayerPrefs.GetString(ProfileHolder.KEY_PWD), OnSuccessSignIn, FailedSignIn);
                }

                //Automatic accound creation (I don't want this but it's possible like this)
                //CloudManager.Instance.Profile.LoginWithEmail(emailInput.text, passwordInput.text, OnFinishedMailAndPasswordEditing, OnFailedSignIn);
                break;
            case WindowState.SignUp:
                if(passwordInput.text != cachedPass) //Has changed his password
                {
                    if (ProfileHolder.IsPassword(passwordInput.text))
                    {
                        CloudManager.Instance.Profile.SignUp(emailInput.text, passwordInput.text, OnFinishedMailAndPasswordEditing, OnNoRegisteredEmail);
                    }
                    else
                    {
                        HintItIsNotAPassword();
                        ShakePadlock();
                    }
                }
                else //Has not changed his password in forms
                {
                    if (ProfileHolder.IsPassword(PlayerPrefs.GetString(ProfileHolder.KEY_PWD)))
                    {
                        CloudManager.Instance.Profile.SignUp(emailInput.text, PlayerPrefs.GetString(ProfileHolder.KEY_PWD), OnFinishedMailAndPasswordEditing, OnAlreadyRegisteredEmail);
                    }
                    else
                    {
                        ShakePadlock();
                        Debug.LogError("The user has changed is password not properly ??");
                    }
                }
                break;
            default: //Connected
                if (emailInput.text != cachedMail)
                    CloudManager.Instance.Profile.ChangeEmail(emailInput.text, OnFinishedMailEditingAndChangeState);

                if (passwordInput.text != PlayerPrefs.GetString(ProfileHolder.KEY_PWD) && ProfileHolder.IsPassword(passwordInput.text))
                    CloudManager.Instance.Profile.ChangePassword(passwordInput.text, OnFinishedPasswordEditingAndChangeState);
                break;
        }
    }
    private void OnCancel()
    {
        emailInput.text = cachedMail;
        passwordInput.text = cachedPass;

        CloseWindow();
    }
    private void OnRecover()
    {
        if (!ProfileHolder.IsEmail(emailInput.text))
        {
            HintItIsNotEmail();
            Debug.LogWarning("Need a real email to recover");
            return;
        }

        HintCantRecoveredPassword(); //TO DELETE IF WE USE A REAL API KEY (registered with no 2FA and with a debit card)
        CloudManager.Instance.Profile.ResetPassword(emailInput.text, OnRecovered, OnFailedRecovered);
    }

    private void OnRecovered()
    {
        HintRecoveredPassword();
    }
    private void OnFailedRecovered()
    {
        HintNoRegisteredEmail();
    }

    //General results
    private void OnFinishedMailAndPasswordEditing(string _newMail, string _pwd)
    {
        OnFinishedMailEditing(_newMail);
        OnFinishedPasswordEditing(_pwd);
        ChangeState(WindowState.Connected);
        CloseWindow();
    }
    private void OnFinishedMailEditingAndChangeState(string _newMail)
    {
        OnFinishedMailEditing(_newMail);
        ChangeState(WindowState.Connected);
    }
    private void OnFinishedMailEditing(string _newMail)
    {
        cachedMail = _newMail;
    }
    private void OnFinishedPasswordEditingAndChangeState(string _newPassword)
    {
        OnFinishedPasswordEditing(_newPassword);
        ChangeState(WindowState.Connected);
    }
    private void OnFinishedPasswordEditing(string _newPassword)
    {
        cachedPass = _newPassword;
    }

    //SignIn Results (For security, I think it's useless to have a splitted behaviour to know if this email is registered. But I would like to show that it's possible to)
    private void OnSuccessSignIn(string _id, string _pwd)
    {
        if(ProfileHolder.IsEmail(_id))
            PlayerPrefs.SetInt(ProfileHolder.KEY_NETWORK, 1);
        else
            PlayerPrefs.SetInt(ProfileHolder.KEY_NETWORK, 0);

        PlayerPrefs.SetString(ProfileHolder.KEY_ID, _id);
        PlayerPrefs.SetString(ProfileHolder.KEY_PWD, _pwd);
        PlayerPrefs.Save();

        cachedMail = _id;
        cachedPass = _pwd;

        ChangeState(WindowState.Connected);
        CloseWindow();
    }
    private void FailedSignIn(string _id, string _pwd)
    {
        HintWrongEmailPassword();
        ShakeCourriel();
        ShakePadlock();
    }
    private void OnNoRegisteredEmail()
    {
        HintNoRegisteredEmail();
        ShakeCourriel();
    }

    //SignUp Results
    private void OnAlreadyRegisteredEmail()
    {
        HintEmailIsAlreadyExisting();
        ShakeCourriel();
    }

    private void GoToSignUp()
    {
        ChangeState(WindowState.SignUp);
    }
    private void GoToSignIn()
    {
        ChangeState(WindowState.SignIn);
    }
    private void Disconnect()
    {
        CloudManager.Instance.Profile.LoginAnonymously();
        ChangeState(WindowState.SignIn);
    }

    private void CloseWindow()
    {
        window.Close();
    }

    public void ChangeState(WindowState _newState)
    {
        emailInput.text = cachedMail;
        passwordInput.text = cachedPass;

        if (_newState == currentState) return;

        TextMeshProUGUI placeHolder;
        switch (_newState)
        {
            case WindowState.SignIn:
                emailInput.text = "";
                passwordInput.text = "";

                placeHolder = ((TextMeshProUGUI)passwordInput.placeholder);
                if (placeHolder)
                    placeHolder.text = "";

                //Buttons
                bottomButtonsCanvas.alpha = 1;
                bottomButtonsCanvas.interactable = true;

                validationCanvas.ignoreParentGroups = false;

                closeButton.image.enabled = false;
                closeButton.interactable = false;
                recoverPasswordButton.image.enabled = true;
                recoverPasswordButton.interactable = true;

                //UI
                rectMainBlock.anchoredPosition = Vector3.zero;
                title.text = "Sign In";
                goToSignUpCanvas.alpha = 1;
                goToSignUpCanvas.interactable = true;
                goToSignUpCanvas.blocksRaycasts = true;

                goToSignInCanvas.alpha = 0;
                goToSignInCanvas.interactable = false;
                goToSignInCanvas.blocksRaycasts = false;

                disconnectCanvas.alpha = 0;
                disconnectCanvas.interactable = false;
                disconnectCanvas.blocksRaycasts = false;

                for (int i = 0; i < editableSigns.Length; i++)
                    editableSigns[i].SetActive(false);

                SetSignsConnected(false);
                break;
            case WindowState.SignUp:
                emailInput.text = "";
                passwordInput.text = "";

                placeHolder = ((TextMeshProUGUI)passwordInput.placeholder);
                if(placeHolder)
                    placeHolder.text = "Optional";

                //Buttons
                bottomButtonsCanvas.alpha = 1;
                bottomButtonsCanvas.interactable = true;

                validationCanvas.ignoreParentGroups = false;

                closeButton.image.enabled = false;
                closeButton.interactable = false;
                recoverPasswordButton.image.enabled = false;
                recoverPasswordButton.interactable = false;

                //UI
                rectMainBlock.anchoredPosition = Vector3.zero;
                title.text = "Sign Up";
                goToSignUpCanvas.alpha = 0;
                goToSignUpCanvas.interactable = false;
                goToSignUpCanvas.blocksRaycasts = false;

                goToSignInCanvas.alpha = 1;
                goToSignInCanvas.interactable = true;
                goToSignInCanvas.blocksRaycasts = true;

                disconnectCanvas.alpha = 0;
                disconnectCanvas.interactable = false;
                disconnectCanvas.blocksRaycasts = false;

                for (int i = 0; i < editableSigns.Length; i++)
                    editableSigns[i].SetActive(false);

                SetSignsConnected(false);
                break;
            case WindowState.Connected:
                emailInput.text = cachedMail;
                passwordInput.text = cachedPass;

                placeHolder = ((TextMeshProUGUI)passwordInput.placeholder);
                if (placeHolder)
                    placeHolder.text = "";

                //Buttons
                bottomButtonsCanvas.alpha = 0;
                bottomButtonsCanvas.interactable = false;

                validationCanvas.ignoreParentGroups = false;

                closeButton.image.enabled = true;
                closeButton.interactable = true;
                recoverPasswordButton.image.enabled = false;
                recoverPasswordButton.interactable = false;

                //UI
                //Center
                rectMainBlock.anchoredPosition = new Vector3(80, 0, 0);
                title.text = "Account";
                goToSignUpCanvas.alpha = 0;
                goToSignUpCanvas.interactable = false;
                goToSignUpCanvas.blocksRaycasts = false;

                goToSignInCanvas.alpha = 0;
                goToSignInCanvas.interactable = false;
                goToSignInCanvas.blocksRaycasts = false;

                disconnectCanvas.alpha = 1;
                disconnectCanvas.interactable = true;
                disconnectCanvas.blocksRaycasts = true;

                for (int i = 0; i < editableSigns.Length; i++)
                    editableSigns[i].SetActive(true);

                SetSignsConnected(true);
                break;
            default:
                break;
        }
        currentState = _newState;
    }
    private void SetSignsConnected(bool _isConnectedWithMail)
    {
        if (_isConnectedWithMail)
        {
            emailButton.sprite = emailConnectedButtonPicture;
            profileNameButton.sprite = profileConnectedButtonPicture;
            editProfileNameButton.sprite = editProfileNameConnectedButtonPicture;

            for (int i = 0; i < signsConnectionMail.Length; i++)
            {
                signsConnectionMail[i].sprite = connectMailSprite;
                signsConnectionMail[i].color = connectedColorSprite;
            }
        }
        else
        {
            emailButton.sprite = emailDisconnectedButtonPicture;
            profileNameButton.sprite = profileDisconnectedButtonPicture;
            editProfileNameButton.sprite = editProfileNameDisconnectedButtonPicture;

            for (int i = 0; i < signsConnectionMail.Length; i++)
            {
                signsConnectionMail[i].sprite = disconnectedMailSprite;
                signsConnectionMail[i].color = disconnectedColorSprite;
            }
        }
    }

    //---------------------------------------------------------------Sign Utilities
    [Space()]
    [Header("Shaking Signs")]
    [SerializeField] private Image padlock = null;
    [SerializeField] private Image courriel = null;
    [SerializeField] private float shakingDuration = 1f;
    [SerializeField] private Gradient shakingColor = new Gradient();
    [SerializeField] private AnimationCurve shakingCurve = AnimationCurve.Linear(0,0,1,0);
    [SerializeField] private float shakingForkDegrees = 60f;

    private Coroutine padlockCoroutine = null;
    private Coroutine courrielCoroutine = null;
    private void InitShakingFeature()
    {
        padlock.color = courriel.color = shakingColor.Evaluate(0);
    }
    private void ShakePadlock()
    {
        if (padlockCoroutine != null)
            StopCoroutine(padlockCoroutine);

        padlockCoroutine = StartCoroutine(ShakingPicture(padlock));
    }
    private void ShakeCourriel()
    {
        if (courrielCoroutine != null)
            StopCoroutine(courrielCoroutine);

        courrielCoroutine = StartCoroutine(ShakingPicture(courriel));
    }

    private IEnumerator ShakingPicture(Image _pictureToShake)
    {
        float elapsedTime = 0;
        while (elapsedTime < shakingDuration)
        {
            float progression = elapsedTime / shakingDuration;
            _pictureToShake.transform.rotation = Quaternion.Euler(_pictureToShake.rectTransform.rotation.eulerAngles.x, _pictureToShake.rectTransform.rotation.eulerAngles.y, shakingCurve.Evaluate(progression) * shakingForkDegrees);
            _pictureToShake.color = shakingColor.Evaluate(progression);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        _pictureToShake.rectTransform.rotation = Quaternion.Euler(_pictureToShake.rectTransform.rotation.eulerAngles.x, _pictureToShake.rectTransform.rotation.eulerAngles.y, 0);
        _pictureToShake.color = shakingColor.Evaluate(0);
    }

    [System.Serializable]
    private struct HintForm
    {
        public string hint;
        public Color hintColor;

        public HintForm(string _hint)
        {
            hint = _hint;
            hintColor = Color.white;
        }
        public HintForm(string _hint, Color _color)
        {
            hint = _hint;
            hintColor = _color;
        }
    }

    //---------------------------------------------------------------Feedbacks Utilities (We could split this part in another script to avoid blob pattern)
    [Header("Result Feedbacks")]
    [SerializeField] private TextMeshProUGUI hintsFormExplanation = null;
    [SerializeField] private float hintDuration = 2f;
    [SerializeField] private AnimationCurve alphaHintCurve = AnimationCurve.EaseInOut(0,1,1,0);
    [SerializeField] private HintForm hintRecoveredPassword = new HintForm("A password has been sent to your email", new Color(0.495f, 1f, 0.5f));
    [SerializeField] private HintForm hintCantRecoveredPassword = new HintForm("Need a 'real' API key, but usually it's working", new Color(1f, 0.3f, 0.32f));
    [SerializeField] private HintForm hintWrongEmailPassword = new HintForm("Wrong password/email", new Color(1f, 0.3f, 0.32f));
    [SerializeField] private HintForm hintItIsNotAPassword = new HintForm("The password must contain at least 6 characters (numbers+letters)", new Color(1f, 0.3f, 0.32f));
    [SerializeField] private HintForm hintItIsNotEmail = new HintForm("Please refer an email", new Color(1f, 0.3f, 0.32f));
    [SerializeField] private HintForm hintNoRegisteredEmail = new HintForm("This email is no registered", new Color(1f, 0.8f, 0.3f));
    [SerializeField] private HintForm hintEmailIsAlreadyExisting = new HintForm("This email is already registered", new Color(1f, 0.8f, 0.3f));

    private Coroutine hintCoroutine = null;
    private void InitHintFeature()
    {
        hintsFormExplanation.color = new Color(1,1,1,0);
        hintsFormExplanation.text = "";
    }
    private void HintRecoveredPassword()
    {
        if (hintCoroutine != null)
            StopCoroutine(hintCoroutine);

        hintCoroutine = StartCoroutine(HintAppear(hintRecoveredPassword));
    }
    private void HintCantRecoveredPassword()
    {
        if (hintCoroutine != null)
            StopCoroutine(hintCoroutine);

        hintCoroutine = StartCoroutine(HintAppear(hintCantRecoveredPassword));
    }
    private void HintWrongEmailPassword()
    {
        if (hintCoroutine != null)
            StopCoroutine(hintCoroutine);

        hintCoroutine = StartCoroutine(HintAppear(hintWrongEmailPassword));
    }
    private void HintItIsNotAPassword()
    {
        if (hintCoroutine != null)
            StopCoroutine(hintCoroutine);

        hintCoroutine = StartCoroutine(HintAppear(hintItIsNotAPassword));
    }
    private void HintItIsNotEmail()
    {
        if (hintCoroutine != null)
            StopCoroutine(hintCoroutine);

        hintCoroutine = StartCoroutine(HintAppear(hintItIsNotEmail));
    }
    private void HintNoRegisteredEmail()
    {
        if (hintCoroutine != null)
            StopCoroutine(hintCoroutine);

        hintCoroutine = StartCoroutine(HintAppear(hintNoRegisteredEmail));
    }
    private void HintEmailIsAlreadyExisting()
    {
        if (hintCoroutine != null)
            StopCoroutine(hintCoroutine);

        hintCoroutine = StartCoroutine(HintAppear(hintEmailIsAlreadyExisting));
    }

    private IEnumerator HintAppear(HintForm _hint)
    {
        float elapsedTime = 0;

        hintsFormExplanation.text = _hint.hint;
        hintsFormExplanation.alpha = 1;
        hintsFormExplanation.color = _hint.hintColor;

        while (elapsedTime < hintDuration)
        {
            hintsFormExplanation.alpha = alphaHintCurve.Evaluate(elapsedTime / hintDuration);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        hintsFormExplanation.alpha = 0;
        hintsFormExplanation.text = "";
        hintsFormExplanation.color = new Color(1, 1, 1, 0);
        hintCoroutine = null;
    }
}
