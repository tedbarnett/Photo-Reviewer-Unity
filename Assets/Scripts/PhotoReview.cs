using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using DBXSync;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PhotoReview : MonoBehaviour
{
    // Button code examples: https://docs.unity3d.com/2019.1/Documentation/ScriptReference/UI.Button-onClick.html
    // Load image resources: https://answers.unity.com/questions/892113/how-can-i-change-a-ui-image-from-a-large-list-of-i.html

    // Attach these items in the Inspector
    public RectTransform PhotoRawImage;
    public RawImage PhotoRawImageObject;
    public RectTransform SlideShowImage;
    //public GameObject PhotoHolder;

    public Button m_PreviousButton, m_NextButton;
    public GameObject StartButton, LoadingPauseButton, VoiceNoteButton, MainMenuButton;
    public GameObject StartMessage, InfoPanel, MainMenuPanel, ErrorMessagePanel, VoiceNoteEditingPanel, PreventClicksPanel;
    public GameObject ChangeEditorNamePanel, EditorNamePanel;
    public GameObject LocationAndDateEditor, LocationAndDateText;
    public InputField MonthInput, DayInput, YearInput, LocationInput, CommentsInput;
    public Text MonthText, DayText, YearText;
    public Text LocationText, DateText, CommentsText, DebugText, ErrorMessageText, editorNameText;
    public Sprite speakerIcon;
    public Sprite microphoneIcon;
    public GameObject AutoCompleteScrollRectObject;
    public GameObject RestartDialog, TestingNotice;
    public bool infoPanelsOpen;
    public Toggle forgetPhotoToggle; // actions assigned in Inspector
    public Toggle favoritePhotoToggle; // actions assigned in Inspector
    public GameObject infoToggleIcon; // actions assigned in Inspector
    public string editorName; //TODO: Store this in persistent cache.  Ask user for name at Startup.
    public string dropBoxDataFolder;
    public AudioSource slideShowAsrc;
    public GameObject favoritesChecked;
    public GameObject forgottenChecked;
    public Text currentPhotoText;

    [HideInInspector]
    public int currentPhoto;
    [HideInInspector]
    public bool settingUpTextFields = true;
    [HideInInspector]
    public List<string> uniqueLocationsList = new List<string>();
    [HideInInspector]
    public bool isBusyLoading;
    [HideInInspector]
    public int latestIncrement;
    [HideInInspector]
    public bool playingSlideShow;
    [HideInInspector]
    public int slideCount;
    [HideInInspector]
    public int currentSlide;

    private List<string> listOfValidPhotoExtensions = new List<string>();

    // private variables
    private GoogleSheetsForUnity.GoogleSheetAccess googleSheetsForUnity;
    private FirebaseConnect firebaseConnect;
    private BasicAudio basicAudio; // Access variables and scripts in BasicAudio.cs
    private FitMyChild fitMyChild; // Access variables and scripts in FitMyChild.cs (resizes info panel as needed)
    private AutoCompleteInput autoCompleteInput; // Access variables and scripts in AutoCompleteScrollRect.cs

    private Texture[] loadedPhotoTextures; // used to cache photos loaded from Dropbox
    public AudioClip[] loadedAudioClips;
    private Color lightBlue;
    private Color darkRed; // for "forgotten" photos
    private readonly int YearMin = 1800; // earliest possible year for a photo
    private readonly int YearMax = 2200; // latest possible year for a photo
    private List<string> PhotoName;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private Toggle favoritePhotoToggleState;
    private Toggle forgetPhotoToggleState;
    private Text loadingMessageText;
    private InputField changeEditorNameInputField;
    private bool showFavoritesOnly;
    private bool hideForgottenPhotos;
    private bool mainMenuIsOpen;
    private DateTime lastLoginTime;
    //private DateTime errorCheckTimeStart; // used to countdown time for errors
    // -----------------------------------------------------------------------------------

    void Start()
    {
        // TODO: Debug settings below
        isBusyLoading = true;
        mainMenuIsOpen = false;
        ChangeEditorNamePanel.transform.gameObject.SetActive(true); // Show the ChangeEditorNamePanel (make sure it's active briefly)
        changeEditorNameInputField = ChangeEditorNamePanel.GetComponentInChildren<InputField>();
        ChangeEditorNamePanel.transform.gameObject.SetActive(false); // Now hide the ChangeEditorNamePanel

        googleSheetsForUnity = GetComponent<GoogleSheetsForUnity.GoogleSheetAccess>();
        //firebaseConnect = GetComponent<FirebaseConnect>();
        firebaseConnect = FindObjectOfType<FirebaseConnect>();

        basicAudio = GetComponent<BasicAudio>();
        fitMyChild = GetComponent<FitMyChild>();
        autoCompleteInput = GetComponent<AutoCompleteInput>();
        loadingMessageText = LoadingPauseButton.GetComponentInChildren<Text>();
        favoritePhotoToggleState = favoritePhotoToggle.GetComponent<Toggle>();
        forgetPhotoToggleState = forgetPhotoToggle.GetComponent<Toggle>();

        MainMenuButton.GetComponent<Button>().onClick.AddListener(MainMenuClicked);

            m_PreviousButton.onClick.AddListener(() => NewPhoto(-1)); // previous button
            m_NextButton.onClick.AddListener(() => NewPhoto(+1)); // next button


        lightBlue = new Color(135.0f / 255.0f, 219.0f / 255.0f, 245.0f / 255.0f);
        darkRed = new Color(130.0f / 255.0f, 30.0f / 255.0f, 30.0f / 255.0f);

        editorName = PlayerPrefs.GetString("editorName");
        if (editorName == "")
        {
            PlayerPrefs.SetString("editorName", "Tester"); // Clear editorName for debug testing
            editorName = "Tester";
        }
        currentPhoto = PlayerPrefs.GetInt("currentPhoto", 0);

        SetUpScreen();
        // Set up list of valid photo extensions (supported by Unity Textures)
            listOfValidPhotoExtensions.InsertRange(listOfValidPhotoExtensions.Count, new List<string> { "bmp", "exr", "gif", "hdr", "iff", "jpg", "pict", "png", "psd", "tga", "tif", "jpeg" });
        DeviceChange.OnOrientationChange += MyOrientationChangeCode; // detect screen rotation
    }

    void OnApplicationPause(bool paused) // see https://answers.unity.com/questions/496290/can-somebody-explain-the-onapplicationpausefocus-s.html
    {
        if (!paused)
        {
            // TODO: Compare to last login time to see if a reload is needed (if over one hour?)
            float elapsedMinutes = ((float)TimeElapsedMilliseconds(lastLoginTime)/1000.0f)/60.0f;
            lastLoginTime = DateTime.Now;
            //DebugText.transform.gameObject.SetActive(true); // show latest error messages
            //DebugText.text = "Re-opened at " + System.DateTime.Now + " (" + elapsedMinutes + " mins after last login)";

        }
    }

    void SetUpScreen() // Hide/show relevant items at Start
    {
        DebugText.transform.gameObject.SetActive(false); // displays all error messages TODO: "true" only when testing
        EditorNamePanel.transform.gameObject.SetActive(false); // Hide the EditorNamePanel
        GetEditorName(); // recall editorName from persistent storage (and then load GoogleSheets)
        EditorNamePanel.transform.gameObject.SetActive(false); // Hide the EditorNamePanel
        PreventClicksPanel.transform.gameObject.SetActive(true); // Prevent clicks while loading...
        StartButton.transform.gameObject.SetActive(true); // Show the StartButton
        ChangeEditorNamePanel.transform.gameObject.SetActive(false); // Now hide the ChangeEditorNamePanel

        InfoPanel.transform.gameObject.SetActive(false); // Hide the data entry panel
        ErrorMessagePanel.transform.gameObject.SetActive(false); // Hide the error message panel
        LoadingPauseButton.transform.gameObject.SetActive(false); // Hide the loading message button
        MainMenuPanel.transform.gameObject.SetActive(false); // Hide the Main Menu Items
        PhotoRawImageObject.transform.gameObject.SetActive(false); // Hide the photo until first photo loaded
        SlideShowImage.parent.transform.gameObject.SetActive(false); // Hide the slide show panel
        VoiceNoteEditingPanel.transform.gameObject.SetActive(true); // Show Voice Note Editing panel (when infoPanel is open)
        VoiceNoteButton.transform.gameObject.SetActive(false);
        RestartDialog.transform.gameObject.SetActive(false);
        Image infoIcon = infoToggleIcon.GetComponent<Image>();
        infoIcon.color = new Color(1.0f, 1.0f, 1.0f, 0.0f); // invisible "i" icon at start

        DateText.text = LocationText.text = CommentsText.text = ErrorMessageText.text = "";

        // hide the next/previous buttons if on mobile device
        #if UNITY_IOS
                m_PreviousButton.transform.gameObject.SetActive(false);
                m_NextButton.transform.gameObject.SetActive(false);
        #endif
    }

    void GetEditorName()
    {

        editorName = PlayerPrefs.GetString("editorName");
        Debug.Log("editorName is " + editorName);
        if (editorName == "")
        {
            editorNameText.text = "Click here to enter your <color=lightblue>name</color>.";
        }
        else // if an editorName is defined, load the appropriate set of photos
        {
            editorNameText.text = "Welcome back <color=lightblue>" + editorName + "</color>.";
            PreventClicksPanel.transform.gameObject.SetActive(true); // Prevent clicks while loading...
            changeEditorNameInputField.text = editorName;
            SetStorageRules(editorName);
            googleSheetsForUnity.ReadinessMessage.text = "\n<color=yellow>Loading photo info...</color>";
            googleSheetsForUnity.GetAllPhotos(); // load all the photo data from Google Sheet
            //TODO: Load all photos from firebase
            //firebaseConnect.LoadData();

            isBusyLoading = false;
        }
    }

    public void SetEditorName()
    {
        if (editorName == changeEditorNameInputField.text)
        {
            ChangeEditorNamePanel.transform.gameObject.SetActive(false);
            return; // if name did not change, then skip
        }
        editorName = changeEditorNameInputField.text;
        PlayerPrefs.SetString("editorName", editorName);
        GetEditorName();
        ChangeEditorNamePanel.transform.gameObject.SetActive(false);
        Debug.Log("at SetEditorName, AFTER hiding panel");
        StartMessage.transform.gameObject.SetActive(true); // hide the Start Messages
        SetUpScreen();

    }

    void SetStorageRules(string editorNameTemp)
    {
        lastLoginTime = DateTime.Now; // timestamp loading data to see if a full update is needed later!
        editorNameTemp = editorNameTemp.ToLower();
        if (editorNameTemp == "judy" ||
            editorNameTemp == "judith" ||
            editorNameTemp == "ted" ||
            editorNameTemp == "chris" ||
            editorNameTemp == "jon" ||
            editorNameTemp == "mark" ||
            editorNameTemp == "neil")
        {
            googleSheetsForUnity.AllPhotosSheet = "all_photos";
            basicAudio.dropBoxAudioFolder = "/PhotoReviewerAudio/";
            TestingNotice.transform.gameObject.SetActive(false);
        }
        else
        {
            googleSheetsForUnity.AllPhotosSheet = "test_all_photos";
            basicAudio.dropBoxAudioFolder = "/PhotoReviewerAudio_TEST/";
            TestingNotice.transform.gameObject.SetActive(true);
        }
    }

    public void TapToStart()
    {
        if (editorName == "")
        {
            ChangeEditorNamePanel.transform.gameObject.SetActive(true);
            return;
        }

        PhotoName = new List<string>();

        for (int i = 0; i < googleSheetsForUnity.allPhotos.Length; i++)
        {
            PhotoName.Add(googleSheetsForUnity.allPhotos[i].dropbox_path); // a list of all photo "names" (dropbox_path)
            //if (i < 10) print ("loading: TapToStart, PhotoName[" + i + "] = " + PhotoName[i]);
        }
        loadedPhotoTextures = new Texture[googleSheetsForUnity.allPhotos.Length]; // photo cache
        loadedAudioClips = new AudioClip[googleSheetsForUnity.allPhotos.Length]; // audio clips cache
        BuildLocationsList(); // sets up rank-ordered location names (for use with Autocompletion)

        StartMessage.transform.gameObject.SetActive(false); // hide the Start Messages
        EditorNamePanel.transform.gameObject.SetActive(false); // hide the EditorNamePanel
        NewPhoto(0); // Load the first photo
        StartButton.transform.gameObject.SetActive(false);
        string favoritesOnly = PlayerPrefs.GetString("showFavoritesOnly");
        showFavoritesOnly = (favoritesOnly == "");
        ChangeShowFavoritesOnly(); // value will be inverted when called

        string hideForgotten = PlayerPrefs.GetString("hideForgottenPhotos");
        hideForgottenPhotos = (hideForgotten == "");
        ChangeHideForgottenPhotos(); // value will be inverted when called


        infoPanelsOpen = true;
        ToggleInfoPanel(); // toggle the info panel off to start
    }

    private void Update()
    {
#if UNITY_EDITOR

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NewPhoto(1);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            NewPhoto(-1);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    Selectable selectable = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnUp();
                    if (selectable != null)
                        selectable.Select();
                }
            }
            else
            {
                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    Selectable selectable = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
                    if (selectable != null)
                        selectable.Select();
                }
            }
        }
#endif
        if (!playingSlideShow) return;
        //if (slideShowAsrc.clip == null) return;
        if (slideShowAsrc.isPlaying == false) GetNextSlide();

    }


    public void NewPhoto(int increment) // Go to previous (-1), save (0), or next (+1) photo
    {
        latestIncrement = increment; // used for getting next cached photo
        if (isBusyLoading) return;
        isBusyLoading = true;
        basicAudio.PauseAllMedia(); // stop any playing or recording now in progress
        loadingMessageText.text = "Loading photo...";
        basicAudio.isplaying = false;
        InfoPanel.transform.gameObject.SetActive(false); // hide all photo info while loading new photo...
        Image infoIcon = infoToggleIcon.GetComponent<Image>(); // set back to default color for "i" icon
            if (infoPanelsOpen) infoIcon.color = Color.white;
            else infoIcon.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
        fitMyChild.isInputCurrently = false; //TODO: Necessary?

        bool isPhoto = false; // used to skip over non-photos
        while (!isPhoto)
        { // skips over non-photo (e.g. video and audio) files
            currentPhoto += increment;
            if (currentPhoto > PhotoName.Count - 1) currentPhoto = 0;
            if (currentPhoto < 0) currentPhoto = PhotoName.Count - 1;
            isPhoto = true;

            if (!isValidPhotoFile(googleSheetsForUnity.allPhotos[currentPhoto].filename))
            {
                isPhoto = false;
                Debug.Log("Skipping file: " + googleSheetsForUnity.allPhotos[currentPhoto].filename);
            }
            if (hideForgottenPhotos && googleSheetsForUnity.allPhotos[currentPhoto].forget_photo != "") isPhoto = false; // skip "forget" photos
            if (showFavoritesOnly && googleSheetsForUnity.allPhotos[currentPhoto].favorite == "") isPhoto = false; // skip "forget" photos
        }
        PlayerPrefs.SetInt("currentPhoto", currentPhoto); // Update currentPhoto (in case we crash!)



        if (loadedPhotoTextures[currentPhoto]) // if photo already downloaded...
        {
            PhotoRawImage.GetComponent<RawImage>().texture = loadedPhotoTextures[currentPhoto];
            PhotoRawImage.GetComponent<RawImage>().SizeToParent(); // see CanvasExtensions.cs for this code
            SetTextFields();
            if (infoPanelsOpen) InfoPanel.transform.gameObject.SetActive(true); // show the InfoPanel again...
            CacheNextPhoto(latestIncrement); // also loads the "next" photo (given currentPhoto and latest increment)
        }
        else
        {
            LoadingPauseButton.transform.gameObject.SetActive(true); // freeze screen while loading image
            GetPhotoFromServer(PhotoName[currentPhoto]); // get from Dropbox
        }

        // reset Photo scale to 1.0 (user may have resized with Lean Pinch earlier)
        Vector3 newScale = transform.localScale;
        newScale *= 1.0f;
        PhotoRawImage.transform.localScale = newScale;
        // reset Photo transform location (if user had used Lean Translate to move image)
        var imageTransform = PhotoRawImage.GetComponent<RectTransform>();
        //var imagePivotX = imageTransform.pivot.x;
        var imageWidth = imageTransform.rect.width;
        //var imageAnchorX = imageTransform.anchoredPosition.x;
        //Debug.Log("imagePivotX = " + imagePivotX + ", imageWidth = " + imageWidth + ", imageAnchorX = " + imageAnchorX);

        imageTransform.anchoredPosition = new Vector2(imageWidth / 2.0f, 0.0f); //ensures photo is against left side of screen


        // GET audio file if needed
        basicAudio.currentAsrc.clip = null; // clear out audio clip from memory

        if (googleSheetsForUnity.allPhotos[currentPhoto].audio_file_path == "") // does it have an audio file associated?
        {
            //VoiceNoteButton.GetComponent<Image>().sprite = microphoneIcon;
            basicAudio.playButton.GetComponent<Button>().interactable = false;
            basicAudio.deleteButton.interactable = false;
            basicAudio.saveButton.interactable = false;

        }
        else
        {
            // TODO: Download audio_file into a new loadedAudioFiles[currentPhoto]
            if (loadedAudioClips[currentPhoto] == null) // if we do not already have a cached copy of the audio...
            {
                GetAudioFileFromServer(googleSheetsForUnity.allPhotos[currentPhoto].audio_file_path); // will get audio_file and put in loadedAudioClips
            }
            else
            {
                VoiceNoteButton.GetComponent<Image>().sprite = speakerIcon;
                basicAudio.playButton.GetComponent<Button>().interactable = true;
                basicAudio.deleteButton.interactable = true;
                basicAudio.saveButton.interactable = false;
                basicAudio.currentAsrc.clip = loadedAudioClips[currentPhoto];
            }
        }
        isBusyLoading = false;

    }

    public Texture GetPhotoFromServer(string fileName)
    {
        DateTime startTime = DateTime.Now;
        // from DropboxSync MultipleFileTypesExampleScript
        PhotoRawImage.transform.gameObject.SetActive(false); // hide photo while loading
        DropboxSync.Main.GetFile<Texture2D>(fileName, new Progress<TransferProgressReport>((progress) => { }),
        (tex) =>
        {

            // Successfully downloaded Photo ("texture") from Dropbox...
            DateTime endTime = DateTime.Now;
            TimeSpan ts = endTime - startTime;
            print("GetPhotoFromServer download duration (ms): " + ts.Milliseconds);
            PhotoRawImage.GetComponent<RawImage>().texture = tex;
            PhotoRawImage.GetComponent<RawImage>().SizeToParent(); // see CanvasExtensions.cs for this code
            loadedPhotoTextures[currentPhoto] = tex; // cache photo for future use
            SetTextFields();
            PhotoRawImageObject.transform.gameObject.SetActive(true); // Show the photo now that it is loaded
            if (infoPanelsOpen) InfoPanel.transform.gameObject.SetActive(true); // show the InfoPanel again...

#if UNITY_EDITOR
            m_PreviousButton.transform.gameObject.SetActive(true);
            m_NextButton.transform.gameObject.SetActive(true);
#endif
            LoadingPauseButton.transform.gameObject.SetActive(false); // unfreeze screen
            CacheNextPhoto(latestIncrement); // also loads the "next" photo (given currentPhoto and latest increment)
        }, (ex) =>
        {
            Debug.LogError($"Error getting picture from Dropbox: {ex}");
            DebugText.text = DebugText.text + "ERROR in GetPhotoFromServer()";
            DebugText.text = DebugText.text + "\nfileName = " + fileName;
            DebugText.text = DebugText.text + "\nException = " + ex;
            Debug.Log(DebugText.text);
            loadingMessageText.text = "...taking a while, but still loading!";
            // ShowRestartDialog(); // TODO: enable this only for certain types of "ex" messages (not on internet?)

        }, receiveUpdates: true, useCachedFirst: true, useCachedIfOffline: true);

        return PhotoRawImageObject.texture; // TODO: don't need to return this texture: delete?
    }

    void CacheNextPhoto(int increment) // pre-load the next photo in the line (given latest increment and currentPhoto)
    {
        if (increment == 0) increment = +1; // assume user will load "next" photo if not otherwise specified
        int nextPhotoInt = currentPhoto;
        bool nextIsPhoto = false;
        while (!nextIsPhoto)
        { // skips over non-photo (e.g. video and audio) files
            nextPhotoInt += increment;
            if (nextPhotoInt > PhotoName.Count - 1) nextPhotoInt = 0;
            if (nextPhotoInt < 0) nextPhotoInt = PhotoName.Count - 1;
            nextIsPhoto = true;
            if (!isValidPhotoFile(googleSheetsForUnity.allPhotos[nextPhotoInt].filename)) nextIsPhoto = false;
            if (googleSheetsForUnity.allPhotos[nextPhotoInt].forget_photo != "") nextIsPhoto = false; // skip "forget" photos
        }

        //Debug.Log("In CacheNextPhoto: currentPhoto = " + currentPhoto + " nextPhotoInt = " + nextPhotoInt);

        if (loadedPhotoTextures[nextPhotoInt]) return; // if next photo is already cached, skip this
        DateTime startTime = DateTime.Now;

        DropboxSync.Main.GetFile<Texture2D>(PhotoName[nextPhotoInt], new Progress<TransferProgressReport>((progress) => { }),
        (tex) =>
        {
            loadedPhotoTextures[nextPhotoInt] = tex; // cache photo for future use
            DateTime endTime = DateTime.Now;
            TimeSpan ts = endTime - startTime;
            print("CacheNextPhoto download duration (ms): " + ts.Milliseconds);
        }, (ex) =>
        {
            Debug.LogError($"CacheNextPhoto: Error getting picture from Dropbox: {ex}");
        }, receiveUpdates: true, useCachedFirst: true, useCachedIfOffline: true);
        return;
    }

    private async void GetAudioFileFromServer(string audio_file_path)
    {
        // TODO: Colorize Play button
        var newColorBlock = basicAudio.playButton.GetComponent<Button>().colors;
        newColorBlock.disabledColor = Color.green;
        basicAudio.playButton.GetComponent<Button>().colors = newColorBlock;
        Image infoIcon = infoToggleIcon.GetComponent<Image>();
        infoIcon.color = Color.green; // make "i" green for images with audio files


        try
        {
            var localPath = await DropboxSync.Main.GetFileAsLocalCachedPathAsync(audio_file_path,
                                new Progress<TransferProgressReport>((report) =>
                                {
                                    Debug.Log($"Downloading: {report.progress}% {report.bytesPerSecondFormatted}");
                                }), _cancellationTokenSource.Token);

            // Succeeded in downloading audio file
            Debug.Log("Retrieving audio file - in GetAudioFileFromServer: Local path: " + localPath);
            await LoadAudio(localPath); // will get the audio file and load it into loadedAudioClips[currentPhoto]
            basicAudio.playButton.GetComponent<Button>().colors = basicAudio.savedColorBlock;

        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                //Debug.Log("Download cancelled");
                //Debug.Log("<color=orange>Download canceled.</color>");
                DebugText.text = DebugText.text + " Download was cancelled.";
                basicAudio.deleteButton.interactable = false;
                basicAudio.saveButton.interactable = false;
            }
            else
            {
                DebugText.text = DebugText.text + "ERROR in GetAudioFileFromServer()\n";
                DebugText.text = DebugText.text + "audio_file_path = " + audio_file_path;
                DebugText.text = DebugText.text + " exception = " + ex;
                Debug.Log(DebugText.text);
                basicAudio.deleteButton.interactable = false;
                basicAudio.saveButton.interactable = false;
                //ShowRestartDialog();
            }
            basicAudio.playButton.GetComponent<Button>().colors = basicAudio.savedColorBlock;
        }
    }

    public void LoadingMessageWait()
    {
        loadingMessageText.text = "... still loading!<color=blue>\n(tap screen 4 times for restart options)</color>";
    }

    IEnumerator LoadAudio(string localFilePath)
    {
        AudioClip audioClip = null;

        using (UnityWebRequest loader = UnityWebRequestMultimedia.GetAudioClip("file://" + localFilePath, AudioType.WAV))
        {
            yield return loader.SendWebRequest();

            if (string.IsNullOrEmpty(loader.error))
            {
                audioClip = DownloadHandlerAudioClip.GetContent(loader);
                basicAudio.currentAsrc.clip = audioClip;
                basicAudio.currentAsrc.clip.name = "downloaded audio";
                loadedAudioClips[currentPhoto] = audioClip; // add this audioClip to the array loadedAudioClips
                VoiceNoteButton.GetComponent<Image>().sprite = speakerIcon;
                basicAudio.playButton.GetComponent<Button>().interactable = true;
                basicAudio.deleteButton.interactable = true;
                basicAudio.saveButton.interactable = false; // no need to save an already-existing recording

            }
            else
            {
                // Error loading audio
                DebugText.text = DebugText.text + "LoadAudio() error.\n";
                DebugText.text = DebugText.text + "localFilePath = " + localFilePath;
                DebugText.text = DebugText.text + ", loader.uri error = " + loader.uri + ", " + loader.error;
                Debug.Log(DebugText.text);
                ShowRestartDialog();
            }
        }
    }

    //void ClearTextFields() // Not used: this might cause fields to get updated because OnValueChanged
    //{
    //    settingUpTextFields = true; // keeps autocomplete from trying to run while setting up text fields
    //    Text LocationInputText = LocationInput.transform.Find("Text").GetComponent<Text>();
    //    Text CommentsInputText = CommentsInput.transform.Find("Text").GetComponent<Text>();
    //    LocationInput.text = "";
    //    LocationText.text = "";
    //    CommentsInput.text = "";
    //    CommentsText.text = "";
    //}

    void SetTextFields() // set up text (and photo colors and VoiceNoteButtons) based on currentPhoto info
    {
        settingUpTextFields = true; // keeps autocomplete from trying to run while still setting up text fields (i.e. ignore onValueChanged)

        Text LocationInputText = LocationInput.transform.Find("Text").GetComponent<Text>();
        Text CommentsInputText = CommentsInput.transform.Find("Text").GetComponent<Text>();
        //fitMyChild.ResetToOriginalParentHeight();

        if (googleSheetsForUnity.allPhotos[currentPhoto].location.Length == 0) // if location data empty
        {
            LocationText.text = "location";
            LocationText.fontStyle = FontStyle.Italic;
            LocationText.color = lightBlue;
            LocationInput.text = "";
            LocationInputText.color = lightBlue;
        }
        else
        {
            LocationText.text = LocationInput.text = googleSheetsForUnity.allPhotos[currentPhoto].location;
            LocationText.fontStyle = FontStyle.Normal;
            LocationText.color = Color.white;
            LocationInputText.color = Color.white;
        }

        if (googleSheetsForUnity.allPhotos[currentPhoto].comments.Length == 0)
        {
            CommentsText.text = "comments";
            CommentsText.fontStyle = FontStyle.Italic;
            CommentsText.color = lightBlue;
            CommentsInput.text = "";
            CommentsInputText.color = lightBlue;

        }
        else
        {
            CommentsText.text = CommentsInput.text = googleSheetsForUnity.allPhotos[currentPhoto].comments;
            CommentsText.fontStyle = FontStyle.Normal;
            CommentsText.color = Color.white;
            CommentsInputText.color = Color.white;
            // TODO: Error here -- CommentsText.text seems to be "showing through".  Hide LocationAndDateText maybe?

        }

        if (googleSheetsForUnity.allPhotos[currentPhoto].date == "") // if no date...
        {
            LocationAndDateEditor.transform.gameObject.SetActive(true); // show editor
            LocationAndDateText.transform.gameObject.SetActive(false); // hide raw text
            fitMyChild.isInputCurrently = true; // ensures box is resized based on correct comment field
            MonthInput.text = DayInput.text = YearInput.text = "";
            DateText.text = "date";
            DateText.color = lightBlue;

        }
        else // if there is a date already entered (i.e. likely reviewed)...
        {
            //Debug.Log("googleSheetsForUnity.allPhotos[currentPhoto].date = " + googleSheetsForUnity.allPhotos[currentPhoto].date);
            //DateTime tempDate = Convert.ToDateTime(googleSheetsForUnity.allPhotos[currentPhoto].date);
            //MonthInput.text = tempDate.Month.ToString();
            //DayInput.text = tempDate.Day.ToString();
            //YearInput.text = tempDate.Year.ToString();

            string tempDate = googleSheetsForUnity.allPhotos[currentPhoto].date;
            //Debug.Log("tempDate = " + tempDate);

            string[] words = tempDate.Split('/');
            if (words.Length != 3)
            {
                Debug.Log("ERROR: Bad date string format in SetTextFields. tempDate = " + tempDate);
                MonthInput.text = "0";
                DayInput.text = "0";
                YearInput.text = "1900";
            }

            MonthInput.text = words[0];
            DayInput.text = words[1];
            YearInput.text = words[2];
            DateText.text = MonthInput.text + "/" + DayInput.text + "/" + YearInput.text;
            if (MonthInput.text == "0" && DayInput.text == "0")
            {
                DateText.text = YearInput.text; // Assume 0/0 date is for the year only
            }
            else
            {
                if (DayInput.text == "0") // Show in form "mmm yyyy"
                {
                    DateText.text = MonthAbbreviation(MonthInput.text) + " " + YearInput.text; // TODO: Assume 0/0 date is for the year
                }
            }

            LocationAndDateEditor.transform.gameObject.SetActive(false); // hide editor
            LocationAndDateText.transform.gameObject.SetActive(true); // show raw text
            fitMyChild.isInputCurrently = false; // ensures box is resized based on correct comment field

            DateText.color = Color.white;
        }

        // Set heart (favorite) icon based on "favorite"...
        if (googleSheetsForUnity.allPhotos[currentPhoto].favorite == "") // if not a favorite...
        {
            favoritePhotoToggle.isOn = false;
        }
        else
        {
            favoritePhotoToggle.isOn = true;
        }

        // Set "x" ("forget this photo") icon and tint of Photo based on "forget_photo"...
        if (googleSheetsForUnity.allPhotos[currentPhoto].forget_photo == "") // if not a photo-to-forget
        {
            forgetPhotoToggle.isOn = false;
            PhotoRawImage.GetComponent<RawImage>().color = Color.white;

        }
        else
        {
            forgetPhotoToggle.isOn = true;
            PhotoRawImage.GetComponent<RawImage>().color = darkRed;
        }

        // TODO: Set the backgroundPanelHeight if comment text spills over

        settingUpTextFields = false; // setup is complete

    }

    public string MonthAbbreviation(string monthInputText)
    {
        string[] monthNames = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "June", "July", "Aug", "Sep", "Oct", "Nov", "Dec" };
        int i = Convert.ToInt32(monthInputText);
        return monthNames[i - 1];
    }

    public void EditedLocation()
    {
        // TODO: Test for valid location
        googleSheetsForUnity.allPhotos[currentPhoto].location = LocationInput.text;
        UpdateGoogleSheetRow(currentPhoto, "location", LocationInput.text);
    }

    public void EditedComments()
    {
        googleSheetsForUnity.allPhotos[currentPhoto].comments = CommentsInput.text;
        UpdateGoogleSheetRow(currentPhoto, "comments", CommentsInput.text);
    }

    public void EditedMonth()
    {
        if (MonthInput.text == "") MonthInput.text = "0";
        if (MonthInput.text == "0") DayInput.text = "0"; // assume unknown month has an unknown day too!
        if (IsValidDateValue("month", MonthInput, 0, 12) && DayInput.text != null) EditedYear();
    }

    public void EditedDay()
    {
        if (DayInput.text == "") DayInput.text = "0";
        if (IsValidDateValue("day", DayInput, 0, 31) && MonthInput.text != null) EditedYear();
    }

    private bool IsValidDateValue(string fieldType, InputField inputObject, int valueMin, int valueMax) // Confirm value is within min/max range
    {
        int valueCheck = int.Parse(inputObject.text);
        if (valueCheck < valueMin || valueCheck > valueMax)
        {
            ErrorMessage("Please enter a " + fieldType + " value between " + valueMin + " and " + valueMax + ".");
            inputObject.text = "";
            return false;
        }
        return true;
    }

    public void EditedYear()
    {
        if (YearInput.text == "" || YearInput.text == null) return; // ignore blank entries
        // Confirm that year value is within min/max range
        int yearTemp = int.Parse(YearInput.text);
        if (yearTemp >= 50 && yearTemp <= 99)
        {
            yearTemp = yearTemp + 1900; // correct for 2-digit years 1950-1999
            YearInput.text = yearTemp.ToString();
        }
        if (yearTemp >= 1 && yearTemp <= 20)
        {
            yearTemp = yearTemp + 2000; // correct for 2-digit years 2001-2020
            YearInput.text = yearTemp.ToString();
        }
        if (yearTemp < YearMin || yearTemp > YearMax)
        {
            ErrorMessage("Please enter a 4-digit year between " + YearMin + " and " + YearMax + ".");
            YearInput.text = "";
            return;
        }

        // If month or day are blank, set to "0" for "unknown" (i.e. some users will only know the Year for a photo)
        if (MonthInput.text == "") MonthInput.text = "0";
        if (DayInput.text == "") DayInput.text = "0";

        // Test for valid day value for the indicated month. If not valid, reject Day value (since it must be the problem!)
        int monthTemp = int.Parse(MonthInput.text);
        int dayTemp = int.Parse(DayInput.text);
        if (((monthTemp == 2) && (dayTemp > 28)) ||
           (((monthTemp == 4) || (monthTemp == 6) || (monthTemp == 9) || (monthTemp == 11)) && (dayTemp > 30)))
        {
            ErrorMessage("Please re-enter day value: not valid for the month indicated.");
            DayInput.text = "";
            return;
        }

        // And since all 3 date fields are now valid, go ahead and update date...
        string newDate = MonthInput.text + "/" + DayInput.text + "/" + YearInput.text;
        Debug.Log("In EditedYear: newDate = " + newDate); // TODO: Test date entry tabbing
        googleSheetsForUnity.allPhotos[currentPhoto].date = newDate;
        //UpdateGoogleSheetRow("date", "'" + newDate);
        UpdateGoogleSheetRow(currentPhoto, "date", newDate);
    }

    void ErrorMessage(string errorMessageString)
    {
        ErrorMessageText.text = errorMessageString;
        ErrorMessagePanel.transform.gameObject.SetActive(true);
    }

    public void UpdateGoogleSheetRow(int currentPhotoTemp, string columnName, string updatedValue)
    {
        googleSheetsForUnity.UpdatePhotoInfo(currentPhotoTemp, columnName, updatedValue); // Update the currentPhoto data in column "columnName"
    }

    void MyOrientationChangeCode(DeviceOrientation orientation)
    {
        PhotoRawImage.GetComponent<RawImage>().SizeToParent(); // Resize image on screen orientation change
    }

    public bool isValidPhotoFile(string fileName)
    {
        string ext = Path.GetExtension(fileName).ToLower().Substring(1); // get lowercase version of extension without "."
        if (listOfValidPhotoExtensions.Contains(ext)) return true;
        return false;
    }


    void ClearPersistentDataDirectories() // TODO: Clear everything except userName
    {
        foreach (var directory in Directory.GetDirectories(Application.persistentDataPath))
        {
            DirectoryInfo data_dir = new DirectoryInfo(directory);
            data_dir.Delete(true);
        }

        foreach (var file in Directory.GetFiles(Application.persistentDataPath))
        {
            FileInfo file_info = new FileInfo(file);
            file_info.Delete();
        }

        Debug.Log("CLEARED all persistent directories.");

    }

    public void ToggleInfoPanel()
    {
        infoPanelsOpen = !infoPanelsOpen;
        InfoPanel.transform.gameObject.SetActive(infoPanelsOpen); // Visibility of infoPanel = infoPanelsOpen
        Image infoIcon = infoToggleIcon.GetComponent<Image>();
        if (infoPanelsOpen)
        {
            infoIcon.color = Color.white;
        }
        else
        {
            infoIcon.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
        }

        if (googleSheetsForUnity.allPhotos[currentPhoto].audio_file_path != "") infoIcon.color = Color.green;

    }


    void BuildLocationsList()
    {
        uniqueLocationsList = googleSheetsForUnity.allPhotos.Select(l => l.location).Distinct().ToList();
        uniqueLocationsList.RemoveAll(item => item == "");
        uniqueLocationsList.Sort(); // TODO: Sort not alphabetically, but by popularity
        autoCompleteInput.SpawnClickableOptions(uniqueLocationsList);

    }

    public void ShowRestartDialog()
    {
        currentPhotoText.text = "photo #" + currentPhoto + "\n" + googleSheetsForUnity.allPhotos[currentPhoto].filename;
        RestartDialog.transform.gameObject.SetActive(true);
    }

    public void DebugShowRestartDialog()
    {
        currentPhotoText.text = "photo #" + currentPhoto + "\n" + googleSheetsForUnity.allPhotos[currentPhoto].filename;
        DebugText.transform.gameObject.SetActive(true); // show latest error messages
        RestartDialog.transform.gameObject.SetActive(true);
    }

    public void TryPhotoReload()
    {
        NewPhoto(0);
    }

    public void RestartApp()
    {
        //SceneManager.LoadScene(1); // Scene "1" is the second scene in the Build Settings: reloadScene
        Application.Quit();
    }

    public void ClearPersistentDataAndQuit()
    {
        ClearPersistentDataDirectories(); // Clears persistent photos, etc.
        PlayerPrefs.SetString("editorName", "Tester"); // Clear editorName for debug testing
        PlayerPrefs.SetInt("currentPhoto", 0); // Set currentPhoto back to 0
        PlayerPrefs.SetString("showFavoritesOnly", "");
        PlayerPrefs.SetString("hideForgottenPhotos", "");

        Application.Quit();
    }

    public void ClearCurrentPhoto()
    {
        currentPhoto = 0;
        PlayerPrefs.SetInt("currentPhoto", 0); // Set currentPhoto back to 0
        NewPhoto(0); // reload currentPhoto
    }

    public void FavoritePhotoToggleClicked()
    {
        int currentPhotoTemp = currentPhoto;
        // Save setting to Google Sheets and allPhotos
        favoritePhotoToggleState.isOn = !favoritePhotoToggleState.isOn; // toggle heart state graphic
        string favoriteString = "";
        if (favoritePhotoToggleState.isOn)
        {
            favoriteString = "yes";
            forgetPhotoToggleState.isOn = false; // turn off "forget" if planning to favorite this photo
        }
        PhotoRawImage.GetComponent<RawImage>().color = Color.white;

        googleSheetsForUnity.allPhotos[currentPhoto].favorite = favoriteString;
        UpdateGoogleSheetRow(currentPhotoTemp, "favorite", favoriteString); // TODO: Confirm that this will save BOTH toggle items

        // TODO: Set these values when the row is downloaded!
    }

    public void ForgetPhotoToggleClicked()
    {
        int currentPhotoTemp = currentPhoto;
        // Save setting to Google Sheets and allPhotos
        forgetPhotoToggleState.isOn = !forgetPhotoToggleState.isOn; // toggle trash can state graphic

        string forget_photoString = "";
        if (forgetPhotoToggleState.isOn)
        {
            forget_photoString = "x";
            // Set RawImage to dark
            PhotoRawImage.GetComponent<RawImage>().color = darkRed;
            favoritePhotoToggleState.isOn = false; // turn off "favorite" if planning to forget this photo
            googleSheetsForUnity.allPhotos[currentPhoto].favorite = "";
        }
        else
        {
            PhotoRawImage.GetComponent<RawImage>().color = Color.white;
        }
        UpdateGoogleSheetRow(currentPhotoTemp, "forget_photo", forget_photoString); // TODO: Confirm that this will save BOTH toggle items
        googleSheetsForUnity.allPhotos[currentPhoto].forget_photo = forget_photoString;

    }

    // ***** Dropbox Upload

    public async void UploadDataFile() // TODO: Save entire allPhotos array as a JSON file?
    {
        // Dropbox Start Items below (from AsyncUploadFileExampleScript for Dropbox upload)
        string fileName = "all_photos_data.csv";
        string filePath = Application.persistentDataPath + "/" + fileName;

        //// builds a copy of current photos[] array and saves it as fileName
        //FileStream fileStream;
        //fileStream = new FileStream(filePath, FileMode.Create);
        //fileStream.WriteByte(emptyByte);
        //fileStream.Close();

        // googleSheetsForUnity.allPhotos[i].dropbox_path

        var allPhotosArray = googleSheetsForUnity.allPhotos.ToArray();
        string jsonData = JsonUtility.ToJson(allPhotosArray);
        File.WriteAllText(filePath, jsonData);


        // Prepare file path for uploading.  Assuming file exists and is valid!
        //string localFileLocation = Application.persistentDataPath + "/" + fileName;
        string _uploadDropboxPath = Path.Combine(dropBoxDataFolder, Path.GetFileName(filePath));
        _cancellationTokenSource = new CancellationTokenSource();
        var localFilePath = filePath;

        try
        {
            var metadata = await DropboxSync.Main.UploadFileAsync(localFilePath, _uploadDropboxPath, new Progress<TransferProgressReport>((report) =>
            {
                if (Application.isPlaying)
                {
                    //Debug.Log("Uploading file " + report.progress + report.bytesPerSecondFormatted);
                }
            }), _cancellationTokenSource.Token);

            Debug.Log("Uploaded DATA file.  metadata.id = " + metadata.id);
            Debug.Log("in UploadFile, fileName = " + fileName);
            //saveButton.GetComponent<Animator>().SetBool("isActive", false); // stop animation of Save button
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                Debug.Log("Upload cancelled");
            }
            else
            {
                Debug.LogException(ex);
            }
        }
    }

    public void PlaySlideShow()
    {
        playingSlideShow = false;
        basicAudio.PauseAllMedia(); // stop any playing or recording now in progress
        BuildSlideShow();
        Debug.Log("Total slides available = " + slideCount);
        if (slideCount == 0)
        {
            ErrorMessageText.text = "No voice notes available for slide show.  Try adding voice notes to a few photos first.";
            ErrorMessagePanel.transform.gameObject.SetActive(true);
            return;
        }
        playingSlideShow = true;
        InfoPanel.transform.gameObject.SetActive(false);
        // hide the next/previous buttons
            m_PreviousButton.transform.gameObject.SetActive(false);
            m_NextButton.transform.gameObject.SetActive(false);
        PhotoRawImageObject.transform.gameObject.SetActive(false); // Hide the photo
        SlideShowImage.parent.transform.gameObject.SetActive(true); // Show the slide show panel

        currentSlide = currentPhoto;
        GetNextSlide();
    }

    private void BuildSlideShow() // starts at nextSlideShowPhoto
    {
        slideCount = 0;
        for (int i = 0; i < googleSheetsForUnity.allPhotos.Length; i++) //Loop through entire photo collection
        {
            if (googleSheetsForUnity.allPhotos[i].audio_file_path != "") slideCount++;
        }
        return;
    }

    private void GetNextSlide()
    {
        while (googleSheetsForUnity.allPhotos[currentSlide].audio_file_path == "")
        { // skips over non-video-note slides
            currentSlide++;
            if (currentSlide > googleSheetsForUnity.allPhotos.Length - 1) currentSlide = 0;
            if (currentSlide < 0) currentSlide = googleSheetsForUnity.allPhotos.Length - 1;
        }
        PlaySlide();
    }

    private void PlaySlide()
    {
        Debug.Log("PlaySlide, currentSlide = " + currentSlide);
        Debug.Log("currentAsrc.clip = " + slideShowAsrc.clip);
        Debug.Log("loadedAudioClips.Length = " + loadedAudioClips.Length);

        SlideShowImage.GetComponent<RawImage>().texture = loadedPhotoTextures[currentSlide];
        SlideShowImage.GetComponent<RawImage>().SizeToParent(); // see CanvasExtensions.cs for this code
        slideShowAsrc.clip = loadedAudioClips[currentSlide];
        slideShowAsrc.Play();
    }

    private void MainMenuClicked()
    {
        Debug.Log("Clicked MainMenu Button.  mainMenuIsOpen = " + mainMenuIsOpen);
        mainMenuIsOpen = !mainMenuIsOpen;
        MainMenuPanel.transform.gameObject.SetActive(mainMenuIsOpen); // Toggle the Main Menu Items

    }

    public void ChangeShowFavoritesOnly()
    {
        //Make sure there are some favorite photos (otherwise infinite loop!). Ignore request if not valid.
            int totalFavorites = 0;
            for (int i = 0; i < googleSheetsForUnity.allPhotos.Length; i++)
            {
                if (googleSheetsForUnity.allPhotos[i].favorite != "") totalFavorites++;
            }
        //Debug.Log("In ChangeShowFavoritesOnly, totalFavorites = " + totalFavorites);
            if (totalFavorites == 0) return;

        showFavoritesOnly = !showFavoritesOnly;
        favoritesChecked.transform.gameObject.SetActive(showFavoritesOnly);
        string favoritesOnly = "";
        if (showFavoritesOnly) favoritesOnly = "yes";
        PlayerPrefs.SetString("showFavoritesOnly", favoritesOnly);
        if (googleSheetsForUnity.allPhotos[currentPhoto].favorite == "") NewPhoto(1); // if current photo is not a "favorite", find next photo
    }

    public void ChangeHideForgottenPhotos()
    {
        //Make sure there are some non-forgotten photos (otherwise infinite loop!). Ignore request if not valid.
            int totalNonForgotten = 0;
            for (int i = 0; i < googleSheetsForUnity.allPhotos.Length; i++)
            {
                if (googleSheetsForUnity.allPhotos[i].forget_photo == "") totalNonForgotten++;
            }
        Debug.Log("In ChangeHideForgottenPhotos, totalNonForgotten = " + totalNonForgotten);

        if (totalNonForgotten == 0) return;
        hideForgottenPhotos = !hideForgottenPhotos;
        forgottenChecked.transform.gameObject.SetActive(hideForgottenPhotos);
        string hideForgotten = "";
        if (hideForgottenPhotos) hideForgotten = "yes";
        PlayerPrefs.SetString("hideForgottenPhotos", hideForgotten);
        if (googleSheetsForUnity.allPhotos[currentPhoto].forget_photo != "") NewPhoto(1); // if current photo is "forgotten", find next photo

    }

    public int TimeElapsedMilliseconds(DateTime startTime)
    {
        DateTime endTime = DateTime.Now;
        TimeSpan ts = endTime - startTime;
        print("Since last login, time elapsed (ms): " + ts.Milliseconds);
        return ts.Milliseconds;
    }

}
