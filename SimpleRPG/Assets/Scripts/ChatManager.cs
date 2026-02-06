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
    [SerializeField] private Color systemMessageColor = new Color(1f, 0.8f, 0.4f); // 노란색 계열

    private List<ChatMessage> _messages = new List<ChatMessage>();
    private GameTimeManager _gameTime;

    private void Awake()
    {
        if (chatContent == null)
        {
            // 자동으로 찾기 시도
            GameObject scrollView = GameObject.Find("Scroll View");
            if (scrollView != null)
            {
                Transform content = scrollView.transform.Find("Content");
                if (content != null)
                    chatContent = content;
            }
        }

        _gameTime = FindFirstObjectByType<GameTimeManager>();
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

    /// <summary>
    /// 메시지 추가 및 UI 업데이트
    /// </summary>
    private void AddMessage(ChatMessage message)
    {
        if (chatContent == null)
        {
            Debug.LogWarning("[ChatManager] chatContent가 설정되지 않았습니다.");
            return;
        }

        _messages.Add(message);

        // 최대 메시지 수 초과 시 오래된 메시지 제거
        while (_messages.Count > maxMessages)
        {
            if (chatContent.childCount > 0)
            {
                Transform oldest = chatContent.GetChild(0);
                if (oldest != null)
                    Destroy(oldest.gameObject);
            }
            _messages.RemoveAt(0);
        }

        // UI에 메시지 아이템 생성
        CreateMessageItem(message);
    }

    /// <summary>
    /// 메시지 UI 아이템 생성
    /// </summary>
    private void CreateMessageItem(ChatMessage message)
    {
        GameObject itemObj = Instantiate(messageItemPrefab, chatContent);
        TextMeshProUGUI textComponent = itemObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent == null)
        {
            Debug.LogWarning("[ChatManager] 메시지 아이템에 TextMeshProUGUI 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        // 메시지 포맷팅
        string formattedMessage = FormatMessage(message);
        textComponent.text = formattedMessage;
        textComponent.color = message.type == ChatMessageType.System ? systemMessageColor : normalMessageColor;

        // Layout Element 추가 (필요시)
        LayoutElement layout = itemObj.GetComponent<LayoutElement>();
        if (layout == null)
            layout = itemObj.AddComponent<LayoutElement>();
        layout.preferredHeight = -1;
        layout.flexibleHeight = 0;
    }

    /// <summary>
    /// 메시지 포맷팅
    /// </summary>
    private string FormatMessage(ChatMessage message)
    {
        if (message.type == ChatMessageType.System)
        {
            // 시스템 메시지: [시스템] 메시지 내용
            return string.IsNullOrEmpty(message.timestamp) 
                ? $"[시스템] {message.content}" 
                : $"[{message.timestamp}] [시스템] {message.content}";
        }
        else
        {
            // 일반 메시지: [시간] 발신자: 메시지 내용
            if (string.IsNullOrEmpty(message.senderName))
                return string.IsNullOrEmpty(message.timestamp) 
                    ? message.content 
                    : $"[{message.timestamp}] {message.content}";
            else
                return string.IsNullOrEmpty(message.timestamp) 
                    ? $"{message.senderName}: {message.content}" 
                    : $"[{message.timestamp}] {message.senderName}: {message.content}";
        }
    }

    /// <summary>
    /// 모든 메시지 제거
    /// </summary>
    public void ClearMessages()
    {
        _messages.Clear();
        if (chatContent != null)
        {
            for (int i = chatContent.childCount - 1; i >= 0; i--)
            {
                Transform child = chatContent.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 최근 메시지 목록 가져오기 (컨텍스트용)
    /// </summary>
    public List<ChatMessage> GetRecentMessages(int count)
    {
        var recent = new List<ChatMessage>();
        int startIndex = Mathf.Max(0, _messages.Count - count);
        
        for (int i = startIndex; i < _messages.Count; i++)
        {
            recent.Add(_messages[i]);
        }
        
        return recent;
    }
}
