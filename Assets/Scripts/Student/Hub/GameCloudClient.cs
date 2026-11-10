using System;
using System.Text;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.Networking;

namespace ImagineQuest.Student
{
    /// <summary>Authenticated callable protocol. Tokens stay in request headers and are never logged.</summary>
    public static class GameCloudClient
    {
        [Serializable] private sealed class Request<T> { public T data; }
        [Serializable] private sealed class Reply<T> { public T result; public Error error; }
        [Serializable] private sealed class Error { public string message, status; }
        [Serializable] public sealed class Empty { }
        [Serializable] public sealed class Acknowledgement { public bool initialized; }

        public static async Task<TReply> CallAsync<TRequest, TReply>(string function, TRequest data)
            where TReply : class
        {
            if (!StudentCloudConfig.Enabled)
                throw new InvalidOperationException("Cloud progress is not enabled yet.");
            var app = FirebaseApp.DefaultInstance;
            if (app.Options.ProjectId != StudentCloudConfig.ProjectId)
                throw new InvalidOperationException("The cloud project configuration needs to be checked.");
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null) throw new InvalidOperationException("Sign in to connect your progress.");
            string owner = user.UserId;
            string token = await user.TokenAsync(false);
            if (FirebaseAuth.DefaultInstance.CurrentUser?.UserId != owner)
                throw new InvalidOperationException("Your account changed. Try again.");
            string url = "https://northamerica-northeast1-" + app.Options.ProjectId + ".cloudfunctions.net/" + function;
            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Request<TRequest> { data = data })));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.timeout = 30;
                var completion = new TaskCompletionSource<bool>();
                var operation = request.SendWebRequest();
                operation.completed += _ => completion.TrySetResult(true);
                if (operation.isDone) completion.TrySetResult(true);
                await completion.Task;
                if (FirebaseAuth.DefaultInstance.CurrentUser?.UserId != owner)
                    throw new InvalidOperationException("Your account changed. Try again.");
                Reply<TReply> reply = null;
                try { reply = JsonUtility.FromJson<Reply<TReply>>(request.downloadHandler.text); } catch (Exception) { }
                if (reply?.error != null) throw new InvalidOperationException(reply.error.message ?? "The change could not be saved. Try again.");
                if (request.result != UnityWebRequest.Result.Success || reply?.result == null)
                    throw new InvalidOperationException("Cloud connection unavailable. Your saved progress is safe; reconnect and retry.");
                return reply.result;
            }
        }
    }
}
