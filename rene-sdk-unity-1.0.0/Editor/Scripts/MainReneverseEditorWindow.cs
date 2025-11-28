using System;
using Rene.Sdk;
using Rene.Sdk.Api.Game.Data;
using ReneVerse;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class MainReneverseEditorWindow : EditorWindow
{
    private bool _isConnected;
    private bool _isLoading;

    public bool IsConnected
    {
        get => SessionState.GetBool(PrefKeyIsConnected, _isConnected);
        private set
        {
            _isConnected = value;
            SessionState.SetBool(PrefKeyIsConnected, _isConnected);
        }
    }


    #region consts

    private Texture2D _logo;
    private Texture2D _loadingIcon;

    #endregion

    private Vector2 scrollPosition;

    private ReneAPICreds _reneAPICreds;

    private static string apiHost;

    private bool feedbackFoldout; // A variable to store the foldout state

    private string feedbackText = ""; // A variable to store the feedback text

    private static MainReneverseEditorWindow _instance;

    private API api;

    private const string PrefKeyIsConnected = "CredentialsEditor_IsConnected";

    private void OnEnable()
    {
        _instance = this;
        MakeConnectionUnitySessionLong();
    }

    private void MakeConnectionUnitySessionLong() =>
        _isConnected = SessionState.GetBool(PrefKeyIsConnected, false);

    private void OnDisable()
    {
        if (_instance == this) _instance = null;
    }

    [MenuItem(Constants.WindowReneverseSettings + Constants.ReneverseShortcut)]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow<MainReneverseEditorWindow>();
        window.titleContent = new GUIContent(Constants.TabName);
    }

    private void Awake()
    {
        _logo = (Texture2D)Resources.Load(Constants.LogoSprite, typeof(Texture2D));
        _loadingIcon = (Texture2D)Resources.Load(Constants.LoadingIcon, typeof(Texture2D));

        _reneAPICreds ??= Resources.Load<ReneAPICreds>(nameof(ReneAPICreds));
    }

    private RenderPipelineAsset currentRenderPipeline;
    private bool IsMatSet;

    private void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        CenterLogo();
        GreetMessage(Constants.GreetingMessage);


        _credentialsFoldout = EditorGUILayout.Foldout(_credentialsFoldout, Constants.ReneCredentialsFoldoutHeader);
        if (_credentialsFoldout)
        {
            CreateLabel(Constants.InstructionText);

            CredentialTextArea(ref _reneAPICreds.APIKey, nameof(_reneAPICreds.APIKey));
            CredentialTextArea(ref _reneAPICreds.PrivateKey, nameof(_reneAPICreds.PrivateKey));

            AddURLButton(Constants.RegisterButtonName, Constants.ReneProdURLLink.ToLoginUrl(), Constants.RegisterTip);
            AddURLButton(Constants.DocsCheckButtonName, Constants.DocsURL, Constants.DocsTip);


            ButtonFunctionality(Constants.ConnectToGame, Constants.ConnectTip, GameConnect);


            CreateLabel(Constants.Reminder);
        }

        if (IsConnected)
        {
            GameRelatedPersistentData gameDataContainer = Helper.LoadAndGetGameRelatedPersistentData;
            ShowAdSurfacesAndAssets(gameDataContainer);
        }

        FeedBackFoldOut();
        GUILayout.EndScrollView();

        Rect windowRect = GUILayoutUtility.GetLastRect();

        if (_isLoading) DrawSpinningLoadingIcon(windowRect);
    }

    private static void ShowAdSurfacesAndAssets(GameRelatedPersistentData gameDataContainer)
    {
        AssetTemplatesFoldout.Instance.AdSurfacesFoldout
            ("No Ad Surfaces Found In Game", gameDataContainer);
        AssetTemplatesFoldout.Instance.AssetsFoldouts
            ("No assets found", gameDataContainer);
    }


    private void DrawSpinningLoadingIcon(Rect windowRect)
    {
        // Draw the loading icon overlay
        GUI.BeginGroup(new Rect(0, 0, position.width, position.height));
        Vector2 iconPosition = new Vector2(windowRect.width / 2, windowRect.height / 2);
        Rect iconRect = new Rect(iconPosition.x - _loadingIcon.width / 2, iconPosition.y - _loadingIcon.height / 2,
            _loadingIcon.width, _loadingIcon.height);

        Matrix4x4 matrixBackup = GUI.matrix;
        GUIUtility.RotateAroundPivot(rotationAngle, iconPosition);
        GUI.DrawTexture(iconRect, _loadingIcon);
        GUI.matrix = matrixBackup;
        GUI.EndGroup();
    }

    private void UpdateLoadingIconRotation()
    {
        rotationAngle += 2; // Adjust this value for speed
        if (rotationAngle >= 360) rotationAngle -= 360;

        Repaint();
    }

    private float rotationAngle = 0f;

    private void CleanUpData()
    {
        // Your disconnect logic here
        IsConnected = false;
        Helper.ResetGameRelatedPersistentData();
        ReneAPIManager.ReneAPICreds.DeleteAuthToken();
    }

    private bool _credentialsFoldout = true;
    private GameResponse.GameData gameData;


    private async void GameConnect()
    {
        try
        {
            CleanUpData();
            EditorApplication.update += UpdateLoadingIconRotation;
            _isLoading = true;
            API api = ReneAPIManager.InitializeAPI();
            ReneAPIManager.ReneAPICreds.SaveAPI(api);

            ReneAPIManager.ReneAPICreds.SaveAuthToken(api.AuthToken);
            gameData = await api.Game().GameData();

            GameRelatedPersistentData gameRelatedPersistentData = Helper.LoadAndGetGameRelatedPersistentData;
            await gameRelatedPersistentData.SaveGameRelatedData(gameData);
            IsConnected = true;
            EditorApplication.update -= UpdateLoadingIconRotation;
        }
        //TODO: Make debugs only on the api level
        catch (Exception)
        {
            CleanUpData();
            Debug.LogError("Error occurred during GameConnect. Check that you've written the correct API keys.");
        }
        finally
        {
            _isLoading = false;
            //OpenOwnableAssetsOnGameConnected();
        }
    }


    private void ButtonFunctionality
    (string buttonName,
        string tooltip,
        Action onButtonClicked)
    {
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            richText = true
        };
        GUIContent buttonContent = new GUIContent(buttonName, tooltip);
        if (GUILayout.Button(buttonContent, buttonStyle))
        {
            onButtonClicked?.Invoke();
        }
    }


    private async void FeedBackFoldOut()
    {
        feedbackFoldout = EditorGUILayout.Foldout(feedbackFoldout, Constants.FeedbackFoldoutHeader);
        if (feedbackFoldout)
        {
            // Create a text area where the user can enter their feedback
            feedbackText = EditorGUILayout.TextArea(feedbackText, GUILayout.ExpandHeight(true));

            // Create a button that sends the feedback to your Slack channel when clicked
            if (GUILayout.Button(Constants.SendFeedbackButton))
            {
                if (feedbackText == null) EmptyFeedbackDisplayDialogue();
                else
                {
                    string messageToSend = feedbackText;
                    ClearFeedbackTextArea();
                    // Call your Slack message-sending code here
                    // Assumes you have a method that sends a message to Slack
                    await SlackMessageSender.PostMessage(messageToSend, SuccessfulFeedbackDisplayDialogue);
                }
            }
        }
    }

    private void ClearFeedbackTextArea()
    {
        feedbackText = "";
        GUI.FocusControl(null); // Remove focus from the text area
        Repaint(); // Request the editor window to repaint
    }

    private void SuccessfulFeedbackDisplayDialogue()
    {
        EditorUtility.DisplayDialog(Constants.FeedBackSent, Constants.FeedBackSentMessage, Constants.Ok);
    }

    private void EmptyFeedbackDisplayDialogue()
    {
        EditorUtility.DisplayDialog(Constants.FeedBackNotSent, Constants.FeedBackNotSent, Constants.Ok);
    }

    private static void AddURLButton(string buttonName, string urlToOpen, string tooltip)
    {
        if (CreateButton(buttonName, tooltip)) Application.OpenURL(urlToOpen);
    }

    private static bool CreateButton(string buttonName, string tooltip)
    {
        return GUILayout.Button(new GUIContent(buttonName, tooltip));
    }


    private static void CreateLabel(string labelText)
    {
        GUILayout.Label(labelText, EditorStyles.label);
    }

    /// <summary>
    /// <see cref="CredentialTextArea"/> without reflection
    /// </summary>
    /// <param name="userCredential"></param>
    /// <param name="keyName"></param>
    private void CredentialTextArea(ref string credential, string keyName)
    {
        GUIStyle wordWrapStyle = new GUIStyle(GUI.skin.textArea) { wordWrap = true };

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(keyName.UnderscoresToSpaces(), GUILayout.Width(150));

        EditorGUI.BeginChangeCheck();
        string newCredential = EditorGUILayout.TextArea(credential, wordWrapStyle, GUILayout.ExpandWidth(true));
        if (EditorGUI.EndChangeCheck())
        {
            credential = newCredential;
            SaveCredentials();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void SaveCredentials()
    {
        if (_reneAPICreds != null)
        {
            _reneAPICreds.SaveAPIKeys(_reneAPICreds.APIKey, _reneAPICreds.PrivateKey);
        }
    }


    private void SaveOnTextChanged(string value, string fieldName)
    {
        var fieldInfo = _reneAPICreds.GetType().GetField(fieldName);
        if (fieldInfo != null)
        {
            fieldInfo.SetValue(_reneAPICreds, value);
            EditorUtility.SetDirty(_reneAPICreds);
            AssetDatabase.SaveAssets();
        }
    }


    private static void GreetMessage(string labelText)
    {
        GUILayout.Space(20f);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 24;
        GUILayout.Label(labelText, style);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(20f);
    }

    private void CenterLogo()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical(GUILayout.Width(_logo.width));

        GUIStyle logoBackground = new GUIStyle();
        logoBackground.padding = new RectOffset(0, 0, 0, 0);
        logoBackground.margin = new RectOffset(0, 0, 0, 0);
        logoBackground.border = new RectOffset(0, 0, 0, 0);
        logoBackground.normal.background = _logo;
        logoBackground.normal.background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        Color grayColor = new Color(56 / 255f, 56 / 255f, 56 / 255f);
        logoBackground.normal.background.SetPixel(0, 0, grayColor);
        logoBackground.normal.background.Apply();

        GUILayout.Label(_logo, logoBackground, GUILayout.MaxWidth(_logo.width), GUILayout.MaxHeight(_logo.height));
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
}