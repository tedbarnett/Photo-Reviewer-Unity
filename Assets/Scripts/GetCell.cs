using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GoogleSheetsForUnity
{

    public class GetCell : MonoBehaviour
    {
        //public Text RowInput;
        //public Text ColumnInput;
        //public Button FileNameButton;
        //public Button CellButton;
        //public Button DateMissingButton;
        //public Text ResultAddress;
        //public Text FileName;
        public Text ReadinessMessage;
        public string AllPhotosSheet = "all_photos";
        public string DateMissingSheet = "date_missing";
        public string SummarySheet = "summary";
        private string cellValue;
        private PhotoReview photoReview;

        // PhotoInfo includes all of the fields on the sheet "all_photos"
        [System.Serializable]
        public struct PhotoInfo
        {
            public string filename;
            public string dropbox_path;
            public string folder;
            public string year;
            public string date;
            public string location;
            public string notes;
            public string file_type;
            public string dropbox_url;
        }

        // DateMissingPhotos includes all of the filenames for photos lacking date info
        [System.Serializable]
        public struct DateMissingPhotos
        {
            public string filename;
            public string dropbox_path;
            public string folder;
            public string year;
            public string date;
            public string location;
            public string notes;
            public string file_type;
            public string dropbox_url;
        }

        // Create an example object (DELETE THIS?)
        // private PhotoInfo _photoData = new PhotoInfo { filename = "a", dropbox_path = "b", best_guess_year = "c", date_guess = "d", location = "e", notes = "f" };


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
            //FileNameButton.onClick.AddListener(GetResultAddress);
            //CellButton.onClick.AddListener(delegate { GetCellValue("A", "2"); });
            //DateMissingButton.onClick.AddListener(GetEntireTable);
            photoReview = GetComponent<PhotoReview>();

        }

        public void GetResultAddress()
        {
            //Drive.GetObjectsByField(AllPhotosSheet, "filename", FileName.text, true);

        }
        public void GetCellValue(string Column, string Row)
        {
           Drive.GetCellValue(AllPhotosSheet, Column, Row, true);
           // return cellValue; // doesn't work: asynchronous process?
        }

        public void GetEntireTable()
        {
            // Get all objects from sheet 'all_photos'.
            Drive.GetTable(AllPhotosSheet, true);
        }

        public void GetAllPhotos()
        {
            // Get all objects from sheet 'all_photos'.
            Drive.GetTable(AllPhotosSheet, true);
        }

        void GetMissingDates()
        {
            // Get all objects from sheet 'date_missing'.
            Drive.GetTable(DateMissingSheet, true);
        }

        // Processes the data received from the cloud.
        public void HandleDriveResponse(Drive.DataContainer dataContainer)
        {
            Debug.Log(dataContainer.msg);

            // If it's getObjectsByField...
            if (dataContainer.QueryType == Drive.QueryType.getObjectsByField)
            {
                string rawJSon = dataContainer.payload;
                Debug.Log(rawJSon);

                // Check if the type is correct.
                if (string.Compare(dataContainer.objType, AllPhotosSheet) == 0)
                {
                    // Parse from json to the desired object type.
                    PhotoInfo[] photos = JsonHelper.ArrayFromJson<PhotoInfo>(rawJSon);

                    for (int i = 0; i < photos.Length; i++)
                    {
                        Debug.Log("<color=yellow>Object retrieved from the cloud and parsed: \n</color>" +
                            "filename: " + photos[i].filename + "\n" +
                            "dropbox_path: " + photos[i].dropbox_path + "\n" +
                            "year: " + photos[i].year + "\n" +
                            "date: " + photos[i].date + "\n" +
                            "location: " + photos[i].location + "\n" +
                            "notes: " + photos[i].notes + "\n" +
                            "file_type: " + photos[i].location + "\n" +
                            "dropbox_url: " + photos[i].location + "\n");
                    }
                }
            }

            // If it's getCellValue...
            if (dataContainer.QueryType == Drive.QueryType.getCellValue)
            {
                cellValue = dataContainer.payload;
                string rawJSon = dataContainer.payload;
                Debug.Log("getCellValue = " + cellValue);

                // Check if the type is correct.
                if (string.Compare(dataContainer.objType, AllPhotosSheet) == 0)
                {
                    //Debug.Log("<color=yellow>Cell " + ColumnInput.text+ RowInput.text + " contents are: \n</color>" + dataContainer.payload + "\n");
                    cellValue = dataContainer.payload;
                    photoReview.DebugText.text = cellValue;
                }
                ReadinessMessage.text = "Ready. Tap to start.";
            }

            // If it's getTable...
            if (dataContainer.QueryType == Drive.QueryType.getTable)
            {
                string rawJSon = dataContainer.payload;
                // Debug.Log(rawJSon);

                // Confirm the sheet was of the right type...
                if (string.Compare(dataContainer.objType, AllPhotosSheet) == 0)
                {
                    // Parse from json to the desired object type.
                    PhotoInfo[] photos = JsonHelper.ArrayFromJson<PhotoInfo>(rawJSon);

                // string logMsg = "<color=yellow>" + photos.Length.ToString() + " objects retrieved from the cloud and parsed:</color>";
                //    for (int i = 0; i < photos.Length; i++)
                //    {
                //        logMsg += "\n" +
                //            "filename: " + photos[i].filename + "\n" +
                //            "dropbox_path: " + photos[i].dropbox_path + "\n" +
                //            "year: " + photos[i].year + "\n" +
                //            "date: " + photos[i].date + "\n" +
                //            "location: " + photos[i].location + "\n" +
                //            "notes: " + photos[i].notes + "\n" +
                //            "file_type: " + photos[i].location + "\n" +
                //            "dropbox_url: " + photos[i].location + "\n";
                //}
                    //Debug.Log(logMsg);
                    ReadinessMessage.text = photos.Length + " photos to review.\nTap to start.";
                    photoReview.StartButton.transform.gameObject.SetActive(true);

                }

            }

            // If it's getAllTables...
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
        }
    }

}
