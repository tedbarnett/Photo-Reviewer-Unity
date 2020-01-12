using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AudioController : MonoBehaviour
{
    public string fileName = "/Users/tedbarnett/Library/Application Support/Barnett Labs/Photo Review/mqlMA/photorevieweraudio_test/01142_s_18alpyxtxx0841.wav";
    public AudioSource audio;


    void Start()
    {
        audio = GetComponent<AudioSource>();
        StartCoroutine(GetAudioClip());


    }

    IEnumerator GetAudioClip()
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + fileName, AudioType.WAV))
        {
            yield return www.Send();

            if (www.isNetworkError)
            {
                Debug.Log(www.error);
            }
            else
            {
                AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                audio.clip = myClip;
                audio.Play();
            }
        }
    }
}