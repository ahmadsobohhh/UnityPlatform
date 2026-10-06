using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WebViewToolkit;
using WebViewToolkit.Native;

namespace ImagineQuest.Gameplay
{
    // Local page supplies a real HTTP origin/referrer for the official YouTube iframe.
    public sealed class QuestYouTubePlayer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
    {
        private WebViewInstance browser;
        private TcpListener server;
        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        private RawImage surface;
        public Action Completed;
        public Action<string> Error;
        public bool Ready { get; private set; }
        public bool Paused { get; private set; }
        public double CurrentTime { get; private set; }
        public double Duration { get; private set; }
        private float startedAt;
        private bool reportedTimeout;
        [Serializable] private class State { public string type; public int state; public int code; public double time; public double duration; }

        public void Open(string videoId, RawImage target)
        {
            Close();
            surface = target;
            startedAt = Time.realtimeSinceStartup;
            reportedTimeout = false;
            try
            {
                server = new TcpListener(IPAddress.Loopback, 0);
                server.Start();
                var listener = server;
                var origin = "http://127.0.0.1:" + ((IPEndPoint)server.LocalEndpoint).Port;
                var path = "/" + Guid.NewGuid().ToString("N");
                var page = BuildPage(videoId, origin);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (true)
                        {
                            using (var client = await listener.AcceptTcpClientAsync())
                            using (var stream = client.GetStream())
                            {
                                client.ReceiveTimeout = 3000;
                                var input = new byte[8192];
                                var count = stream.Read(input, 0, input.Length);
                                bool valid = Encoding.ASCII.GetString(input, 0, count).StartsWith("GET " + path + " ", StringComparison.Ordinal);
                                var body = Encoding.UTF8.GetBytes(valid ? page : "Not found");
                                var header = Encoding.ASCII.GetBytes("HTTP/1.1 " + (valid ? "200 OK" : "404 Not Found") + "\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: " + body.Length + "\r\nConnection: close\r\n\r\n");
                                await stream.WriteAsync(header, 0, header.Length);
                                await stream.WriteAsync(body, 0, body.Length);
                            }
                        }
                    }
                    catch (Exception) { /* Listener shutdown or a disconnected local browser. */ }
                });
                browser = WebViewManager.Instance.CreateWebView(1280, 720);
                if (browser == null) throw new InvalidOperationException("Embedded browser could not start.");
                browser.MessageReceived += OnMessage;
                browser.NavigationCompleted += OnNavigation;
                browser.Navigate(origin + path);
            }
            catch (Exception ex) { Close(); Error?.Invoke("YouTube browser could not start: " + ex.Message); }
        }

        private void OnMessage(string message) => messages.Enqueue(message);
        private void OnNavigation(string url, bool success) { if (!success) messages.Enqueue("{\"type\":\"error\",\"code\":-1}"); }
        private void Update()
        {
            if (browser == null) return;
            WebViewManager.Instance.Tick();
            if (surface != null) surface.texture = browser.Texture;
            while (messages.TryDequeue(out var message))
            {
                State state;
                try { state = JsonUtility.FromJson<State>(message); } catch { continue; }
                if (state == null) continue;
                if (state.type == "ready") Ready = true;
                if (state.type == "state")
                {
                    CurrentTime = state.time;
                    Duration = state.duration;
                    Paused = state.state == 2 || state.state == 5;
                    if (state.state == 0) { Completed?.Invoke(); return; }
                }
                if (state.type == "error") Error?.Invoke("YouTube could not play this video (" + state.code + "). Retry or exit; lesson has not started.");
            }
            if (!Ready && !reportedTimeout && Time.realtimeSinceStartup - startedAt > 30)
            {
                reportedTimeout = true;
                Error?.Invoke("YouTube is taking too long to load. Check your connection, then retry.");
            }
        }
        public void TogglePause() => Execute(Paused ? "p.playVideo()" : "p.pauseVideo()");
        public void Seek(double seconds) => Execute("p.seekTo(" + seconds.ToString(CultureInfo.InvariantCulture) + ",true)");
        public void Volume(float value, bool mute) => Execute("p.setVolume(" + Mathf.RoundToInt(value * 100) + ");p." + (mute ? "mute()" : "unMute()"));
        private void Execute(string script) { if (Ready) browser?.ExecuteScript("if(window.p){" + script + ";}"); }
        public void OnPointerDown(PointerEventData e) => Send(e, MouseEventType.Down);
        public void OnPointerUp(PointerEventData e) => Send(e, MouseEventType.Up);
        public void OnPointerMove(PointerEventData e) => Send(e, MouseEventType.Move);
        private void Send(PointerEventData e, MouseEventType type)
        {
            if (browser == null || surface == null) return;
            var rect = surface.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera ?? Camera.main, out var local)) return;
            browser.SendMouseEvent(type, WebViewToolkit.Native.MouseButton.Left,
                Mathf.Clamp01((local.x - rect.rect.xMin) / rect.rect.width),
                Mathf.Clamp01(1 - (local.y - rect.rect.yMin) / rect.rect.height));
        }
        public void Close()
        {
            if (browser != null)
            {
                browser.MessageReceived -= OnMessage;
                browser.NavigationCompleted -= OnNavigation;
                browser.Dispose();
                browser = null;
            }
            server?.Stop(); server = null;
            Ready = false; CurrentTime = Duration = 0;
            while (messages.TryDequeue(out _)) { }
        }
        private void OnDestroy() => Close();
        private static string BuildPage(string id, string origin) => @"<!doctype html><html><head><meta name='referrer' content='strict-origin-when-cross-origin'><style>html,body,#player{margin:0;width:100%;height:100%;background:#000;overflow:hidden}</style></head><body><div id='player'></div><script>
function send(x){window.chrome.webview.postMessage(JSON.stringify(x));}
var p; function onYouTubeIframeAPIReady(){p=new YT.Player('player',{videoId:'" + id + @"',playerVars:{origin:'" + origin + @"',autoplay:1,controls:1,playsinline:1,fs:0},events:{onReady:function(){send({type:'ready'});p.playVideo();},onError:function(e){send({type:'error',code:e.data});},onStateChange:function(){report();}}});}
function report(){if(p&&p.getPlayerState)send({type:'state',state:p.getPlayerState(),time:p.getCurrentTime(),duration:p.getDuration()});}
setInterval(report,250);</script><script src='https://www.youtube.com/iframe_api'></script></body></html>";
    }
}
