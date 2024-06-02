using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using UnityEngine;
using System.Diagnostics;

/*
 * HarmonyConnector - A Plugin that connects a ProjectP AI NPC with Harmony Link
 * Author: katoki (ProjectP) and RuntimeRacer (Project Harmony.AI)
 * Version: 0.1
 * Compatibility: Harmony Link v0.2.0 onwards.
 * 
 * ---- Usage Instructions ----
 * Currently the configuratin happens inline. This might change in a later version,
 * or we might figure out a simpler way to improve UX here.
 * 
 * Please make sure to read the following instructions carefully when using these Plugins:
 * 
 * _entityId: This value needs to EXACTLY match the ID if your entity in Harmony Link. Otherwise it won't be able to initialize the AI NPC.
 * 
 * _wsEndpoint: This needs to match the Endpoint of Harmony Link from the perspective of the CLIENT.
 *              This Plugin is being registered to LiveManager (SERVER), and then downloaded by the game (CLIENT).
 *              As soon as it has been downloaded, the Plugin will try to connect to Harmony Link.
 *              
 *              If you play SOLO and are running LiveManager and Harmony Link on you machine locally, DO NOT CHANGE this value.
 *              
 *              If you want to play together with others and want to have people join your ProjectP World, please make sure to do one of the following:
 *              
 *              - Variant A: Host Harmony Link on your machine, and replace this Value with the external IP or DynDNS IP and Port of your machine, so others can connect to Harmony Link.
 *                           Also make sure to open the Port of your Router to the outside and map it to your PC, and your Firewall allows the traffic, so it's possible to connect.
 *                           
 *              - Variant B: Host Harmony Link on a public server and replace this value with the external IP and Port of the public server, and make sure the server's Firewall allows the traffic, so it's possible to connect.
 *              
 * _wsBufferSize: This value specifies the maximum size of a websocket message transferred between Plugin and Harmony Link, in bytes. By default it's set to roughly 8MB.
 *                Do not change this value unless you run into Issues with modules that handle larger data amounts (Like TTS / STT / Vision / Movement).
 * 
 * 
 * ----------------------------
 */
public class HarmonyConnector : CustomBehaviour
{
    // Module Configuration
    public string _entityId = "kaji";
    private string _wsEndpoint = "ws://127.0.0.1:28080";
    private int _wsBufferSize = 8192000;

    // Processing related    
    private string _harmonySessionId;
    private ClientWebSocket _webSocketClient;
    public string chatResult = null;
    // Sync Strings are used across Unity Plugins and Components to exchange information (interal, global state machine)
    public SyncString npcName;

    // Implements Start() method of CustomBehaviour
    public void Start()
    {
        // Initializes all parts of the module
        npcName = new SyncString(this, "npcName");
        ConnectorEventHandler(_wsEndpoint, _wsBufferSize);
    }

    // ConnectorEventHandler runs the Harmony Link Connection in a Subtask / Thread
    public async Task ConnectorEventHandler(string wsEndpoint, int wsBufferSize)
    {
        _wsEndpoint = wsEndpoint;
        _wsBufferSize = wsBufferSize;
        _webSocketClient = new ClientWebSocket();
        await StartAsync();
    }

    public async Task StartAsync()
    {
        try
        {
            await _webSocketClient.ConnectAsync(new Uri(_wsEndpoint), CancellationToken.None);                
            InitEntityAsync(_entityId);
            ListenToWebSocketAsync();
        }
        catch (Exception e)
        {
            Debug.Log("WebSocket Handler Crashed: " + e.ToString());
        }
    }

    private async Task ListenToWebSocketAsync()
    {
        var buffer = new byte[_wsBufferSize];
        while (_webSocketClient.State == WebSocketState.Open)
        {
            var result = await _webSocketClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            else
            {
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                HandleResponse(receivedMessage);
            }
        }
    }


    public async Task SendEventAsync(string eventId, string eventType, string status, object payload)
    {
        var message = new
        {
            event_id = eventId,
            event_type = eventType,
            status = status,
            payload = payload
        };

        string messageJson = Newtonsoft.Json.JsonConvert.SerializeObject(message);
        byte[] buffer = Encoding.UTF8.GetBytes(messageJson);
        Debug.Log(messageJson);
        await _webSocketClient.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task InitEntityAsync(string characterId)
    {
        var payload = new { entity_id = characterId }; 
        await SendEventAsync("init_char0", "INIT_ENTITY", "NEW", payload);
    }

    public async Task UserUttrance(string content)
    {
        var payload = new { type = "UTTERANCE_VERBAL", content = content}; 
        await SendEventAsync("utterance_1234", "USER_UTTERANCE", "NEW", payload);       
    }
        
    public async Task StartListeningAsync()
    {
        await SendEventAsync("start_listen", "STT_START_LISTEN", "NEW", null);
    }

    public async Task StopListeningAsync()
    {
        await SendEventAsync("stop_listen", "STT_STOP_LISTEN", "NEW", null);
    }

    public async Task SendAudioDataAsync(string audioBytes)
    {
        var payload = new { AudioBytes = audioBytes };
        await SendEventAsync("audio_data_1234", "STT_INPUT_AUDIO", "NEW", payload);
    }

    public async Task SendTtsGenerateSpeechAsync(string text)
    {        
        var payload = new { type = "UTTERANCE_VERBAL", content = text}; 
        await SendEventAsync("speechgen_123", "TTS_GENERATE_SPEECH", "NEW", payload);
    }

    class HarmonyLinkEvent
    {
        public string event_id { get; set; }
        public string event_type { get; set; }
        public string status { get; set; }
        public dynamic payload { get; set; }
    }

    private void HandleResponse(string message)
    {

        Debug.Log("HandleResponse: " + message);
        var eventResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<HarmonyLinkEvent>(message);
        switch ((string)eventResponse.event_type)
        {  
            case "CHAT_HISTORY":
            {
                    // Evaluates the Chat history from Harmony Link if receives
                    JObject parsedJson = JObject.Parse(message);
                    var lastItem = parsedJson["payload"].Last;
                    npcName.val = (string)lastItem["Name"];
                    chatResult = (string)lastItem["Message"];
            }   
            break;
            case "INIT_ENTITY":
                // TODO
                break;
            case "AI_UTTERANCE":
                // TODO
                break;            
            case "STT_START_LISTEN":
                // TODO
                break;
            case "STT_STOP_LISTEN":
                // TODO
                break;
            case "STT_INPUT_AUDIO":
                // TODO
                break;
            case "AI_SPEECH":
                {    
                    // Parse JSON and update SyncString in voice Module with the value received.
                    // If voice module is not loaded, this will do nothing.
                    JObject parsedJson = JObject.Parse(message);
                    SyncString npcAudioBase64 = new SyncString(this, "npcAudioBase64");
                    npcAudioBase64.val = parsedJson["payload"]["audio"].ToString();
                }                
                break;
            default:
                Console.WriteLine("Unhandled event type");
                break;
        }
    }

    public async Task StopAsync()
    {
        if (_webSocketClient != null && _webSocketClient.State == WebSocketState.Open)
        {
            await _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
    }
}