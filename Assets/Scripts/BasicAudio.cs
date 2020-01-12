using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DBXSync; // Dropbox Sync

public class BasicAudio : MonoBehaviour
{
    // PUBLIC Inspector items
    public AudioSource currentAsrc;
    public Button recordNewButton;
    public GameObject playButton;
    public GameObject popUp;
    public Button deleteButton;
    public Button saveButton;
    public Text DebugText;
    public string dropBoxAudioFolder;


    // Hide the following (currently) unused "Advanced Editing" items from Inspector
    [HideInInspector]
        public Slider rightcropSli, leftcropSli, playbackSli;
        [HideInInspector]
        public Dropdown recordDropdown;
        [HideInInspector]
        public Button cancelUploadButton;
        [HideInInspector]
        public GameObject trimButton;
        [HideInInspector]
        public WaveFormDraw wfDraw;
    [HideInInspector]
    public ColorBlock savedColorBlock;


    // private variables
    private bool isRecording = false;
    private bool haveNewRecordingToSave = false;
    private int recordNum = 0;
    //private List<AudioClip> myClips = new List<AudioClip>();
    public AudioClip currentMyClip;
    //[HideInInspector]
    public bool isplaying = false;
    private bool PlayHeadTouch;
    private PhotoReview photoReview; // Access variables and scripts in PhotoReview.cs
    private GoogleSheetsForUnity.GoogleSheetAccess googleSheetsForUnity; // Access variables and scripts in GoogleSheetAccess.cs

    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private string _uploadDropboxPath;
    private string _deleteDropboxPath;

    private void Start()
    {
        photoReview = GetComponent<PhotoReview>();
        googleSheetsForUnity = GetComponent<GoogleSheetsForUnity.GoogleSheetAccess>();

        playButton.GetComponent<Button>().interactable = false;
        playButton.GetComponent<Button>().onClick.AddListener(PlayStopRecording);
        saveButton.onClick.AddListener(SaveAudioClip);
        saveButton.interactable = false;
        savedColorBlock = playButton.GetComponent<Button>().colors;

        recordNewButton.onClick.AddListener(RecordNewAudio);
        haveNewRecordingToSave = false;

        recordNewButton.GetComponent<Animator>().SetBool("isActive", false);

        deleteButton.interactable = false;
        cancelUploadButton.onClick.AddListener(() => {
            _cancellationTokenSource.Cancel();
        });

        RARE.Instance.OutputVolume(0.6f); // TODO: Confirm setting is optimal (vs. 1.0f)

    }

    void Update()
    {
        if (currentAsrc.clip == null) return;

        if (currentAsrc.isPlaying == false) playButton.GetComponent<Animator>().SetBool("isActive", false);
        return;
    }

    public void RecordNewAudio()
    {
        string currentPhotoFileName = googleSheetsForUnity.allPhotos[photoReview.currentPhoto].filename;
        int findExtensionStart = currentPhotoFileName.Length;
        if (currentPhotoFileName.Contains(".")) {
            findExtensionStart = currentPhotoFileName.IndexOf(".", StringComparison.CurrentCulture);
        }
        currentPhotoFileName = currentPhotoFileName.Substring(0, findExtensionStart);
        Debug.Log("In RecordNewAudio(), currentPhotoFileName = " + currentPhotoFileName);

        if (!isRecording) // START recording...
        { 
            if (currentAsrc.isPlaying) PlayStopRecording();

            playButton.GetComponentInChildren<Button>().interactable = false;
            saveButton.interactable = false;
            deleteButton.interactable = false;
            isRecording = true;
            RARE.Instance.StartMicRecording(599);
            recordNewButton.GetComponent<Animator>().SetBool("isActive", true);
            Debug.Log("Started Recording...");

        }
        else // STOP recording (if was already in progress)
        { 
            playButton.GetComponent<Button>().interactable = true;
            RARE.Instance.StopMicRecording(currentPhotoFileName, ClipLoaded, popUp);
            recordNum++;
            isRecording = false;
            recordNewButton.GetComponent<Animator>().SetBool("isActive", false);
            deleteButton.interactable = true;
            saveButton.interactable = true;
            haveNewRecordingToSave = true;
            photoReview.loadedAudioClips[photoReview.currentPhoto] = currentAsrc.clip;
        }

    }

    public void PlayStopRecording() // Pause/Play button
    {
        if (currentAsrc.isPlaying) // if currentAsrc already playing, stop it
        {
            currentAsrc.Pause();
            isplaying = false;
            playButton.GetComponent<Animator>().SetBool("isActive", false);
        }
        else // if paused, then Play currentAsrc
        {
            // newly added
            playButton.GetComponent<Button>().interactable = true;
            currentAsrc.Play();
            isplaying = true;
            playButton.GetComponent<Animator>().SetBool("isActive", true);
        }
    }

    public void PauseAllMedia()
    {
        currentAsrc.Pause();
        photoReview.loadedAudioClips[photoReview.currentPhoto] = null;

        deleteButton.GetComponent<Animator>().SetBool("isActive", false); // stop any animation of Delete button
        playButton.GetComponent<Animator>().SetBool("isActive", false); // stop any animation of Play button
        playButton.GetComponent<Button>().colors = savedColorBlock;

        recordNewButton.GetComponent<Animator>().SetBool("isActive", false); // stop any animation of Record button
        saveButton.GetComponent<Animator>().SetBool("isActive", false);
        saveButton.GetComponentInChildren<Text>().text = "";


        deleteButton.interactable = false;
        saveButton.interactable = false;
        isplaying = false;
        isRecording = false;
        currentAsrc.clip = null;
        haveNewRecordingToSave = false;

        // TODO: need to stop any in-progress Microphone recording in RARE
        //RARE.Instance.StopMicRecording("deleteMe");
        //RARE.Instance.StopMicRecording("deleteMe", ClipLoaded, popUp);
    }

    public void ClipLoaded(AudioClip myClip, string clipName = null) // This routine is called by RARE.Instance.StopMicRecording when done
    {
        if (clipName != null) myClip.name = clipName;
            else myClip.name = "untitled";

        Debug.Log("in ClipLoaded(), MyClip.name = " + myClip.name);
        currentMyClip = myClip;

        //we need to shut off recording if a recording session is in progress
        if (isRecording == true && recordNewButton.interactable == true) RecordNewAudio(); //TODO: Why is this called here AFTER a recording?
        currentAsrc.Stop();
        playButton.SetActive(true);
        currentAsrc.clip = currentMyClip;
        Debug.Log("in ClipLoaded() at END, currentAsrc.clip.name = " + currentAsrc.clip.name);
        SaveAudioClip(); // auto-saving audio clip
    }


    // *** DELETE called by deleteButton

    public void DeleteClip()
    {
        Debug.Log("at DeleteClip, currentAsrc.clip = " + currentAsrc.clip);
        currentAsrc.Pause();
        deleteButton.GetComponent<Animator>().SetBool("isActive", false); // stop any animation of Delete button
        playButton.GetComponent<Animator>().SetBool("isActive", false); // stop any animation of Play button
        recordNewButton.GetComponent<Animator>().SetBool("isActive", false); // stop any animation of Record button

        if (currentAsrc.clip != null)
        {
            _deleteDropboxPath = googleSheetsForUnity.allPhotos[photoReview.currentPhoto].audio_file_path;
            _uploadDropboxPath = "";
            isRecording = false;
            //deleteButton.GetComponent<Animator>().SetBool("isActive", true); // start animation of Delete button

            if (haveNewRecordingToSave)
            {
                // delete from memory only (no need to delete from Dropbox or GoogleSheets)
                Debug.Log("at DeleteClip, haveNewRecordingToSave, so local... ");
                currentAsrc.clip = null;
                deleteButton.interactable = false;
                saveButton.interactable = false;
                playButton.GetComponentInChildren<Button>().interactable = false;
                
                photoReview.loadedAudioClips[photoReview.currentPhoto] = null; // delete from list of cached audio clips too
                haveNewRecordingToSave = false; // no local audio file anymore
            }
            else
            {
                DeleteFile();
            }
            
        }
    }

    async void DeleteFile()
    {
        int currentPhotoTemp = photoReview.currentPhoto;
        deleteButton.interactable = false;
        saveButton.interactable = false;
        playButton.GetComponentInChildren<Button>().interactable = false;

        DropboxSync.Main.Delete(_deleteDropboxPath, (ex) => {
            // Successful deletion of Dropbox audio file
            Debug.Log("Deleted clip _deleteDropboxPath: " + _deleteDropboxPath);
            UpdateGoogleSheet(currentPhotoTemp); // will write blank _uploadDropboxPath into the column "audio_path_name"
            haveNewRecordingToSave = false; // no local audio file anymore
            currentAsrc.clip = null;
        }, (ex) => {
            Debug.Log("Delete from Dropbox failed");
            deleteButton.interactable = true;
            saveButton.interactable = true;
        });
        //deleteButton.GetComponent<Animator>().SetBool("isActive", false); // stop animation of Delete button
    }

    void UpdateGoogleSheet(int currentPhotoTemp) // places _uploadDropboxPath in the "audio_file_path" column;
    {
        string columnName = "audio_file_path";
        //int i = photoReview.currentPhoto;
        int i = currentPhotoTemp;
        googleSheetsForUnity.allPhotos[i].audio_file_path = _uploadDropboxPath;
        googleSheetsForUnity.UpdatePhotoInfo(i, columnName, _uploadDropboxPath); // Update the currentPhoto data in column "columnName"
        if (_uploadDropboxPath == "")
        {
            _uploadDropboxPath = "DELETED";
            deleteButton.interactable = false;
            saveButton.interactable = false;
        }
        else
        {
            deleteButton.interactable = true;
            saveButton.interactable = false;
        }
        Debug.Log("Google Sheet audio-file_path set to " + _uploadDropboxPath);
    }

    // ****** Save the current Audio Clip to Dropbox (saveButton pressed)

    void SaveAudioClip()
    {
        if (haveNewRecordingToSave) // TODO: Is this the best way to see if there is a new sound to save?
        {
            Debug.Log("Closing and Saving clip " + currentAsrc.clip.name);
            UploadFile();
            haveNewRecordingToSave = false;
        }
        saveButton.GetComponent<Animator>().SetBool("isActive", true);
        saveButton.GetComponentInChildren<Text>().text = "Saving...";
    }


    // ***** Dropbox Upload Handling from AsyncUploadFileExampleScript

    async void UploadFile() // TODO: Make this a proper async function?
    {
        // Dropbox Start Items below (from AsyncUploadFileExampleScript for Dropbox upload)

        // Prepare file path for uploading.  Assuming file exists and is valid!
        string localFileLocation = Application.persistentDataPath + "/" + currentAsrc.clip.name + ".wav";
        _uploadDropboxPath = Path.Combine(dropBoxAudioFolder, Path.GetFileName(localFileLocation));
        int currentPhotoTemp = photoReview.currentPhoto;
        _cancellationTokenSource = new CancellationTokenSource();
        var localFilePath = localFileLocation;

        try
        {
            var metadata = await DropboxSync.Main.UploadFileAsync(localFilePath, _uploadDropboxPath, new Progress<TransferProgressReport>((report) => {
                if (Application.isPlaying)
                {
                    //Debug.Log("Uploading file " + report.progress + report.bytesPerSecondFormatted);
                }
            }), _cancellationTokenSource.Token);

            Debug.Log("Uploaded.  metadata.id = " + metadata.id);
            Debug.Log("in UploadFile, currentPhotoTemp = " + currentPhotoTemp + ", photoReview.currentPhoto = " + photoReview.currentPhoto);
            UpdateGoogleSheet(currentPhotoTemp); //TODO: Maybe should still be (photoReview.currentPhoto)
            saveButton.GetComponent<Animator>().SetBool("isActive", false); // stop animation of Save button
            saveButton.GetComponentInChildren<Text>().text = "";

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

    
}
