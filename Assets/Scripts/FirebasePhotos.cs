using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;
using Firebase.Auth;
using Firebase.Storage;
using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.Networking;
using System.IO;

public class FirebasePhotos : MonoBehaviour
{

    public Text photoUID, location;
    public Text statusText;
    public InputField photoUID_Input;
    public RawImage photoRawImage;
    public Texture2D dogTexture;
    public StorageReference storage_ref;
    [HideInInspector]
    public string newFilename;

    private PhotoData data;
    private string spreadsheetID = "BarnettPhotos";
    private string lastStatus = "";




    private string DATA_URL = "https://fir-and-unity-tutorial-d206a.firebaseio.com/"; // insert data URL here

    private DatabaseReference databaseReference;


    void Start()
    {
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl(DATA_URL);
        photoUID_Input.text = "B003523";

        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        lastStatus = "";
        newFilename = "";

        // Get a reference to the storage service, using the default Firebase App
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;

        // Create a storage reference from our storage service
        storage_ref = storage.GetReferenceFromUrl("gs://fir-and-unity-tutorial-d206a.appspot.com/PhotoReviewerTest/Barnett Family Photos - ScanCafe/Barnett Family Photos 1955-2000/1975/");


    }

    private void LateUpdate()
    {
        if (lastStatus == statusText.GetComponent<Text>().text) return;

        statusText.GetComponent<Text>().text = lastStatus;
        lastStatus = statusText.GetComponent<Text>().text;

    }

    public void GetPhotoPath()
    {

        //lastStatus = statusText.GetComponent<Text>().text;
        //photoRawImage.texture = dogTexture;

        FirebaseDatabase.DefaultInstance.GetReferenceFromUrl(DATA_URL).Child(spreadsheetID).Child(photoUID.text).GetValueAsync()
                .ContinueWith((task => {

                    if (task.IsCanceled)
                    {
                        Firebase.FirebaseException e =
                        task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;
                        GetErrorMessage((AuthError)e.ErrorCode);
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        Firebase.FirebaseException e =
                        task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;
                        GetErrorMessage((AuthError)e.ErrorCode);
                        return;
                    }

                    if (task.IsCompleted)
                    {
                        DataSnapshot snapshot = task.Result; //task.Result;
                        string t = snapshot.GetRawJsonValue();
                        PhotoData extractedData = JsonUtility.FromJson<PhotoData>(t);
                        print("Filename is: " + extractedData.Filename);
                        lastStatus = "Filename is: " + extractedData.Filename;
                        newFilename = extractedData.Filename;
                    }
                }));


    }

  public void DownloadPhotoToDevice()
    {
        DateTime startTime = DateTime.Now;
        var filePath = Path.Combine(Application.persistentDataPath, newFilename);
        Debug.Log("downloading from " + filePath);
        Debug.Log("getting file " + newFilename);
        // Start downloading a file
        Task task = storage_ref.Child(newFilename).GetFileAsync(filePath,
          new Firebase.Storage.StorageProgress<DownloadState>((DownloadState state) => {
      // called periodically during the download
      //Debug.Log(String.Format(
      //  "Progress: {0} of {1} bytes transferred.",
      //  state.BytesTransferred,
      //  state.TotalByteCount
      //));
          }), CancellationToken.None);

        task.ContinueWith(resultTask => {
            if (!resultTask.IsFaulted && !resultTask.IsCanceled)
            {
                Debug.Log("Download finished.");
                DateTime endTime = DateTime.Now;
                TimeSpan ts = endTime - startTime;
                print("Download duration (ms): " + ts.Milliseconds);
            }
        });
    }

    public void DisplayPhoto()
    {
        StartCoroutine(LoadTexture());
    }

    IEnumerator LoadTexture()
    {
        var filePath = "file://" + Path.Combine(Application.persistentDataPath, newFilename);
        Debug.Log("filePath = " + filePath);
        UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(filePath);
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError || uwr.isHttpError)
        {
            Debug.Log(uwr.error);
        }
        else
        {
            Texture texture = ((DownloadHandlerTexture)uwr.downloadHandler).texture;
            photoRawImage.texture = texture;
            Debug.Log("Texture should load now.");

        }
    }

    private void DownloadPhotoGetBytesAsync(string Filename)
    {
        storage_ref.GetBytesAsync(10000000).
        ContinueWith((System.Threading.Tasks.Task<byte[]> task) =>
    {
        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.Log(task.Exception.ToString());
            lastStatus = "ERROR: " + task.Exception.ToString();

        }
        else
        {

            byte[] fileContents = task.Result;
            Debug.Log("fileContents.Length: " + fileContents.Length);
            Debug.Log("Got 1");
            Debug.Log("Got 2");
            Texture2D newTexture = new Texture2D(1, 1);
            Debug.Log("Got 3");
            newTexture.LoadImage(fileContents);
            Debug.Log("Got 4");
            photoRawImage.texture = newTexture;
            Debug.Log("Finished downloading!  newTexture.graphicsFormat = " + newTexture.graphicsFormat);
            Debug.Log("newTexture.width = " + newTexture.width);
            lastStatus = "Downloaded photo!";
        }
    });
    }

    public void SaveData()
    {

        if (photoUID.text.Equals(""))
        {
            print("NO DATA");
            return;
        }

        // example below will update location field
        databaseReference.Child(spreadsheetID).Child(photoUID.text).Child("Comments").
            SetValueAsync(location.text).ContinueWith((task => {

                if (task.IsCanceled)
                {
                    Firebase.FirebaseException e =
                    task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;

                    GetErrorMessage((AuthError)e.ErrorCode);

                    return;
                }

                if (task.IsFaulted)
                {

                    Firebase.FirebaseException e =
                    task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;

                    GetErrorMessage((AuthError)e.ErrorCode);

                    return;
                }

                if (task.IsCompleted)
                {

                    print("Success writing!");
                    lastStatus = "Success writing " + photoUID.text;

                }

            }));

    }

    public void LoadData()
    {
            DateTime startTime = DateTime.Now;
            lastStatus = "Starting large database read...";
        print("Starting database read...");

        FirebaseDatabase.DefaultInstance.GetReferenceFromUrl(DATA_URL).Child(spreadsheetID).GetValueAsync()
                    .ContinueWith((task => {

                        if (task.IsCanceled)
                        {
                            Firebase.FirebaseException e =
                            task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;

                            GetErrorMessage((AuthError)e.ErrorCode);

                            return;
                        }

                        if (task.IsFaulted)
                        {

                            Firebase.FirebaseException e =
                            task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;

                            GetErrorMessage((AuthError)e.ErrorCode);

                            return;
                        }

                        if (task.IsCompleted)
                        {

                            DataSnapshot snapshot = task.Result; //task.Result;

                            string photoData = snapshot.GetRawJsonValue();
                            //print("photoData = " + photoData);

                            int rowCount = 0;

                            foreach (var child in snapshot.Children)
                            {

                                string t = child.GetRawJsonValue();
                                rowCount++;
                                PhotoData extractedData = JsonUtility.FromJson<PhotoData>(t);

                                //print("row " + rowCount + " t: " + t);
                                //print("photoUID: " + extractedData.Photo_uid + ", size: " + extractedData.Size + ", location: " + extractedData.Location);

                                    //print("extractedData = " + extractedData);
                            }
                            lastStatus = "Read " + rowCount + " rows from database.";
                            print("Read " + rowCount + " rows from database.");
                            TimeElapsed(startTime);

                        }

                    }));

    }

    public void TimeElapsed (DateTime startTime)
    {
        DateTime endTime = DateTime.Now;
        TimeSpan ts = endTime - startTime;
        print("Time elapsed (ms): " + ts.Milliseconds);
    }

    void GetErrorMessage(AuthError errorCode)
    {

        string msg = errorCode.ToString();

        print(msg);
        lastStatus = msg;


    }

} // class




































