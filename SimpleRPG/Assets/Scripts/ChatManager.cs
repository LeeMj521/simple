using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 채팅 메시지 타입
/// </summary>
public enum ChatMessageType
{
    Normal,     // 일반 메시지
    System      // 시스템 메시지
}

/// <summary>
/// 채팅 메시지 데이터
/// </summary>
public class ChatMessage
{
    public ChatMessageType type;
    public string senderName;
    public string content;
    public string timestamp;

    public ChatMessage(ChatMessageType messageType, string sender, string message, string time = "")
    {
        type = messageType;
        senderName = sender;
        content = message;
        timestamp = time;
    }
}

/// <summary>
/// 채팅 메시지 관리. 일반 메시지와 시스템 메시지를 표시.
/// </summary>
public class ChatManager : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("Scroll View의 Content")]
    [SerializeField] private Transform chatContent;
    [Tooltip("메시지 아이템 프리팹")]
    [SerializeField] private GameObject messageItemPrefab;
    [Tooltip("최대 표시 메시지 수 (초과 시 오래된 메시지 제거)")]
    [SerializeField] private int maxMessages = 100;

    [Header("메시지 스타일")]
    [Tooltip("일반 메시지 색상")]
    [SerializeField] private Color normalMessageColor = Color.white;
    [Tooltip("시스템 메시지 색상")]
    [SerializeField] private Color systemMessageColor = new Color(1f, 0.8f, 0.4f);

    [Header("채팅 버블")]
    [Tooltip("유저 머리 위 버블 표시 시간(초)")]
    [SerializeField] private float chatBubbleDuration = 4f;

    [Header("플레이어 채팅")]
    [Tooltip("플레이어 UserObject (이름·채팅 버블 등 여기서 사용)")]
    [SerializeField] private UserObject playerUserObject;
    [Tooltip("인풋필드+전송버튼 부모. 비활성 시 숨김, 엔터/클릭 시 활성화")]
    [SerializeField] private GameObject chatInputFieldRoot;
    [Tooltip("채팅 입력 필드")]
    [SerializeField] private TMP_InputField chatInputField;
    [Tooltip("NPC 답장 연쇄용. 비어 있으면 씬에서 찾음")]
    [SerializeField] private NPCChatSystem npcChatSystem;

    private List<ChatMessage> _messages = new List<ChatMessage>();
    private GameTimeManager _gameTime;
    private bool _chatGroupJustClosed;

    public float ChatBubbleDuration => chatBubbleDuration;

    private void Awake()
    {
        if (chatContent == null)
        {
            var content = GameObject.Find("Scroll View")?.transform.Find("Content");
            if (content != null) chatContent = content;
        }
        _gameTime = FindFirstObjectByType<GameTimeManager>();
        if (npcChatSystem == null) npcChatSystem = FindFirstObjectByType<NPCChatSystem>();
        SetChatGroupActive(false);
    }

    private void Start()
    {
        StartCoroutine(HideChatGroupAfterFirstFrame());
    }

    private IEnumerator HideChatGroupAfterFirstFrame()
    {
        yield return null;
        SetChatGroupActive(false);
    }

    private void Update()
    {
        if (chatInputFieldRoot == null) return;
        if (_chatGroupJustClosed) { _chatGroupJustClosed = false; return; }

        bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (!enter) return;

        if (chatInputFieldRoot.activeSelf)
        {
            if (chatInputField != null) TrySendPlayerMessage(chatInputField.text);
            SetChatGroupActive(false);
            _chatGroupJustClosed = true;
        }
        else
        {
            SetChatGroupActive(true);
        }
    }

    public void OpenChatGroup() => SetChatGroupActive(true);
    public void CloseChatGroup() => SetChatGroupActive(false);

    private void SetChatGroupActive(bool active)
    {
        if (chatInputFieldRoot != null) chatInputFieldRoot.SetActive(active);
        if (active && chatInputField != null) chatInputField.ActivateInputField();
    }

    public void SendPlayerMessage()
    {
        if (chatInputField == null) return;
        TrySendPlayerMessage(chatInputField.text);
        SetChatGroupActive(false);
        _chatGroupJustClosed = true;
    }

    private void TrySendPlayerMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        text = text.Trim();

        string senderName = playerUserObject != null ? playerUserObject.DisplayName : "나";
        AddNormalMessage(senderName, text);
        if (playerUserObject != null && chatBubbleDuration > 0f)
            playerUserObject.ShowChatBubble(text, chatBubbleDuration);
        npcChatSystem?.OnMessageReceived("player", text);

        if (chatInputField != null)
        {
            chatInputField.text = "";
            chatInputField.ReleaseSelection();
        }
    }

    /// <summary>
    /// 일반 메시지 추가
    /// </summary>
    public void AddNormalMessage(string senderName, string message)
    {
        string time = _gameTime != null ? _gameTime.GetTimeString() : "";
        ChatMessage msg = new ChatMessage(ChatMessageType.Normal, senderName, message, time);
        AddMessage(msg);
    }

    /// <summary>
    /// 시스템 메시지 추가
    /// </summary>
    public void AddSystemMessage(string message)
    {
        string time = _gameTime != null ? _gameTime.GetTimeString() : "";
        ChatMessage msg = new ChatMessage(ChatMessageType.System, "", message, time);
        AddMessage(msg);
    }

    private void AddMessage(ChatMessage message)
    {
        if (chatContent == null) return;

        _messages.Add(message);
        while (_messages.Count > maxMessages)
        {
            if (chatContent.childCount > 0) Destroy(chatContent.GetChild(0).gameObject);
            _messages.RemoveAt(0);
        }
        CreateMessageItem(message);
    }

    private void CreateMessageItem(ChatMessage message)
    {
        var itemObj = Instantiate(messageItemPrefab, chatContent);
        var textComponent = itemObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent == null) return;

        textComponent.text = FormatMessage(message);
        textComponent.color = message.type == ChatMessageType.System ? systemMessageColor : normalMessageColor;
        if (itemObj.GetComponent<LayoutElement>() == null)
        {
            var layout = itemObj.AddComponent<LayoutElement>();
            layout.preferredHeight = -1;
            layout.flexibleHeight = 0;
        }
    }

    private string FormatMessage(ChatMessage message)
    {
        if (message.type == ChatMessageType.System)
            return string.IsNullOrEmpty(message.timestamp) ? $"[시스템] {message.content}" : $"[{message.timestamp}] [시스템] {message.content}";
        string namePart = string.IsNullOrEmpty(message.senderName) ? "" : $"{message.senderName}: ";
        return string.IsNullOrEmpty(message.timestamp) ? $"{namePart}{message.content}" : $"[{message.timestamp}] {namePart}{message.content}";
    }

    public void ClearMessages()
    {
        _messages.Clear();
        if (chatContent == null) return;
        for (int i = chatContent.childCount - 1; i >= 0; i--)
            Destroy(chatContent.GetChild(i).gameObject);
    }

    public List<ChatMessage> GetRecentMessages(int count)
    {
        int start = Mathf.Max(0, _messages.Count - count);
        return _messages.GetRange(start, _messages.Count - start);
    }
}
