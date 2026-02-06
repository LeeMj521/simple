using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// NPC 채팅 시스템 - 랜덤 주기마다 AI API를 사용하여 NPC 채팅 생성 및 답장 연쇄 처리
/// </summary>
public class NPCChatSystem : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private NPCManager npcManager;
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private GameTimeManager gameTime;
    [SerializeField] private AIAPIService aiAPIService;

    [Header("랜덤 주기 설정")]
    [Tooltip("랜덤 주기 범위 (초) - 최소값")]
    [SerializeField] private float randomCycleMin = 30f;
    [Tooltip("랜덤 주기 범위 (초) - 최대값")]
    [SerializeField] private float randomCycleMax = 120f;

    [Header("채팅 컨텍스트")]
    [Tooltip("최근 채팅 기록을 컨텍스트로 사용할 개수")]
    [SerializeField] private int contextMessageCount = 5;

    private float _nextRandomCycleTime;
    private bool _isResponseChainActive = false; // 답장 연쇄 진행 중 여부

    private void Awake()
    {
        if (npcManager == null)
            npcManager = FindFirstObjectByType<NPCManager>();
        if (chatManager == null)
            chatManager = FindFirstObjectByType<ChatManager>();
        if (gameTime == null)
            gameTime = FindFirstObjectByType<GameTimeManager>();
        if (aiAPIService == null)
            aiAPIService = FindFirstObjectByType<AIAPIService>();

        ResetRandomCycle();
    }

    private void Update()
    {
        // 답장 연쇄가 진행 중이면 랜덤 주기 정지
        if (_isResponseChainActive)
            return;

        // 랜덤 주기 체크
        if (Time.time >= _nextRandomCycleTime)
        {
            TryRandomNPCSpeak();
            ResetRandomCycle();
        }
    }

    /// <summary>
    /// 랜덤 주기 초기화
    /// </summary>
    private void ResetRandomCycle()
    {
        _nextRandomCycleTime = Time.time + Random.Range(randomCycleMin, randomCycleMax);
    }

    /// <summary>
    /// 랜덤 주기마다 NPC가 말할 확률 체크 및 실행
    /// </summary>
    private void TryRandomNPCSpeak()
    {
        if (npcManager == null || chatManager == null)
            return;

        // 접속 중인 NPC 목록 가져오기
        var onlineNPCs = GetOnlineNPCs();
        if (onlineNPCs.Count == 0){
            Debug.Log("[NPCChatSystem] 접속 중인 NPC가 없습니다.");
            return;
        }

        // 가중치 기반으로 NPC 선택
        string selectedNPCId = SelectNPCByWeight(onlineNPCs);
        if (string.IsNullOrEmpty(selectedNPCId)){
            Debug.Log("[NPCChatSystem] 가중치 기반으로 NPC 선택 실패");
            return;
        }

        // 선택된 NPC가 말할 확률 체크
        var npc = npcManager.GetNPC(selectedNPCId);
        if (npc == null){
            Debug.Log("[NPCChatSystem] NPC 데이터를 찾을 수 없습니다.");
            return;
        }

        float speakWeight = npc.GetSpeakWeight();
        float randomValue = Random.value;
        if (randomValue > speakWeight){
            Debug.Log($"[NPCChatSystem] {npc.name}는 말하려다 말하지 않음: {speakWeight}/{randomValue}");
            return;
        }
        Debug.Log($"[NPCChatSystem] {npc.name}는 말함: {speakWeight}/{randomValue}");

        // AI API로 채팅 생성
        GenerateNPCChat(selectedNPCId, npc, null, null);
    }

    /// <summary>
    /// 접속 중인 NPC 목록 가져오기
    /// </summary>
    private List<string> GetOnlineNPCs()
    {
        var onlineNPCs = new List<string>();
        if (npcManager == null)
            return onlineNPCs;

        var allNPCs = npcManager.GetLoadedNPCs();
        foreach (var kv in allNPCs)
        {
            if (kv.Value != null && kv.Value.isOnline)
                onlineNPCs.Add(kv.Key);
        }

        return onlineNPCs;
    }

    /// <summary>
    /// 가중치 기반으로 NPC 선택
    /// </summary>
    private string SelectNPCByWeight(List<string> onlineNPCs)
    {
        if (onlineNPCs.Count == 0)
            return null;

        if (npcManager == null)
            return null;

        // 가중치 계산
        float totalWeight = 0f;
        var weights = new Dictionary<string, float>();

        foreach (string npcId in onlineNPCs)
        {
            var npc = npcManager.GetNPC(npcId);
            if (npc == null)
                continue;

            float weight = npc.GetSpeakWeight();
            weights[npcId] = weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
            return onlineNPCs[Random.Range(0, onlineNPCs.Count)]; // 가중치가 모두 0이면 랜덤 선택

        // 가중치 기반 랜덤 선택
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var kv in weights)
        {
            currentWeight += kv.Value;
            if (randomValue <= currentWeight)
                return kv.Key;
        }

        return onlineNPCs[onlineNPCs.Count - 1]; // 폴백
    }

    /// <summary>
    /// AI API를 사용하여 NPC 채팅 생성
    /// </summary>
    private void GenerateNPCChat(string npcId, NPCData npc, string replyToMessage, string replyToSenderId)
    {
        // 답장 연쇄 시작
        if (!_isResponseChainActive)
        {
            _isResponseChainActive = true;
        }

        if (aiAPIService == null)
        {
            Debug.LogError("[NPCChatSystem] AIAPIService가 설정되지 않았습니다.");
            EndResponseChain();
            return;
        }

        // 최근 채팅 기록 가져오기 (컨텍스트용)
        string context = GetRecentChatContext();

        // AI 프롬프트 생성
        string prompt = BuildAIPrompt(npc, context, replyToMessage, replyToSenderId);

        // AI API 호출
        aiAPIService.GenerateChatMessage(
            prompt,
            npc.name,
            (chatMessage) => OnChatGenerated(npcId, npc.name, chatMessage),
            (error) => OnChatGenerationFailed(error)
        );
    }

    /// <summary>
    /// 채팅 생성 성공 콜백
    /// </summary>
    private void OnChatGenerated(string npcId, string npcName, string chatMessage)
    {
        if (chatManager != null)
        {
            chatManager.AddNormalMessage(npcName, chatMessage);
            CheckResponseChain(npcId, chatMessage);
        }
        if (npcManager != null && chatManager != null && chatManager.ChatBubbleDuration > 0f)
            npcManager.ShowChatBubbleForUser(npcId, chatMessage, chatManager.ChatBubbleDuration);
    }

    /// <summary>
    /// 채팅 생성 실패 콜백
    /// </summary>
    private void OnChatGenerationFailed(string error)
    {
        Debug.LogError($"[NPCChatSystem] AI API 호출 실패: {error}");
        EndResponseChain();
    }

    /// <summary>
    /// AI 프롬프트 생성
    /// </summary>
    private string BuildAIPrompt(NPCData npc, string context, string replyToMessage, string replyToSenderId)
    {
        StringBuilder prompt = new StringBuilder();

        prompt.AppendLine($"당신은 온라인 게임 채팅방에 있는 '{npc.name}'라는 플레이어입니다.");
        prompt.AppendLine($"성격 및 행동 타입: {npc.behaviorType}");
        prompt.AppendLine($"말투 예시: {npc.behaviorExample}");

        if (!string.IsNullOrEmpty(context))
        {
            prompt.AppendLine($"\n[최근 채팅 기록]");
            prompt.AppendLine(context);
        }

        if (!string.IsNullOrEmpty(replyToMessage) && !string.IsNullOrEmpty(replyToSenderId))
        {
            var senderNPC = npcManager.GetNPC(replyToSenderId);
            string senderName = senderNPC != null ? senderNPC.name : replyToSenderId;
            float relationship = npc.GetRelationship(replyToSenderId);

            prompt.AppendLine($"\n{senderName}님이 당신에게 다음과 같이 말했습니다:");
            prompt.AppendLine($"\"{replyToMessage}\"");
            prompt.AppendLine($"당신과 {senderName}님의 관계도: {relationship:F1}/100");
            prompt.AppendLine("위 메시지에 자연스럽게 답장해주세요.");
        }
        else
        {
            prompt.AppendLine("\n[자유 채팅]");
            prompt.AppendLine("게임 채팅방에 자연스럽게 말을 걸어주세요.");
        }

        prompt.AppendLine("\n[작성 규칙]");
        prompt.AppendLine("- 말투와 성격을 정확히 반영");
        prompt.AppendLine("- 게임 채팅처럼 짧고 간결하게 (최대 50자 권장)");
        prompt.AppendLine("- 오타나 줄임말 사용 가능 (자연스러운 범위 내)");
        prompt.AppendLine("- 과도하게 길거나 형식적인 문장 금지");
        prompt.AppendLine("- 채팅방에서 실제로 말하는 것처럼 작성");

        return prompt.ToString();
    }


    /// <summary>
    /// 최근 채팅 기록 가져오기 (컨텍스트용)
    /// </summary>
    private string GetRecentChatContext()
    {
        if (chatManager == null)
            return "";

        var recentMessages = chatManager.GetRecentMessages(contextMessageCount);
        if (recentMessages.Count == 0)
            return "";

        StringBuilder context = new StringBuilder();
        foreach (var msg in recentMessages)
        {
            if (msg.type == ChatMessageType.Normal)
            {
                string sender = string.IsNullOrEmpty(msg.senderName) ? "알 수 없음" : msg.senderName;
                context.AppendLine($"{sender}: {msg.content}");
            }
        }

        return context.ToString();
    }

    /// <summary>
    /// 답장 연쇄 체크
    /// </summary>
    private void CheckResponseChain(string senderId, string message)
    {
        if (npcManager == null)
        {
            EndResponseChain();
            return;
        }

        var onlineNPCs = GetOnlineNPCs();
        var possibleResponders = new List<(string npcId, float probability)>();

        // 접속 중인 NPC 중 답장할 수 있는 NPC 찾기
        foreach (string npcId in onlineNPCs)
        {
            if (npcId == senderId)
                continue; // 자신에게는 답장하지 않음

            var npc = npcManager.GetNPC(npcId);
            if (npc == null)
                continue;

            // 이름 언급 체크
            bool nameMentioned = npc.IsNameMentioned(message, npc.name);
            
            // 답장 확률 계산
            float responseProb = npc.CalculateResponseProbability(message, senderId);
            
            // 이름이 언급되었으면 확률 증가
            if (nameMentioned)
            {
                float mentionBonus = npc.CalculateNameMentionConfidence(message, npc.name);
                responseProb = Mathf.Clamp01(responseProb + mentionBonus * 0.3f);
            }

            if (responseProb > 0f)
            {
                possibleResponders.Add((npcId, responseProb));
            }
        }

        // 확률 기반으로 답장할 NPC 선택
        if (possibleResponders.Count > 0)
        {
            // 가장 높은 확률의 NPC 선택
            possibleResponders.Sort((a, b) => b.probability.CompareTo(a.probability));
            
            var responder = possibleResponders[0];
            if (Random.value <= responder.probability)
            {
                // 답장 생성
                var responderNPC = npcManager.GetNPC(responder.npcId);
                if (responderNPC != null)
                {
                    GenerateNPCChat(responder.npcId, responderNPC, message, senderId);
                    return; // 연쇄 계속
                }
            }
        }

        // 답장할 NPC가 없으면 연쇄 종료
        EndResponseChain();
    }

    /// <summary>
    /// 답장 연쇄 종료
    /// </summary>
    private void EndResponseChain()
    {
        _isResponseChainActive = false;
        ResetRandomCycle(); // 랜덤 주기 재시작
    }

    /// <summary>
    /// 외부에서 메시지 수신 시 답장 연쇄 시작 (플레이어 메시지 등)
    /// </summary>
    public void OnMessageReceived(string senderId, string message)
    {
        if (_isResponseChainActive)
            return; // 이미 연쇄 진행 중

        CheckResponseChain(senderId, message);
    }
}
