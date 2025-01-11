using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Text;
using Steamworks;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Linq;
using Random = System.Random;

public class SteamHandler : MonoBehaviour
{
    Random rng = new Random();

    private Callback<LobbyMatchList_t> lobbyListCallback;
    private Callback<LobbyCreated_t> lobbyCreatedCallback;
    private Callback<LobbyEnter_t> joinLobbyCompletedCallback;
    private Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;
    private Callback<LobbyChatMsg_t> lobbyMessageCallback;
    private Callback<P2PSessionRequest_t> PeerSessionRequestCallback;

    public TextMeshProUGUI usernameTMP;
    public TMP_InputField lobbyName;
    public GameObject Room;

    public Transform roomsTransform;

    private CSteamID personalCSteamID;
    private float lastLobbyYpos = 670f;

    private int currentMembersY = 0;
    private Dictionary<CSteamID, int> currentMembers = new Dictionary<CSteamID, int>();
    private Dictionary<CSteamID, Coroutine> playerSmoothings = new Dictionary<CSteamID, Coroutine>();
    byte[] lastPositionPakcet = new byte[0];
    private List<string> currentChats = new List<string>();

    private CSteamID currentLobby;
    private CSteamID lastLobby;

    private bool justSentMessage = false;
    private bool canSendMessage = true;

    private GameObject Canvas;
    private GameObject MainMenu;
    private GameObject MPCanvas;
    private GameObject MPManage;
    private GameObject OptionsScreen;

    private GameObject Chat;
    private TMP_InputField ChatInput;

    private GameObject Memberlist;
    private GameObject Messages;
    private GameObject NewMessage;

    private GameObject Player;
    private GameObject PlayerModel;
    private Animator PlayerAnimator;

    private GameObject PlayerPrefab;
    private GameObject Players;

    private List<CSteamID> Connections = new List<CSteamID>();
    private List<CSteamID> BlueTeam = new List<CSteamID>();
    private List<CSteamID> RedTeam = new List<CSteamID>();
    private int redTeamTotal = 0;
    private int blueTeamTotal = 0;

    private bool isInMultiplayer = false;
    private bool wonGame = false;
    private Dictionary<CSteamID, double> currentGameLeaderboard = new Dictionary<CSteamID, double>();

    public float fadeDuration = 2f;
    private Color startColor = Color.white;
    private Color endColor = new Color(1f, 1f, 1f, 0f);

    private float fadeTimer;
    private float fadeUntilTimer;
    private bool fading = false;

    bool optionsOpened = false;

    private CSteamID hostSteamID;
    private bool isHost;
    private int membersReady;
    private List<int> votedModes = new List<int> { 0, 0, 0, 0 };
    private List<int> votedMaps = new List<int> { 0, 0, 0, 0 };
    private int votedMap = 3;
    private int currentlobbyMap = 2;
    private int currentLobbyMode = 0;
    private int membersVoting = 1;
    private int cooldown = 5;
    private bool coolDownActive = false;

    private List<CSteamID> IDofReadyMembers = new List<CSteamID>();
    public TextMeshProUGUI readyUp;
    private GameObject readyUpObject;
    public TextMeshProUGUI startVoting;
    private GameObject LobbyObjects;
    private GameObject startVotingObject;
    private GameObject ModeVotingMenu;
    private GameObject votingMenu;
    private GameObject votedInfoObject;
    private GameObject GameCanvas;
    private GameObject InitialCountdown;
    private GameObject WinScreen;
    public GameObject RetryButton;
    public GameObject WaitingPlayersText;
    public GameObject GameOverObject;
    public TextMeshProUGUI WinnerTMP;
    public TextMeshProUGUI FinalLeaderboard;
    public TextMeshProUGUI votedInfo;

    private void Start()
    {

        Debug.Log("Starting GameHandler");
        SceneManager.LoadScene("Main Menu");
        readyUpObject = GameObject.Find("Ready");
        startVotingObject = GameObject.Find("Start");
        ModeVotingMenu = GameObject.Find("ModeVotingMenu");
        votingMenu = GameObject.Find("VotingMenu");
        InitialCountdown = GameObject.Find("InitialCountdown");
        WinScreen = GameObject.Find("WinMenu");
        Canvas = GameObject.Find("Canvas");
        MPCanvas = GameObject.Find("Multiplayer Canvas");
        MPManage = GameObject.Find("Multiplayer Mangement");
        Chat = GameObject.Find("Chat");
        ChatInput = GameObject.Find("message").GetComponent<TMP_InputField>(); ;
        Memberlist = GameObject.Find("memberlist");
        Messages = GameObject.Find("messages");
        NewMessage = GameObject.Find("newMessage");
        PlayerPrefab = GameObject.Find("PlayerPrefab");
        Players = GameObject.Find("Players");
        OptionsScreen = GameObject.Find("Options");
        MainMenu = GameObject.Find("MainMenu");
        votedInfoObject = GameObject.Find("VotedInfo");
        LobbyObjects = GameObject.Find("LobbyObjects");
        GameCanvas = GameObject.Find("Game Canvas");
        Player = GameObject.Find("Player");
        PlayerModel = GameObject.Find("PlayerModel");
        PlayerAnimator = PlayerModel.GetComponent<Animator>();

        ModeVotingMenu.SetActive(false);
        votingMenu.SetActive(false);
        readyUpObject.SetActive(false);
        startVotingObject.SetActive(false);
        votedInfoObject.SetActive(false);
        InitialCountdown.SetActive(false);
        OptionsScreen.SetActive(false);
        MPCanvas.SetActive(false);
        MPManage.SetActive(false);
        LobbyObjects.SetActive(false);
        GameCanvas.SetActive(false);
        Player.SetActive(false);

        currentMembersY = Screen.height - 20;

        if (SteamManager.Initialized)
        {
            personalCSteamID = SteamUser.GetSteamID();
            string name = SteamFriends.GetPersonaName();
            usernameTMP.text = name;
            lobbyListCallback = Callback<LobbyMatchList_t>.Create(OnLobbyListReceived);
            lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            joinLobbyCompletedCallback = Callback<LobbyEnter_t>.Create(OnJoinLobbyCompleted);
            lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            lobbyMessageCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChatRecieved);
            PeerSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);

            SteamNetworkingUtils.InitRelayNetworkAccess();

        }
        else
        {
            Debug.LogError("Steamworks is not initialized.");
        }

        SceneManager.sceneLoaded += ResetPlayerPos;
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (isInMultiplayer)
        {
            uint packetSize;
            if (SteamNetworking.IsP2PPacketAvailable(out packetSize))
            {
                byte[] receiveBuffer = new byte[1200];
                CSteamID remoteSteamID;

                if (SteamNetworking.ReadP2PPacket(receiveBuffer, 1200, out packetSize, out remoteSteamID))
                {
                    if (System.Text.Encoding.UTF8.GetString(receiveBuffer).Contains("establish"))
                    {
                        if (!Connections.Contains(remoteSteamID))
                        {
                            Debug.Log("Establishing");
                            byte[] message = System.Text.Encoding.UTF8.GetBytes("establish");
                            SteamNetworking.SendP2PPacket(remoteSteamID, message, (uint)message.Length, EP2PSend.k_EP2PSendReliable);

                            GameObject newPlayer = Instantiate(PlayerPrefab, Players.transform);
                            TextMeshPro Nametag = newPlayer.GetComponentInChildren<TextMeshPro>();
                            Nametag.text = SteamFriends.GetFriendPersonaName(remoteSteamID);
                            newPlayer.name = remoteSteamID.ToString();
                            DontDestroyOnLoad(newPlayer);
                            Connections.Add(remoteSteamID);
                            UpdateMemberList();
                        }
                    }
                    else if (System.Text.Encoding.UTF8.GetString(receiveBuffer).Contains("Ready"))
                    {
                        membersReady++;
                        IDofReadyMembers.Add(remoteSteamID);
                    }
                    else if (System.Text.Encoding.UTF8.GetString(receiveBuffer).Contains("Finished "))
                    {

                        double dateFinish = double.Parse(System.Text.Encoding.UTF8.GetString(receiveBuffer).Replace("Finished ", ""));
                        Debug.Log("Someone has finished. " + dateFinish);
                        Debug.Log(currentGameLeaderboard.Count);


                        if (currentGameLeaderboard.Count <= 0)
                        {
                            string message = "Somebody has finished the map. Warping back to lobby in 1 minute.";

                            currentChats.Add(message + "\n");
                            UpdateChatList();
                            Invoke("BackToLobby", 60);
                        }

                        currentGameLeaderboard.Add(remoteSteamID, dateFinish);

                        if (currentGameLeaderboard.Count == currentMembers.Count)
                        {
                            string message = "Warping to lobby now.";

                            currentChats.Add(message + "\n");
                            UpdateChatList();
                            BackToLobby();
                        }
                    }

                    else if (System.Text.Encoding.UTF8.GetString(receiveBuffer).Contains("VotingMode"))
                    {

                        // Handle smoooothingGameOverObject.SetActive(false);
                        Memberlist.SetActive(true);
                        ModeVotingMenu.SetActive(true);
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else if (System.Text.Encoding.UTF8.GetString(receiveBuffer).Contains("VotingMap"))
                    {
                        votingMenu.SetActive(true);
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else if (System.Text.Encoding.UTF8.GetString(receiveBuffer).Contains("VotedMode "))
                    {
                        int Message = int.Parse(System.Text.Encoding.UTF8.GetString(receiveBuffer).Replace("VotedMode ", ""));
                        votedModes[Message - 1]++;
                    }
                    else if (System.Text.Encoding.UTF8.GetString(receiveBuffer).Contains("Voted "))
                    {
                        int Message = int.Parse(System.Text.Encoding.UTF8.GetString(receiveBuffer).Replace("Voted ", ""));
                        votedMaps[Message - 3]++;
                    }
                    else if (System.Text.Encoding.UTF8.GetString(receiveBuffer).Contains("ChosenMode "))
                    {
                        int Message = int.Parse(System.Text.Encoding.UTF8.GetString(receiveBuffer).Replace("ChosenMode ", ""));
                        currentLobbyMode = Message;
                        Debug.Log("mode recieved: " + currentLobbyMode);
                    }
                    else if (System.Text.Encoding.UTF8.GetString(receiveBuffer).Contains("Team "))
                    {
                        int Message = int.Parse(System.Text.Encoding.UTF8.GetString(receiveBuffer).Replace("Team ", ""));
                        SteamMatchmaking.SetLobbyMemberData(currentLobby, "team", Message.ToString());
                        Debug.Log("myteam: " + Message);
                        if (Message == 0)
                        {
                            RedTeam.Add(personalCSteamID);
                        }
                        else
                        {
                            BlueTeam.Add(personalCSteamID);
                        }
                        GetOtherPlayerTeams();
                    }
                    else if (System.Text.Encoding.UTF8.GetString(receiveBuffer).Contains("Playing Map "))
                    {
                        int Message = int.Parse(System.Text.Encoding.UTF8.GetString(receiveBuffer).Replace("Playing Map ", ""));
                        votedMap = Message;
                        StartMapMulti();
                    }
                    else
                    {
                        (Vector3 toPlayerPosition, Vector3 toPlayerRotation, byte animationState) = DecompressBytesToData(receiveBuffer);
                        if (Connections.Contains(remoteSteamID))
                        {
                            GameObject otherPlayer = GameObject.Find(remoteSteamID.ToString());
                            Animator otherPlayerAnimator = otherPlayer.GetComponentInChildren<Animator>();
                            PlayerPrefabValues otherPlayerValues = otherPlayer.GetComponent<PlayerPrefabValues>();

                            otherPlayer.transform.eulerAngles = toPlayerRotation;

                            if (playerSmoothings.ContainsKey(remoteSteamID))
                            {
                                StopCoroutine(playerSmoothings[remoteSteamID]);
                            }

                            if (otherPlayer.transform.position != PlayerPrefab.transform.position)
                            {
                                Coroutine smoothingCoroutine = StartCoroutine(smoothUntil(otherPlayer, toPlayerPosition));
                                playerSmoothings[remoteSteamID] = smoothingCoroutine;
                            }
                            else
                            {
                                otherPlayer.transform.position = toPlayerPosition;
                            }

                            if (animationState == 0)
                            {
                                if (otherPlayerValues.LastAnimation != 0)
                                {
                                    otherPlayerValues.LastAnimation = 0L;
                                    otherPlayerAnimator.SetTrigger("Idle");
                                }
                            }
                            else if (animationState == 1)
                            {
                                if (otherPlayerValues.LastAnimation != 1)
                                {
                                    otherPlayerValues.LastAnimation = 1L;
                                    otherPlayerAnimator.SetTrigger("Jump");
                                }
                            }
                            else if (animationState == 2)
                            {
                                if (otherPlayerValues.LastAnimation != 2)
                                {
                                    otherPlayerValues.LastAnimation = 2L;
                                    otherPlayerAnimator.SetTrigger("WALK frw");
                                }
                            }
                            else if (animationState == 3)
                            {
                                if (otherPlayerValues.LastAnimation != 3)
                                {
                                    otherPlayerValues.LastAnimation = 3L;
                                    otherPlayerAnimator.SetTrigger("WALK bkw");
                                }
                            }
                            else if (animationState == 4)
                            {
                                if (otherPlayerValues.LastAnimation != 4)
                                {
                                    otherPlayerValues.LastAnimation = 4L;
                                    otherPlayerAnimator.SetTrigger("RUN frw");
                                }
                            }
                            else if (animationState == 5)
                            {
                                if (otherPlayerValues.LastAnimation != 5)
                                {
                                    otherPlayerValues.LastAnimation = 5L;
                                    otherPlayerAnimator.SetTrigger("RUN bkw");
                                }
                            }
                            else if (animationState == 6)
                            {
                                if (otherPlayerValues.LastAnimation != 6)
                                {
                                    otherPlayerValues.LastAnimation = 6L;
                                    otherPlayerAnimator.SetTrigger("Hanging");
                                }
                            }
                            else if (animationState == 10)
                            {
                                if (otherPlayerValues.LastAnimation != 10)
                                {
                                    otherPlayerValues.LastAnimation = 10L;
                                    otherPlayerAnimator.SetTrigger("Emote1");
                                }
                            }
                            else if (animationState == 11)
                            {
                                if (otherPlayerValues.LastAnimation != 11)
                                {
                                    otherPlayerValues.LastAnimation = 11L;
                                    otherPlayerAnimator.SetTrigger("Emote2");
                                }
                            }
                            else if (animationState == 12)
                            {
                                if (otherPlayerValues.LastAnimation != 12)
                                {
                                    otherPlayerValues.LastAnimation = 12L;
                                    otherPlayerAnimator.SetTrigger("Emote3");
                                }
                            }
                            else if (animationState == 13)
                            {
                                if (otherPlayerValues.LastAnimation != 13)
                                {
                                    otherPlayerValues.LastAnimation = 13L;
                                    otherPlayerAnimator.SetTrigger("Emote4");
                                }
                            }
                        }
                    }
                }
            }


            if (Input.GetKeyDown(KeyCode.T))
            {
                if (GameObject.Find("Options") == null)
                {
                    Chat.SetActive(true);
                    NewMessage.SetActive(false);
                    ChatInput.ActivateInputField();
                }
            }

            if (GameObject.Find("Chat") != null)
            {
                // Sometimes the messsage is sent twice. This is to ensure it does not happen
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (canSendMessage)
                    {
                        string userName = SteamFriends.GetFriendPersonaName(personalCSteamID);
                        byte[] messageBuffer;
                        if (ChatInput.text.Length > 200)
                        {
                            messageBuffer = System.Text.Encoding.UTF8.GetBytes(userName + ": " + ChatInput.text.Substring(0, 200));
                        }
                        else
                        {
                            messageBuffer = System.Text.Encoding.UTF8.GetBytes(userName + ": " + ChatInput.text);
                        }
                        SteamMatchmaking.SendLobbyChatMsg(currentLobby, messageBuffer, messageBuffer.Length);
                        canSendMessage = false;
                        ChatInput.text = "";
                        Chat = GameObject.Find("Chat");
                        Chat.SetActive(false);
                        EventSystem.current.SetSelectedGameObject(null);
                        justSentMessage = true;
                    }
                }

            }

            if (justSentMessage && Input.GetKeyUp(KeyCode.Return))
            {
                canSendMessage = true;
            }

            if (fading)
            {
                fadeUntilTimer += Time.deltaTime;
                if (fadeUntilTimer >= 5f)
                {
                    fadeTimer += Time.deltaTime;
                    float t = Mathf.Clamp01(fadeTimer / fadeDuration);
                    NewMessage.GetComponent<TextMeshProUGUI>().color = Color.Lerp(startColor, endColor, t);

                    if (fadeTimer >= fadeDuration)
                    {
                        fading = false;
                    }
                }
            }

            if (coolDownActive)
            {
                Player.transform.position = Vector3.zero;
            }

            if (WinScreen.activeSelf && !wonGame)
            {
                wonGame = true;
                Debug.Log("You won!!! Ws IN THE CHAT");


                double dateFinish = Math.Round((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds);
                byte[] data = System.Text.Encoding.UTF8.GetBytes("Finished " + dateFinish.ToString());

                if (currentGameLeaderboard.Count <= 0)
                {
                    string message = "Warping back to lobby in 1 minute.";

                    currentChats.Add(message + "\n");
                    UpdateChatList();
                    Invoke("BackToLobby", 60);
                }

                currentGameLeaderboard.Add(personalCSteamID, dateFinish);

                for (int i = 0; i < Connections.Count; i++)
                {
                    if (Connections[i] != personalCSteamID)
                    {
                        SteamNetworking.SendP2PPacket(Connections[i], data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
                    }
                }


                if (currentGameLeaderboard.Count == currentMembers.Count)
                {
                    string message = "Warping to lobby now.";

                    currentChats.Add(message + "\n");
                    UpdateChatList();
                    BackToLobby();
                }

            }

            if (isHost)
            {
                membersReady = IDofReadyMembers.Count;
                if (!votingMenu.activeSelf)
                {
                    if (membersReady < currentMembers.Count)
                    {
                        startVoting.text = "Waiting for other players to ready up (" + membersReady.ToString() + "/" + currentMembers.Count + ")";
                    }
                    else
                    {
                        startVoting.text = "Press E to start";
                        if (Input.GetKeyDown(KeyCode.E) && !Chat.activeSelf && !OptionsScreen.activeSelf)
                        {
                            startVotingObject.SetActive(false);
                            ModeVotingMenu.SetActive(true);
                            Cursor.lockState = CursorLockMode.None;
                            Cursor.visible = true;
                            StartVotingMode();
                        }
                    }
                }
                if (votedModes.Sum() == membersVoting)
                {
                    int maxVotes = votedModes.Max();
                    List<int> maxVotedModes = new List<int>();
                    int votedMode;

                    for (int i = 0; i < votedModes.Count; i++)
                    {
                        if (votedModes[i] == maxVotes)
                        {
                            maxVotedModes.Add(i);
                        }
                    }
                    if (maxVotedModes.Count > 0)
                    {
                        int randomMode = rng.Next(maxVotedModes.Count);
                        votedMode = maxVotedModes[randomMode] + 1;
                    }
                    else
                    {
                        votedMode = maxVotedModes[0] + 1;
                    }
                    maxVotedModes.Clear();
                    votedModes.Clear();
                    votedModes = new List<int> { 0, 0, 0, 0 };

                    SteamMatchmaking.SetLobbyData(currentLobby, "mode", votedMode.ToString());

                    byte[] data = System.Text.Encoding.UTF8.GetBytes("ChosenMode " + votedMode.ToString());

                    for (int i = 0; i < Connections.Count; i++)
                    {
                        if (Connections[i] != personalCSteamID)
                        {
                            Debug.Log("Sending mode to a user");
                            SteamNetworking.SendP2PPacket(Connections[i], data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
                        }
                    }

                    currentLobbyMode = votedMode;
                    if (votedMode == 3 || votedMode == 4)
                    {
                        DecideTeams();
                        UpdateMemberList();
                    }
                    Debug.Log("Map voting completed, need to vote map");
                    StartVoting();
                }
                if (votedMaps.Sum() == membersVoting)
                {
                    int maxVotes = votedMaps.Max();
                    List<int> maxVotedMaps = new List<int>();

                    for (int i = 0; i < votedMaps.Count; i++)
                    {
                        if (votedMaps[i] == maxVotes)
                        {
                            maxVotedMaps.Add(i);
                        }
                    }
                    if (maxVotedMaps.Count > 0)
                    {
                        int randomMode = rng.Next(maxVotedMaps.Count);
                        votedMap = maxVotedMaps[randomMode] + 3;
                    }
                    else
                    {
                        votedMap = maxVotedMaps[0] + 3;
                    }
                    maxVotedMaps.Clear();
                    Debug.Log("Voted map: " + votedMap);

                    SteamMatchmaking.SetLobbyData(currentLobby, "state", "starting");
                    SteamMatchmaking.SetLobbyData(currentLobby, "map", votedMap.ToString());

                    votedMaps.Clear();
                    votedMaps = new List<int> { 0, 0, 0, 0 };

                    membersReady = 1;
                    IDofReadyMembers.Clear();

                    byte[] messageBuffer = System.Text.Encoding.UTF8.GetBytes("Game starting in 10 seconds");
                    SteamMatchmaking.SendLobbyChatMsg(currentLobby, messageBuffer, messageBuffer.Length);

                    Invoke("LaunchMap", 10.0f);
                }
            }
            else
            {
                if (!votingMenu.activeSelf)
                {
                    if (readyUpObject.activeSelf && !Chat.activeSelf && !OptionsScreen.activeSelf)
                    {
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            readyUpObject.SetActive(false);
                            Ready();
                        }
                    }
                }
            }
        }

        if (SceneManager.GetActiveScene().name != "Main Menu" && SceneManager.GetActiveScene().name != "Preload")
        {

            if (!Player.activeSelf)
            {
                Debug.Log("Showing Player GameObject!");
                Player.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!optionsOpened)
                {
                    GameObject Chat = GameObject.Find("Chat");
                    GameObject Win = GameObject.Find("WinMenu");
                    if (Chat != null)
                    {
                        Chat = GameObject.Find("Chat");
                        Chat.SetActive(false);
                        EventSystem.current.SetSelectedGameObject(null);

                    }
                    else if (Win == null && !votingMenu.activeSelf && !ModeVotingMenu.activeSelf)
                    {
                        Debug.Log("Opened Options");

                        OptionsScreen.SetActive(true);
                        optionsOpened = true;
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                        if (Memberlist != null)
                        {
                            Memberlist.SetActive(false);
                        }
                    }
                }
                else
                {
                    OptionsScreen.SetActive(false);
                    optionsOpened = false;

                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    Time.timeScale = 1f;

                    if (Memberlist != null && !GameOverObject.activeSelf)
                    {
                        Memberlist.SetActive(true);
                    }
                }
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Make sure the player is not active and is at 0,0,0 when in the Main Menu. This is to ensure that the player starts at 0,0,0 when a map is loaded.
            if (Player.activeSelf)
            {
                Player.SetActive(false);
            }
        }
    }

    IEnumerator smoothUntil(GameObject otherPlayer, Vector3 toPlayerPosition)
    {
        while (true)
        {
            if (otherPlayer == null)
            {
                break;
            }

            float distance = Vector3.Distance(otherPlayer.transform.position, toPlayerPosition);

            otherPlayer.transform.position = Vector3.MoveTowards(otherPlayer.transform.position, toPlayerPosition, distance * 10 * Time.fixedDeltaTime);
            yield return new WaitForSeconds(0.007f);
        }
    }

    public void DecideTeams()
    {
        Debug.Log("Deciding Teams");

        List<CSteamID> MembersCopy = currentMembers.Keys.ToList();
        int team = 0;
        byte[] data;
        while (MembersCopy.Count > 0)
        {

            int rand1 = rng.Next(MembersCopy.Count);
            if (team == 0)
            {
                data = System.Text.Encoding.UTF8.GetBytes("Team 0");
                RedTeam.Add(MembersCopy[rand1]);
            }
            else
            {
                data = System.Text.Encoding.UTF8.GetBytes("Team 1");
                BlueTeam.Add(MembersCopy[rand1]);
            }
            if (MembersCopy[rand1] == personalCSteamID)
            {
                Debug.Log("My team (HOST) : " + team);
                SteamMatchmaking.SetLobbyMemberData(currentLobby, "team", team.ToString());
            }
            else
            {
                SteamNetworking.SendP2PPacket(MembersCopy[rand1], data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
            }

            if (team == 0)
            {
                team = 1;
            }
            else
            {
                team = 0;
            }

            MembersCopy.RemoveAt(rand1);
        }
    }

    public void LaunchMap()
    {
        Debug.Log("voting. I love democracy. Jk. This ain't democratic because the host still needs to start the game. Like if the president could choose to not launch the next vote. He would be in charge forever. Thnk about that.");

        SteamMatchmaking.SetLobbyData(currentLobby, "state", "playing");
        Debug.Log("Playing Map: " + votedMap);
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Playing Map " + votedMap);

        for (int i = 0; i < Connections.Count; i++)
        {
            if (Connections[i] != personalCSteamID)
            {
                SteamNetworking.SendP2PPacket(Connections[i], data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
            }
        }

        StartMapMulti();
    }

    public void reCalculateTeamPoints()
    {
        int newRedTeamPoints = 0;
        int newBlueTeamPoints = 0;
        for (int i = 0; i < currentMembers.Count; i++)
        {
            if (RedTeam.Contains(currentMembers.ElementAt(i).Key))
            {
                newRedTeamPoints += currentMembers.ElementAt(i).Value;
            }
            else if (BlueTeam.Contains(currentMembers.ElementAt(i).Key))
            {
                newBlueTeamPoints += currentMembers.ElementAt(i).Value;
            }
        }
        redTeamTotal = newRedTeamPoints;
        blueTeamTotal = newBlueTeamPoints;
    }

    public void ShowFinalLeaderboard()
    {
        Debug.Log("Printing final leaderboard");

        string memberlist = "Leaderboard\n";

        if (currentLobbyMode == 3 || currentLobbyMode == 4)
        {
            memberlist += "<color=\"red\">Red<color=\"white\">: " + redTeamTotal.ToString() + "\n";
            memberlist += "<color=\"blue\">Blue<color=\"white\">: " + blueTeamTotal.ToString() + "\n";
            memberlist += "\n";
        }

        foreach (var member in currentMembers.OrderByDescending(x => x.Value))
        {
            Debug.Log("mode: " + currentLobbyMode);
            if (currentLobbyMode == 3 || currentLobbyMode == 4)
            {
                Debug.Log("test 1");
                if (RedTeam.Contains(member.Key))
                {
                    Debug.Log("test 2");
                    memberlist += "<color=\"red\">" + SteamFriends.GetFriendPersonaName(member.Key) + "<color=\"white\">: " + member.Value + "\n";
                }
                else if (BlueTeam.Contains(member.Key))
                {
                    Debug.Log("test 3");
                    memberlist += "<color=\"blue\">" + SteamFriends.GetFriendPersonaName(member.Key) + "<color=\"white\">: " + member.Value + "\n";
                }
                else
                {
                    Debug.Log("test 4");
                    memberlist += SteamFriends.GetFriendPersonaName(member.Key) + ": " + member.Value + "\n";
                }
            }
            else
            {
                memberlist += SteamFriends.GetFriendPersonaName(member.Key) + ": " + member.Value + "\n";
            }

        }

        FinalLeaderboard.text = memberlist;
    }

    public bool CalculateLeaderBoard()
    {

        var ordered = currentGameLeaderboard.OrderBy(x => x.Value);

        Debug.Log("Mode " + currentLobbyMode);
        switch (currentLobbyMode)
        {
            case 1:
                CSteamID winner = ordered.ElementAt(0).Key;
                int newScore = currentMembers[winner] += 1;
                currentMembers[winner] = newScore;

                if (winner == personalCSteamID)
                {
                    SteamMatchmaking.SetLobbyMemberData(currentLobby, "score", newScore.ToString());
                }

                UpdateMemberList();

                if (newScore == 3)
                {
                    return true;
                }
                break;
            case 2:
                int loopLenght;
                bool playerWon = false;

                if (currentGameLeaderboard.Count >= 3)
                {
                    loopLenght = 3;
                }
                else
                {
                    loopLenght = currentGameLeaderboard.Count;
                }

                for (int i = 3; i > 3 - loopLenght; i--)
                {
                    winner = ordered.ElementAt(3 - i).Key;
                    newScore = currentMembers[winner] += i;
                    currentMembers[winner] = newScore;

                    if (winner == personalCSteamID)
                    {
                        SteamMatchmaking.SetLobbyMemberData(currentLobby, "score", newScore.ToString());
                    }

                    if (newScore >= 9)
                    {
                        playerWon = true;
                    }
                }

                UpdateMemberList();

                if (playerWon)
                {
                    return true;
                }
                break;
            case 3:
                winner = ordered.ElementAt(0).Key;
                newScore = currentMembers[winner] += 1;
                currentMembers[winner] = newScore;

                if (winner == personalCSteamID)
                {
                    SteamMatchmaking.SetLobbyMemberData(currentLobby, "score", newScore.ToString());
                }

                reCalculateTeamPoints();
                UpdateMemberList();

                if (redTeamTotal >= 6 || blueTeamTotal >= 6)
                {
                    return true;
                }
                break;
            case 4:

                if (currentGameLeaderboard.Count >= 3)
                {
                    loopLenght = 3;
                }
                else
                {
                    loopLenght = currentGameLeaderboard.Count;
                }

                for (int i = 3; i > 3 - loopLenght; i--)
                {
                    winner = ordered.ElementAt(3 - i).Key;
                    newScore = currentMembers[winner] += i;
                    currentMembers[winner] = newScore;

                    if (winner == personalCSteamID)
                    {
                        SteamMatchmaking.SetLobbyMemberData(currentLobby, "score", newScore.ToString());
                    }
                }

                reCalculateTeamPoints();
                UpdateMemberList();

                if (redTeamTotal >= 18 || blueTeamTotal >= 18)
                {
                    return true;
                }
                break;
            default:
                Debug.Log("Invalid Mode ?");
                break;
        }
        return false;
    }

    public void ReenterLobbyScene(Scene scene, LoadSceneMode mode)
    {
        WinScreen.SetActive(false);
        SceneManager.sceneLoaded -= ReenterLobbyScene;
    }

    public void BackToLobby()
    {
        CancelInvoke("BackToLobby");

        SceneManager.LoadScene("Default Map");
        SceneManager.sceneLoaded += ReenterLobbyScene;

        GameCanvas.SetActive(false);
        LobbyObjects.SetActive(true);

        if (CalculateLeaderBoard())
        {
            if (isHost)
            {
                SteamMatchmaking.SetLobbyData(currentLobby, "state", "lobby");
                IDofReadyMembers.Add(hostSteamID);
                startVotingObject.SetActive(true);
            }
            else
            {
                readyUpObject.SetActive(true);
            }

            switch (currentLobbyMode)
            {
                case 1:
                    CSteamID winner = currentMembers.OrderByDescending(x => x.Value).ElementAt(0).Key;
                    Debug.Log("GAME OVER, WINNER: " + SteamFriends.GetFriendPersonaName(winner));
                    WinnerTMP.text = "Winner:\n" + SteamFriends.GetFriendPersonaName(winner);
                    break;
                case 2:
                    List<CSteamID> winners = new List<CSteamID>();
                    string debugMessage = "GAME OVER, ";
                    string WinnerMessage = "";
                    for (int i = 0; i < currentMembers.Count(); i++)
                    {
                        if (currentMembers.ElementAt(i).Value >= 9)
                        {
                            winners.Add(currentMembers.ElementAt(i).Key);
                            debugMessage += "WINNER: " + SteamFriends.GetFriendPersonaName(currentMembers.ElementAt(i).Key) + ", ";
                        }
                    }
                    if (winners.Count() == 1)
                    {
                        WinnerMessage += "Winner:\n" + SteamFriends.GetFriendPersonaName(winners.ElementAt(0));
                    }
                    else
                    {
                        WinnerMessage += "Winners:\n";
                        for (int i = 0; i < winners.Count(); i++)
                        {
                            WinnerMessage += SteamFriends.GetFriendPersonaName(winners.ElementAt(i)) + "\n";
                        }
                    }
                    WinnerTMP.text = WinnerMessage;
                    Debug.Log(debugMessage);
                    break;
                case 3:
                    string winnerText = "";
                    if (redTeamTotal >= 6 && blueTeamTotal < 6)
                    {
                        winnerText += "Winner:\n<color=\"red\">Red Team<color=\"white\">";
                    }
                    else if (blueTeamTotal >= 6 && redTeamTotal < 6)
                    {
                        winnerText += "Winner:\n<color=\"blue\">Blue Team<color=\"white\">";
                    }
                    else
                    {
                        winnerText += "Tie!";
                    }
                    WinnerTMP.text = winnerText;
                    Debug.Log(winnerText);
                    break;
                case 4:
                    winnerText = "";
                    if (redTeamTotal >= 18 && blueTeamTotal < 18)
                    {
                        winnerText += "Winner:\n<color=\"red\">Red Team<color=\"white\">";
                    }
                    else if (blueTeamTotal >= 18 && redTeamTotal < 18)
                    {
                        winnerText += "Winner:\n<color=\"blue\">Blue Team<color=\"white\">";
                    }
                    else
                    {
                        winnerText += "Tie";
                    }
                    WinnerTMP.text = winnerText;
                    Debug.Log(winnerText);
                    break;
            }
            ShowFinalLeaderboard();
            GameOverObject.SetActive(true);
            Memberlist.SetActive(false);
            BlueTeam.Clear();
            RedTeam.Clear();
            redTeamTotal = 0;
            blueTeamTotal = 0;
            foreach (var member in currentMembers.Keys.ToList())
            {
                currentMembers[member] = 0;
            }
            UpdateMemberList();
        }
        else
        {
            if (isHost)
            {
                SteamMatchmaking.SetLobbyData(currentLobby, "state", "waiting");
                byte[] messageBuffer = System.Text.Encoding.UTF8.GetBytes("Next vote starting in 5 seconds");
                SteamMatchmaking.SendLobbyChatMsg(currentLobby, messageBuffer, messageBuffer.Length);
                Invoke("StartVoting", 5);
            }
        }

    }

    public void BackToMenu()
    {
        if (isInMultiplayer)
        {
            if (isHost)
            {
                CancelInvoke("LaunchMap");
            }
            CancelInvoke("StartVoting");

            SteamMatchmaking.LeaveLobby(currentLobby);
            lastLobby = currentLobby;
            currentLobby = CSteamID.Nil;

            foreach (Transform child in Players.transform)
            {
                Destroy(child.gameObject);
            }

            if (Connections.Count > 0)
            {
                for (int i = 0; i < Connections.Count; i++)
                {
                    Debug.Log("Removed connection with user: " + Connections[i]);
                    Connections.RemoveAt(i);
                }
            }

            RedTeam.Clear();
            BlueTeam.Clear();
            currentMembers.Clear();
            playerSmoothings.Clear();

            wonGame = false;
            coolDownActive = false;

            ListLobbies();
        }
        // Destroy all rooms


        Player.SetActive(false);

        OptionsScreen.SetActive(false);
        optionsOpened = false;
        WinScreen.SetActive(false);
        GameCanvas.SetActive(false);
        MPCanvas.SetActive(false);
        Canvas.SetActive(true);
        WaitingPlayersText.SetActive(false);
        RetryButton.SetActive(true);
        readyUpObject.SetActive(false);

        // Set timeScale to 1 before going back to the main menu, so other code works
        Time.timeScale = 1.0f;

        SceneManager.LoadScene("Main Menu");

        isInMultiplayer = false;

        votedInfo.text = "";
        LobbyObjects.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ResetPlayerPos(Scene scene, LoadSceneMode mode)
    {
        Player.transform.position = Vector3.zero;
    }

    private string GetMapName(int index)
    {
        switch (index)
        {
            case 2:
                return "Lobby";
            case 3:
                return "Tutorial";
            case 4:
                return "Harbour 2093";
            case 5:
                return "Prism";
            case 6:
                return "Rooftops";
            default:
                return "";
        }
    }

    private string GetModeName(int index)
    {
        switch (index)
        {
            case 0:
                return "Starting";
            case 1:
                return "Wins Normal";
            case 2:
                return "Points Normal";
            case 3:
                return "Teams Wins";
            case 4:
                return "Teams Points";
            default:
                return "";
        }
    }

    private byte[] CompressDataToBytes(Vector3 posVector, Vector3 rotEuler, byte animationState)
    {
        // 9 byte minipackets

        byte[] bytes = new byte[9];

        short positionX = (short)Math.Round(posVector.x * 10);

        bytes[0] = (byte)(positionX & 0xFF);
        bytes[1] = (byte)((positionX >> 8) & 0xFF);

        short positionY = (short)Math.Round(posVector.y * 10);

        bytes[2] = (byte)(positionY & 0xFF);
        bytes[3] = (byte)((positionY >> 8) & 0xFF);

        short positionZ = (short)Math.Round(posVector.z * 10);

        bytes[4] = (byte)(positionZ & 0xFF);
        bytes[5] = (byte)((positionZ >> 8) & 0xFF);

        short rotationY = (short)(rotEuler.y);


        bytes[6] = (byte)(rotationY & 0xFF);
        bytes[7] = (byte)((rotationY >> 8) & 0xFF);

        bytes[8] = animationState;




        BitArray bitArray = new BitArray(48);

        return bytes;
    }

    public static (Vector3 position, Vector3 rotation, byte animationState) DecompressBytesToData(byte[] bytes)
    {
        if (bytes.Length < 9)
        {
            throw new ArgumentException("Byte array must be at least 9 bytes long.");
        }

        // Extract position (X, Y, Z)


        short positionXshort = (short)((bytes[1] << 8) | bytes[0]);
        short positionYshort = (short)((bytes[3] << 8) | bytes[2]);
        short positionZshort = (short)((bytes[5] << 8) | bytes[4]);

        int positionXint = positionXshort;
        int positionYint = positionYshort;
        int positionZint = positionZshort;

        float positionXfloat = (float)positionXint / 10;
        float positionYfloat = (float)positionYint / 10;
        float positionZfloat = (float)positionZint / 10;

        Vector3 position = new Vector3(positionXfloat, positionYfloat, positionZfloat);

        short rotationY = (short)((bytes[7] << 8) | bytes[6]);
        Vector3 rotation = new Vector3(0, rotationY, 0); // Simplified example

        byte animationState = bytes[8];

        return (position, rotation, animationState);
    }


    public void UpdateMemberList()
    {
        Debug.Log("Updating memberlist");

        string memberlist = "Leaderboard\n";

        if (currentLobbyMode == 3 || currentLobbyMode == 4)
        {
            memberlist += "<color=\"red\">Red<color=\"white\">: " + redTeamTotal.ToString() + "\n";
            memberlist += "<color=\"blue\">Blue<color=\"white\">: " + blueTeamTotal.ToString() + "\n";
            memberlist += "\n";
        }

        foreach (var member in currentMembers.OrderByDescending(x => x.Value))
        {
            Debug.Log("mode: " + currentLobbyMode);
            if (currentLobbyMode == 3 || currentLobbyMode == 4)
            {
                Debug.Log("test 1");
                if (RedTeam.Contains(member.Key))
                {
                    if (member.Key != personalCSteamID)
                    {
                        if (GameObject.Find(member.Key.ToString()) != null)
                        {
                            GameObject playerObject = GameObject.Find(member.Key.ToString());
                            TextMeshPro Nametag = playerObject.GetComponentInChildren<TextMeshPro>();
                            Nametag.text = "<color=\"red\">" + SteamFriends.GetFriendPersonaName(member.Key);
                        }
                    }
                    Debug.Log("test 2");
                    memberlist += "<color=\"red\">" + SteamFriends.GetFriendPersonaName(member.Key) + "<color=\"white\">: " + member.Value + "\n";
                }
                else if (BlueTeam.Contains(member.Key))
                {
                    if (member.Key != personalCSteamID)
                    {
                        if (GameObject.Find(member.Key.ToString()) != null)
                        {
                            GameObject playerObject = GameObject.Find(member.Key.ToString());
                            TextMeshPro Nametag = playerObject.GetComponentInChildren<TextMeshPro>();
                            Nametag.text = "<color=\"blue\">" + SteamFriends.GetFriendPersonaName(member.Key);
                        }
                    }
                    Debug.Log("test 3");
                    memberlist += "<color=\"blue\">" + SteamFriends.GetFriendPersonaName(member.Key) + "<color=\"white\">: " + member.Value + "\n";
                }
                else
                {
                    if (member.Key != personalCSteamID)
                    {
                        if (GameObject.Find(member.Key.ToString()) != null)
                        {
                            GameObject playerObject = GameObject.Find(member.Key.ToString());
                            TextMeshPro Nametag = playerObject.GetComponentInChildren<TextMeshPro>();
                            Nametag.text = SteamFriends.GetFriendPersonaName(member.Key);
                        }
                    }
                    Debug.Log("test 4");
                    memberlist += SteamFriends.GetFriendPersonaName(member.Key) + ": " + member.Value + "\n";
                }
            }
            else
            {
                if (member.Key != personalCSteamID)
                {
                    if (GameObject.Find(member.Key.ToString()) != null)
                    {
                        GameObject playerObject = GameObject.Find(member.Key.ToString());
                        TextMeshPro Nametag = playerObject.GetComponentInChildren<TextMeshPro>();
                        Nametag.text = SteamFriends.GetFriendPersonaName(member.Key);
                    }
                }
                memberlist += SteamFriends.GetFriendPersonaName(member.Key) + ": " + member.Value + "\n";
            }

        }


        Memberlist.GetComponent<TextMeshProUGUI>().text = memberlist;
    }

    public void UpdateChatList()
    {
        string chats = "";
        for (int i = 0; i < currentChats.Count; i++)
        {
            chats += currentChats[i];
        }
        Messages.GetComponent<TextMeshProUGUI>().text = chats;

        if (GameObject.Find("Chat") == null)
        {
            NewMessage.SetActive(true);
            NewMessage.GetComponent<TextMeshProUGUI>().color = startColor;
            NewMessage.GetComponent<TextMeshProUGUI>().text = currentChats[currentChats.Count - 1];
            fadeUntilTimer = 0f;
            fadeTimer = 0f;
            fading = true;
        }
    }


    public void OnLobbyListReceived(LobbyMatchList_t callback)
    {
        Debug.Log("Received lobby list.");

        uint numLobbies = callback.m_nLobbiesMatching;
        Debug.Log("Number of lobbies found: " + numLobbies);

        // Clear all the rooms before
        for (int i = 0; i < roomsTransform.childCount - 1; i++)
        {
            DestroyImmediate(roomsTransform.GetChild(1).gameObject);
        }

        float lastLobbyXpos = 0;

        for (int i = 0; i < numLobbies; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            string lobbyName = SteamMatchmaking.GetLobbyData(lobbyId, "name");
            string lobbyMap = GetMapName(int.Parse(SteamMatchmaking.GetLobbyData(lobbyId, "map"))); // Unused atm
            string lobbyMode = GetModeName(int.Parse(SteamMatchmaking.GetLobbyData(lobbyId, "mode")));

            int numMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyId);


            float screen_pixel_multiplier_x = 1928f / Screen.width;
            float screen_pixel_multiplier_y = 1080f / Screen.height;

            GameObject clonedObject = Instantiate(Room, roomsTransform);
            Vector3 clonedObject_pos = clonedObject.transform.position;
            clonedObject_pos.x = clonedObject.transform.position.x + lastLobbyXpos;
            clonedObject_pos.y = lastLobbyYpos / screen_pixel_multiplier_y;
            TextMeshProUGUI[] textMeshPros = clonedObject.GetComponentsInChildren<TextMeshProUGUI>();
            textMeshPros[0].text = lobbyName;
            textMeshPros[1].text = lobbyMode + " " + numMembers + "/5";

            Button join = clonedObject.GetComponent<Button>();

            join.onClick.AddListener(() => EnterLobby(lobbyId));
            clonedObject.transform.position = clonedObject_pos;

            lastLobbyXpos += 342 / screen_pixel_multiplier_x;

            Debug.Log("Lobby " + i + ": " + lobbyName + " (" + numMembers + " members)");
        }
    }

    public void ListLobbies()
    {

        SteamMatchmaking.AddRequestLobbyListStringFilter("rooftoprunner", "any", ELobbyComparison.k_ELobbyComparisonEqual);

        SteamMatchmaking.RequestLobbyList();

    }

    public void RefreshLobbies()
    {
        ListLobbies();
        EventSystem.current.SetSelectedGameObject(null);

    }

    private void OnLobbyChatRecieved(LobbyChatMsg_t callback)
    {
        CSteamID lobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        byte[] messageBuffer = new byte[4096]; // Adjust the size as needed
        EChatEntryType chatEntryType;
        int messageLength = SteamMatchmaking.GetLobbyChatEntry(lobbyID, (int)callback.m_iChatID, out var steamIDUser, messageBuffer, messageBuffer.Length, out chatEntryType);

        string message = System.Text.Encoding.UTF8.GetString(messageBuffer, 0, messageLength);

        currentChats.Add(message + "\n");
        UpdateChatList();
        Debug.Log("Received lobby message " + message);
    }


    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        CSteamID memberId = new CSteamID(callback.m_ulSteamIDUserChanged);

        EChatMemberStateChange stateChange = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;

        if (stateChange == EChatMemberStateChange.k_EChatMemberStateChangeEntered)
        {
            string memberName = SteamFriends.GetFriendPersonaName(memberId);
            Debug.Log(memberName + " joined the lobby.");
            currentChats.Add(memberName + " joined the room.\n");
            currentMembers.Add(memberId, 0);

            UpdateChatList();

            if (currentLobbyMode == 3 || currentLobbyMode == 4)
            {
                GetOtherPlayerTeams();
            }
            UpdateMemberList();

        }
        else if (stateChange == EChatMemberStateChange.k_EChatMemberStateChangeLeft)
        {
            // Check if the host has left, in that case, assign a new host.

            if (hostSteamID != SteamMatchmaking.GetLobbyOwner(currentLobby))
            {
                Debug.Log("The host has quit. Lobby broken");
                BackToMenu();

            }

            string memberName = SteamFriends.GetFriendPersonaName(memberId);
            if (RedTeam.Contains(memberId))
            {
                RedTeam.Remove(memberId);
            }
            else if (BlueTeam.Contains(memberId))
            {
                BlueTeam.Remove(memberId);
            }

            Debug.Log(memberName + " left the lobby.");
            currentChats.Add(memberName + " left the room.\n");
            currentMembers.Remove(memberId);
            Connections.Remove(memberId);
            if (isHost)
            {
                if (votedModes.Sum() > 0 || votedMaps.Sum() > 0 || votingMenu.activeSelf || ModeVotingMenu.activeSelf)
                {
                    membersVoting -= 1;
                }
            }

            Debug.Log("Destroying a GameObject");
            Destroy(GameObject.Find(memberId.ToString()));

            UpdateChatList();
            reCalculateTeamPoints();
            UpdateMemberList();

        }
    }

    private void CheckIfMemberWasReady(CSteamID memberId)
    {
        if (IDofReadyMembers.Contains(memberId))
        {
            IDofReadyMembers.Remove(memberId);
        }
    }

    public void OnLobbyCreated(LobbyCreated_t callback)
    {
        Debug.Log("Lobby created!");

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to create lobby. Error: " + callback.m_eResult);
            return;
        }

        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        string lobbyNameValue;

        if (lobbyName.text == "")
        {
            lobbyNameValue = SteamFriends.GetFriendPersonaName(personalCSteamID);
        }
        else
        {
            if (lobbyName.text.Length > 30)
            {
                lobbyNameValue = lobbyName.text.Substring(0, 30);
            }
            else
            {
                lobbyNameValue = lobbyName.text;
            }

        }

        CancelInvoke("RunInitialCooldown");
        InitialCountdown.SetActive(false);

        SteamMatchmaking.SetLobbyData(lobbyId, "mode", "0");
        SteamMatchmaking.SetLobbyData(lobbyId, "name", lobbyNameValue);
        SteamMatchmaking.SetLobbyData(lobbyId, "map", "2");
        SteamMatchmaking.SetLobbyData(lobbyId, "state", "lobby");
        SteamMatchmaking.SetLobbyData(lobbyId, "rooftoprunner", "any");
        currentLobbyMode = 0;
        currentLobby = lobbyId;
    }

    private void OnJoinLobbyCompleted(LobbyEnter_t pCallback)
    {
        Debug.Log("current lobby: " + currentLobby + ", last lobby: " + lastLobby);
        if (currentLobby != lastLobby)
        {
            CancelInvoke("RunInitialCooldown");
            InitialCountdown.SetActive(false);
        }
        Debug.Log("On Join Lobby Completed");
        hostSteamID = SteamMatchmaking.GetLobbyOwner(currentLobby);

        if (hostSteamID == personalCSteamID)
        {
            isHost = true;
        }
        else
        {
            isHost = false;
        }

        SteamMatchmaking.SetLobbyMemberData(currentLobby, "score", "0");
        SteamMatchmaking.SetLobbyMemberData(currentLobby, "team", "9");

        int numMembers = SteamMatchmaking.GetNumLobbyMembers(currentLobby);

        byte[] data = System.Text.Encoding.UTF8.GetBytes("establish");
        Debug.Log("Attempting to establish connection with the members of the lobby. If there are none, ignore this message.");

        for (int i = 0; i < numMembers; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(currentLobby, i);

            // Establish with all lobby members
            if (!currentMembers.ContainsKey(memberId))
            {
                int otherMemberScore = int.Parse(SteamMatchmaking.GetLobbyMemberData(currentLobby, memberId, "score"));

                currentMembers.Add(memberId, otherMemberScore);

                if (memberId != personalCSteamID)
                {
                    SteamNetworking.SendP2PPacket(memberId, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
                }
            }
        }

        Canvas.SetActive(false);
        WaitingPlayersText.SetActive(true);
        RetryButton.SetActive(false);

        string lobbyState = SteamMatchmaking.GetLobbyData(currentLobby, "state");
        currentlobbyMap = int.Parse(SteamMatchmaking.GetLobbyData(currentLobby, "map"));
        currentLobbyMode = int.Parse(SteamMatchmaking.GetLobbyData(currentLobby, "mode"));
        if (currentLobbyMode == 3 || currentLobbyMode == 4)
        {
            int team;
            GetOtherPlayerTeams();
            if (RedTeam.Count < BlueTeam.Count)
            {
                team = 0;
                RedTeam.Add(personalCSteamID);
            }
            else if (BlueTeam.Count < RedTeam.Count)
            {
                team = 1;
                BlueTeam.Add(personalCSteamID);
            }
            else
            {
                team = rng.Next(2);
                if (team == 0)
                {
                    RedTeam.Add(personalCSteamID);
                }
                else
                {
                    BlueTeam.Add(personalCSteamID);
                }
            }
            SteamMatchmaking.SetLobbyMemberData(currentLobby, "team", team.ToString());
            reCalculateTeamPoints();
        }

        SceneManager.sceneLoaded += EnterLobbyScene;

        if (lobbyState == "playing")
        {
            SceneManager.LoadScene(currentlobbyMap);
        }
        else
        {
            SceneManager.LoadScene(2);
        }

    }

    public void SendPackets()
    {
        AnimatorTransitionInfo transitionInfo = PlayerAnimator.GetAnimatorTransitionInfo(0);
        int transitionHash = transitionInfo.nameHash;
        //Debug.Log(transitionHash);
        AnimatorStateInfo stateInfo = PlayerAnimator.GetCurrentAnimatorStateInfo(0);

        byte animationState;
        // Check if the animator is in a specific state

        if (stateInfo.IsName("Jump") || transitionHash == 1937263074)
        {
            animationState = 1;
        }
        else if (stateInfo.IsName("WALK frw") || transitionHash == -1845481888)
        {
            animationState = 2;
        }
        else if (stateInfo.IsName("WALK bkw") || transitionHash == -1903600600)
        {
            animationState = 3;
        }
        else if (stateInfo.IsName("RUN frw") || transitionHash == 789918134)
        {
            animationState = 4;
        }
        else if (stateInfo.IsName("RUN bkw") || transitionHash == -1963026556)
        {
            animationState = 5;
        }
        else if (stateInfo.IsName("Hanging") || transitionHash == 30752475)
        {
            animationState = 6;
        }
        else if (stateInfo.IsName("Framed Dance") || transitionHash == -1294975956)
        {
            animationState = 10;
        }
        else if (stateInfo.IsName("Breakdance") || transitionHash == 1285470201)
        {
            animationState = 11;
        }
        else if (stateInfo.IsName("GooberDance") || transitionHash == 421632798)
        {
            animationState = 12;
        }
        else if (stateInfo.IsName("HipHop Dance") || transitionHash == -731044462)
        {
            animationState = 13;
        }
        else
        {
            animationState = 0;
        }

        //Debug.Log("self animationState: " + animationState);

        if (isInMultiplayer)
        {
            byte[] data = CompressDataToBytes(Player.transform.position, Player.transform.eulerAngles, animationState);

            if (lastPositionPakcet.Length > 0)
            {
                if (DecompressBytesToData(lastPositionPakcet) != DecompressBytesToData(data))
                {
                    //Debug.Log("last pos: " + DecompressBytesToData(lastPositionPakcet) + ", current pos: " + DecompressBytesToData(data));
                    for (int i = 0; i < Connections.Count; i++)
                    {
                        if (Connections[i] != personalCSteamID)
                        {
                            SteamNetworking.SendP2PPacket(Connections[i], data, (uint)data.Length, EP2PSend.k_EP2PSendUnreliableNoDelay);
                        }
                    }
                }
            }
            lastPositionPakcet = data;
            Invoke("SendPackets", 0.06f);
        }
    }

    private void EnterLobbyScene(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Loaded scene");

        string lobbyState = SteamMatchmaking.GetLobbyData(currentLobby, "state");
        Debug.Log("lobbyState: " + lobbyState);

        LobbyObjects.SetActive(true);

        isInMultiplayer = true;
        UpdateMemberList();
        SendPackets();

        MPCanvas.SetActive(true);
        MPManage.SetActive(true);
        Memberlist.SetActive(true);
        Chat.SetActive(false);
        NewMessage.SetActive(false);

        if (lobbyState == "starting")
        {
            string message = "Game is starting soon";

            currentChats.Add(message + "\n");
            UpdateChatList();
        }

        else if (lobbyState == "voting" || lobbyState == "mapvoting")
        {
            string message = "Waiting for all players to finish voting.";

            currentChats.Add(message + "\n");
            UpdateChatList();
        }
        else if (lobbyState == "lobby")
        {
            if (isHost)
            {
                IDofReadyMembers.Add(hostSteamID);
                startVotingObject.SetActive(true);
            }
            else
            {
                readyUpObject.SetActive(true);
            }

        }

        SceneManager.sceneLoaded -= EnterLobbyScene;
    }

    public void CreateLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 5);
    }

    private void EnterLobby(CSteamID lobbyId)
    {
        int numMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        if (numMembers < 5)
        {
            SteamMatchmaking.JoinLobby(lobbyId);
            currentLobby = lobbyId;
        }

    }

    public void OnP2PSessionRequest(P2PSessionRequest_t pCallback)
    {
        Debug.Log("Recieved connection request from: " + pCallback.m_steamIDRemote);
        SteamNetworking.AcceptP2PSessionWithUser(pCallback.m_steamIDRemote);
    }

    public void StartVotingMode()
    {
        SteamMatchmaking.SetLobbyData(currentLobby, "state", "voting");
        GameOverObject.SetActive(false);
        Memberlist.SetActive(true);

        byte[] messageBuffer = System.Text.Encoding.UTF8.GetBytes("Mode voting has started!");
        SteamMatchmaking.SendLobbyChatMsg(currentLobby, messageBuffer, messageBuffer.Length);

        byte[] data = System.Text.Encoding.UTF8.GetBytes("VotingMode");
        membersVoting = currentMembers.Count;
        for (int i = 0; i < Connections.Count; i++)
        {
            if (Connections[i] != personalCSteamID)
            {
                SteamNetworking.SendP2PPacket(Connections[i], data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
            }
        }
    }

    public void StartVoting()
    {
        Debug.Log("STARTING VOTING");
        SteamMatchmaking.SetLobbyData(currentLobby, "state", "mapvoting");

        byte[] messageBuffer = System.Text.Encoding.UTF8.GetBytes("Map voting has started!");
        SteamMatchmaking.SendLobbyChatMsg(currentLobby, messageBuffer, messageBuffer.Length);

        // Show voting menu for host aswell
        votingMenu.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        byte[] data = System.Text.Encoding.UTF8.GetBytes("VotingMap");
        membersVoting = currentMembers.Count;
        for (int i = 0; i < Connections.Count; i++)
        {
            if (Connections[i] != personalCSteamID)
            {
                SteamNetworking.SendP2PPacket(Connections[i], data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
            }
        }
    }

    public void Ready()
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Ready");
        SteamNetworking.SendP2PPacket(hostSteamID, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
        string userName = SteamFriends.GetFriendPersonaName(personalCSteamID);
        byte[] messageBuffer = System.Text.Encoding.UTF8.GetBytes(userName + " is ready!");
        SteamMatchmaking.SendLobbyChatMsg(currentLobby, messageBuffer, messageBuffer.Length);
    }

    public void VoteMode(int index)
    {
        if (!isHost)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes("VotedMode " + index.ToString());
            SteamNetworking.SendP2PPacket(hostSteamID, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
        }
        else
        {
            votedModes[index - 1]++;
        }

        string userName = SteamFriends.GetFriendPersonaName(personalCSteamID);
        byte[] messageBuffer = System.Text.Encoding.UTF8.GetBytes(userName + " has voted!");
        SteamMatchmaking.SendLobbyChatMsg(currentLobby, messageBuffer, messageBuffer.Length);

        votedInfoObject.SetActive(true);
        ModeVotingMenu.SetActive(false);
    }

    public void VoteMap(int index)
    {
        if (!isHost)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes("Voted " + index.ToString());
            SteamNetworking.SendP2PPacket(hostSteamID, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
        }
        else
        {
            votedMaps[index - 3]++;
        }

        string map = GetMapName(index);

        string userName = SteamFriends.GetFriendPersonaName(personalCSteamID);
        byte[] messageBuffer = System.Text.Encoding.UTF8.GetBytes(userName + " has voted!");
        SteamMatchmaking.SendLobbyChatMsg(currentLobby, messageBuffer, messageBuffer.Length);
        if (!isHost)
        {
            //votedInfo.text = "Your vote: " + map;
            votedInfo.text = "";
        }
        else
        {
            //votedInfo.text = "Your vote: " + map;
            votedInfo.text = "";
        }

        votedInfoObject.SetActive(true);
        votingMenu.SetActive(false);
    }

    public void RunInitialCooldown()
    {

        if (cooldown == 0)
        {
            coolDownActive = false;
            Player.transform.position = Vector3.zero;
            InitialCountdown.SetActive(false);
            GameCanvas.SetActive(true);
            Debug.Log("Go!");
        }
        else
        {
            InitialCountdown.GetComponent<TMP_Text>().text = cooldown.ToString();
            cooldown -= 1;
            Invoke("RunInitialCooldown", 1.0f);
        }
    }

    public void GetOtherPlayerTeams()
    {
        for (int i = 0; i < currentMembers.Count; i++)
        {
            Debug.Log("Getting Player Team");
            if (currentMembers.ElementAt(i).Key != personalCSteamID && !RedTeam.Contains(currentMembers.ElementAt(i).Key) && !BlueTeam.Contains(currentMembers.ElementAt(i).Key))
            {
                if (SteamMatchmaking.GetLobbyMemberData(currentLobby, currentMembers.ElementAt(i).Key, "team") != "")
                {
                    int memberTeam = int.Parse(SteamMatchmaking.GetLobbyMemberData(currentLobby, currentMembers.ElementAt(i).Key, "team"));
                    Debug.Log(memberTeam);
                    if (memberTeam == 0 || memberTeam == 1)
                    {
                        if (memberTeam == 0)
                        {
                            RedTeam.Add(currentMembers.ElementAt(i).Key);
                        }
                        else if (memberTeam == 1)
                        {
                            BlueTeam.Add(currentMembers.ElementAt(i).Key);
                        }
                    }
                }
            }
        }

        Debug.Log("Getting other players teams.. SumLenght: " + (BlueTeam.Count + RedTeam.Count) + "currentmember lenght: " + currentMembers);

        if ((BlueTeam.Count + RedTeam.Count) == currentMembers.Count)
        {
            Debug.Log("Everyone is in a team.");
            UpdateMemberList();
        }
        else
        {
            Invoke("GetOtherPlayerTeams", 0.25f);
        }
    }

    public void StartMapMulti()
    {
        SceneManager.LoadScene(votedMap);
        Debug.Log("Entering map...");

        LobbyObjects.SetActive(false);
        currentLobbyMode = int.Parse(SteamMatchmaking.GetLobbyData(currentLobby, "mode"));
        Debug.Log("lobby mode when starting game: " + currentLobbyMode);
        currentGameLeaderboard.Clear();
        wonGame = false;

        InitialCountdown.SetActive(true);
        coolDownActive = true;
        cooldown = 5;

        RunInitialCooldown();
    }
}
