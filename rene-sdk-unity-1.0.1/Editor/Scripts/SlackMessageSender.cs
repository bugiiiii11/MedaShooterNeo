using System;
using System.Threading.Tasks;
using ReneVerse;
using UnityEngine;
using UnityEngine.Networking;

public class SlackMessageSender
{
    private static string url = "https://slack.com/api/chat.postMessage";
    private static string token = "xoxb-3111250812337-5771011138994-kx6P4yI8uz6KJKqtqfO06qdi";
    private static string channelID = "C05N8HMD615";


    public static async Task PostMessage(string text, Action onFeedBackMessageSuccessfullySent = null)
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            WWWForm form = new WWWForm();
            form.AddField("token", token);
            form.AddField("channel", channelID);
            form.AddField("text", text);

            byte[] bodyRaw = form.data;
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            www.SetRequestHeader("Authorization", "Bearer " + token);

            await www.SendWebRequestAsync();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                onFeedBackMessageSuccessfullySent?.Invoke();
            }
        }
    }


}