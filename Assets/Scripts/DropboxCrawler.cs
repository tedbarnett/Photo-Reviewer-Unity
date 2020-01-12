// DropboxSync v2.1.1
// Created by George Fedoseev 2018-2019

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;
using UnityEngine.UI;

using System.IO;

public class DropboxCrawler : MonoBehaviour
{
    [HideInInspector]
    public Button goUpButton;
    [HideInInspector]
    public ScrollRect scrollRect;
    public Text fileStatusText;
    [HideInInspector]
    public Text fileListText;
    public Button crawlButton;

    List<string> pathsHistory = new List<string>();

    void Start()
    {
        fileStatusText.text = "Click button above to start";
        crawlButton.onClick.AddListener(() => {
            GetFullFileList();
        });
    }

    public void GetFullFileList()
    {
        fileStatusText.text = "Downloading file list...";
        string dropboxFolderPath = "/";
        //string dropboxFolderPath = "//Barnett Family Photos - ScanCafe/Barnett Family Photos 1955-2000/";
        bool recursionFlag = true;
        Debug.Log("GetFullFileList " + dropboxFolderPath);
        //RenderLoading();
        //pathsHistory.Add(dropboxFolderPath);

        DropboxSync.Main.ListFolder(dropboxFolderPath, (folderItems) => {
            DBXSync.Metadata newMetadataWrapper = new DBXSync.Metadata();
            newMetadataWrapper = folderItems[1];
            string folderListToJson = JsonUtility.ToJson(newMetadataWrapper); // how to turn full folderIems list into JSON?
            Debug.Log("folderItem[1] = " + newMetadataWrapper);
            Debug.Log("folderListToJson[1] = " + folderListToJson);
            WriteFileList(folderItems);

        }, (ex) => {
            Debug.LogError($"Failed to get folder items for folder {ex}");
        }, recursive: recursionFlag); // flip to true later!
    }


    public void WriteFileList(List<DBXSync.Metadata> folderItems)
    {

        var fileName = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "/AllFiles.csv";

        if (File.Exists(fileName))
        {
            Debug.Log(fileName + " already exists.  Over-writing...");
            //return;
        }
        var sr = File.CreateText(fileName);
        string csvSep = '"' + "," + '"';

        //var orderedItems = folderItems.OrderBy(x => x.name).OrderByDescending(x => x.IsFolder);
        var orderedItems = folderItems.OrderBy(x => x.path_display);

        sr.WriteLine('"' + "name" + csvSep
            + "path_display" + csvSep
            + "id" + csvSep
            + "size"
            + '"');

        foreach (var item in orderedItems)
        {
            var _item = item;
            if (!_item.IsFolder)
            {
                sr.WriteLine('"' + _item.name + csvSep
                    + _item.path_display + csvSep
                    + _item.id + csvSep
                    + _item.size
                    + '"');
            }

        }
        sr.Close();
        Debug.Log("File completed!");
        fileStatusText.text = "Completed.\nFilename: " + fileName;
        return;
    }

}
