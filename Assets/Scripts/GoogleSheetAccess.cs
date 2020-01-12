using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GoogleSheetsForUnity
{

    public class GoogleSheetAccess : MonoBehaviour
    {
        public Text ReadinessMessage;
        public string AllPhotosSheet; // set sheet name in Inspector
        private PhotoReview photoReview;
        private int notReviewedCount;

        // PhotoInfo includes all of the fields on the sheet "all_photos"
        [System.Serializable]
        public struct PhotoInfo
        {
            public string filename;
            public string photo_uid;
            public string dropbox_path;
            public string folder;
            public string date;
            public string location;
            public string comments;
            public string favorite;
            public string forget_photo;
            public string file_type;
            public string audio_file_path; // dropbox path for audio file attached to this photo
            public string last_edited_date; // when was this photo last edited (in PhotoReviewer)?
            public string last_editor_name; // who last edited this photo in PhotoReviewer?
        }
        [HideInInspector]
        public PhotoInfo[] allPhotos;
        public PhotoInfo[] slideShowPhotos;
        public GameObject PhotoReviewerLogo;

        //void Awake()
        //{
        //    photoReviewerLogoAnimator = PhotoReviewerLogo.GetComponent<Animator>();
        //}

        private void OnEnable()
        {
            // Suscribe for catching cloud responses.
            Drive.responseCallback += HandleDriveResponse;
        }

        private void OnDisable()
        {
            // Remove listeners.
            Drive.responseCallback -= HandleDriveResponse;
        }

        void Start()
        {
            photoReview = GetComponent<PhotoReview>();
            PhotoReviewerLogo.gameObject.SetActive(true);
        }

        public void GetAllPhotos()
        {
            // Get all objects from current sheet
            Debug.Log("*** Started GetAllPhotos() loading from '" + AllPhotosSheet + "' at " + System.DateTime.Now.ToLongTimeString());
            Drive.GetTable(AllPhotosSheet, true);
        }


        public void UpdatePhotoInfo(int currentPhotoTemp, string columnName, string updatedValue)
        {
            // We will update an entire row for this photo.  Might someday update just the specific cell if faster.
            // First, construct a row item with current photo data...
            //int i = photoReview.currentPhoto;
            int i = currentPhotoTemp;
            string rowUID = allPhotos[i].filename; // TODO: see if match on filename is much faster than dropbox_path
            string columnToMatchUID = "filename";
            // handle blank date field
            var dateTempString = "";
            if(allPhotos[i].date != "")
            {
                //dateTempString = System.DateTime.Parse(allPhotos[i].date).ToString("d"); // de-serialize JSON formatting of date (not needed?)
            }

            PhotoInfo _photoData = new PhotoInfo
            {
               filename = allPhotos[i].filename,
               photo_uid = allPhotos[i].photo_uid,
               dropbox_path = allPhotos[i].dropbox_path,
               folder = allPhotos[i].folder,
               date = dateTempString, 
               location = allPhotos[i].location,
               comments = allPhotos[i].comments,
               favorite = allPhotos[i].favorite,
               forget_photo = allPhotos[i].forget_photo,
               file_type = allPhotos[i].file_type,
               audio_file_path = allPhotos[i].audio_file_path,
               last_edited_date = allPhotos[i].last_edited_date,
               last_editor_name = allPhotos[i].last_editor_name
            };

            // Now substitute in the updatedValue wherever it belongs...
            if (columnName == "date") _photoData.date = updatedValue;
            if (columnName == "location") _photoData.location = updatedValue;
            if (columnName == "comments") _photoData.comments = updatedValue;
            if (columnName == "favorite") _photoData.favorite = updatedValue;
            if (columnName == "forget_photo") _photoData.forget_photo = updatedValue;
            if (columnName == "audio_file_path") _photoData.audio_file_path = updatedValue;

            // Add last_edited_date and last_editor_name
            //dateTempString = System.DateTime.Now.ToLongTimeString();
            string dateTimeNowString = System.DateTime.Now.ToString("yyyy-MM-dd\\THH:mm:ss\\Z");
            _photoData.last_edited_date = dateTimeNowString;
            _photoData.last_editor_name = photoReview.editorName;

            string jsonPhotoInfo = JsonUtility.ToJson(_photoData);

            // Look in the current sheet for a photo with same "dropbox_path", and overwrite with the updated photo data.
            Debug.Log("*** Started UpdatePhotoInfo() loading at " + System.DateTime.Now.ToLongTimeString());

            Debug.Log("AllPhotosSheet = " + AllPhotosSheet);
            Drive.UpdateObjects(AllPhotosSheet, columnToMatchUID, rowUID, jsonPhotoInfo, false, true);
            Debug.Log("Updating row " + _photoData.filename);
            Debug.Log("With this data:\n" + jsonPhotoInfo);
        }

        // Processes the data received from the cloud.
        public void HandleDriveResponse(Drive.DataContainer dataContainer)
        {
            Debug.Log("In HandleDriveResponse. dataContainer.msg = " + dataContainer.msg);

            // getTable...
            if (dataContainer.QueryType == Drive.QueryType.getTable)
            {
                string rawJSon = dataContainer.payload;

                if (string.Compare(dataContainer.objType, AllPhotosSheet) == 0) // AllPhotosSheet
                {
                    // Parse from json to the desired object type.
                    PhotoInfo[] photos = JsonHelper.ArrayFromJson<PhotoInfo>(rawJSon);
                    allPhotos = photos; // allPhotos is public array of all the photo info

                    // Calculate # of un-reviewed photos
                    notReviewedCount = 0;
                    int forgottenPhotos = 0;
                    for (int i = 0; i < photos.Length; i++)
                    {
                        //if(i < 10) print("fn: " + photos[i].filename +
                        //    ", path:" + photos[i].dropbox_path +
                        //    ", date: " + photos[i].date);
                        if((photos[i].date == "" || photos[i].location == "") && (photos[i].forget_photo == ""))
                        {
                            notReviewedCount++;
                        }
                        if(photos[i].forget_photo != "")
                        {
                            forgottenPhotos++;
                        }
                    }

                    PhotoReviewerLogo.gameObject.SetActive(true);
                    ReadinessMessage.text = photos.Length + " photos\n" +
                        forgottenPhotos + " 'forgotten'" + "\n" + 
                        notReviewedCount + " still need info";

                    ReadinessMessage.text = ReadinessMessage.text + "\n\n<color=yellow>Tap to start</color>";
                    //ReadinessMessage.text = "allPhotos[0].filename = " + allPhotos[0].filename;
                    Debug.Log("*** FINISHED GetAllPHotos() loading at " + System.DateTime.Now.ToLongTimeString());
                    photoReview.EditorNamePanel.transform.gameObject.SetActive(true); // Show the EditorNamePanel

                    photoReview.StartButton.transform.gameObject.SetActive(true); // Tap button activated
                    photoReview.PreventClicksPanel.transform.gameObject.SetActive(false); // User cannot click anything until table is loaded
                }

            }

            // getAllTables...
            if (dataContainer.QueryType == Drive.QueryType.getAllTables)
            {
                string rawJSon = dataContainer.payload;

                // The response for this query is a json list of objects that hold two fields:
                // * objType: the table name (we use for identifying the type).
                // * payload: the contents of the table in json format.
                Drive.DataContainer[] tables = JsonHelper.ArrayFromJson<Drive.DataContainer>(rawJSon);

                // Once we get the list of tables, we could use the objTypes to know the type and convert json to specific objects.
                // On this example, we will just dump all content to the console, sorted by table name.
                string logMsg = "<color=yellow>All data tables retrieved from the cloud.\n</color>";
                for (int i = 0; i < tables.Length; i++)
                {
                    logMsg += "\n<color=blue>Table Name: " + tables[i].objType + "</color>\n" + tables[i].payload + "\n";
                }
                Debug.Log(logMsg);
            }

            // getObjectsByField...
            if (dataContainer.QueryType == Drive.QueryType.getObjectsByField)
            {
                string rawJSon = dataContainer.payload;
                Debug.Log(rawJSon);

                // Check if the type is correct.
                if (string.Compare(dataContainer.objType, AllPhotosSheet) == 0)
                {
                    // Parse from json to the desired object type.
                    PhotoInfo[] photos = JsonHelper.ArrayFromJson<PhotoInfo>(rawJSon);

                    for (int i = 0; i < photos.Length; i++) //TODO: Delete this after debug
                    {
                        Debug.Log("<color=yellow>Object retrieved from the cloud and parsed: \n</color>" +
                            "filename: " + photos[i].filename + "\n" +
                            "photo_uid: " + photos[i].photo_uid + "\n" +
                            "dropbox_path: " + photos[i].dropbox_path + "\n" +
                            "date: " + photos[i].date + "\n" +
                            "location: " + photos[i].location + "\n" +
                            "comments: " + photos[i].comments + "\n" +
                            "favorite: " + photos[i].favorite + "\n" +
                            "forget_photo: " + photos[i].forget_photo + "\n" +
                            "file_type: " + photos[i].location + "\n" +
                            "audio_file_path: " + photos[i].audio_file_path + "\n" +
                            "last_edited_date: " + photos[i].last_edited_date + "\n" +
                            "last_editor_name: " + photos[i].last_editor_name + "\n");
                    }
                }
            }

        }
    }

}
