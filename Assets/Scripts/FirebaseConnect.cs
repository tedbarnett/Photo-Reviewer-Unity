using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Unity.Editor;
using UnityEngine;
using UnityEngine.UI;

public class FirebaseConnect : MonoBehaviour
{

    private string DATA_URL = "https://photoreviewer-94984.firebaseio.com/"; // insert data URL here
    private DatabaseReference databaseReference;
    private string spreadsheetID = "BarnettPhotos200"; //TODO: Rename as dataNode, or something non-spreadsheety


    private PhotoData data;
    private string lastStatus;

    public GameObject firebaseDebugText;
    public PhotoData[] photosTable;

    void Start()
    {
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl(DATA_URL);
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        firebaseDebugText.GetComponent<Text>().text = "";
        lastStatus = "";
    }

    private void LateUpdate()
    {
        if (lastStatus == firebaseDebugText.GetComponent<Text>().text) return;
        firebaseDebugText.GetComponent<Text>().text = lastStatus;
        lastStatus = firebaseDebugText.GetComponent<Text>().text;

    }

    public void LoadData()
    {
        DateTime startTime = DateTime.Now;
        lastStatus = "Starting large database read...";
        print("*** Starting database read...");
        //FirebaseDatabase.DefaultInstance.GetReferenceFromUrl(DATA_URL).Child(spreadsheetID).GetValueAsync()

        databaseReference.Child(spreadsheetID).GetValueAsync()
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
                            print("in LoadData(), task.IsCompleted.");
                            lastStatus = "in LoadData(), task.IsCompleted.";
                            DataSnapshot snapshot = task.Result; //task.Result;

                            string photoData = snapshot.GetRawJsonValue();
                            //print("photoData = " + photoData);

                            // Parse from json to the desired object type.
                            PhotoData[] photos = JsonHelper.ArrayFromJson<PhotoData>(photoData);
                            photosTable = photos; // allPhotos is public array of all the photo info

                            //int rowCount = 0;

                            //foreach (var child in snapshot.Children)
                            //{

                            //    string t = child.GetRawJsonValue();
                            //    rowCount++;
                            //    PhotoData extractedData = JsonUtility.FromJson<PhotoData>(t);
                            //    if(rowCount < 10)
                            //    {
                            //        print("row " + rowCount + " t: " + t);
                            //        print("photoUID: " + extractedData.Photo_uid + ", size: " + extractedData.Size + ", location: " + extractedData.Location);
                            //        print("extractedData = " + extractedData);
                            //    }

                            //}
                            lastStatus = "allPhotos.Length = " + photosTable.Length;
                            print("allPhotos.Length = " + photosTable.Length);
                            TimeElapsed(startTime);

                        }

                    }));

    }

    public void TimeElapsed(DateTime startTime)
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

    // Helper class: because UnityEngine.JsonUtility does not support deserializing an array...
    // http://forum.unity3d.com/threads/how-to-load-an-array-with-jsonutility.375735/
    public class JsonHelper
    {
        public static T[] ArrayFromJson<T>(string json)
        {
            string newJson = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson); // fails here
            return wrapper.array;
        }

        public static string ToJson<T>(T[] array, bool prettyPrint = false)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.array = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }


} //class
