using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using DBXSync;
using System;
using UnityEngine.UI;

public class ServerAccess : MonoBehaviour
{
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    public Text statusText;
    public RawImage rawImage;



    public void DownloadFile(string fileName)
    {

        // IMAGE
        DropboxSync.Main.GetFile<Texture2D>(fileName, new Progress<TransferProgressReport>((progress) => { }),
        (tex) => {
            UpdatePicture(tex);
        }, (ex) => {
            Debug.LogError($"Error getting picture from Dropbox: {ex}");
        }, receiveUpdates: true, useCachedFirst: true);


        Debug.Log("Got file " + fileName);

        return;
    }

    void UpdatePicture(Texture2D tex)
    {
        rawImage.texture = tex;
        rawImage.GetComponent<AspectRatioFitter>().aspectRatio = (float)tex.width / tex.height;
    }



    private async void OLDDownloadFileFromDropbox(string dropBoxFileName)
    {
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var localPath = await DropboxSync.Main.GetFileAsLocalCachedPathAsync(dropBoxFileName,
                                new Progress<TransferProgressReport>((report) => {
                                    statusText.text = $"Downloading: {report.progress}% {report.bytesPerSecondFormatted}";
                                }), _cancellationTokenSource.Token);
            //var localPath = await DropboxSync.Main.GetFileAsLocalCachedPathAsync(dropBoxFileName,
            //        new Progress<TransferProgressReport>((report) => {
            //            statusText.text = $"Downloading: {report.progress}% {report.bytesPerSecondFormatted}";
            //        }), _cancellationTokenSource.Token);

            print($"Completed");
            statusText.text = $"<color=green>Local path: {localPath}</color>";

        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                Debug.Log("Download cancelled");
                statusText.text = $"<color=orange>Download canceled.</color>";
            }
            else
            {
                Debug.LogException(ex);
                statusText.text = $"<color=red>Download failed.</color>";
            }
        }


    }
}
