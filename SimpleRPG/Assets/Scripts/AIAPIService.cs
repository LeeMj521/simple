using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// AI API 서비스 - AI API 호출 및 응답 처리
/// </summary>
public class AIAPIService : MonoBehaviour
{
    // [Tooltip("API 키")]
    private string apiKey = "";
    [Header("AI API 설정")]
    [Tooltip("AI API 엔드포인트 URL")]
    [SerializeField] private string apiEndpoint = "https://api.openai.com/v1/chat/completions";
    [Tooltip("사용할 AI 모델")]
    [SerializeField] private string modelName = "gpt-4o-mini";
    // [SerializeField] private string modelName = "gemini-2.5-flash";
    [Tooltip("최대 토큰 수")]
    [SerializeField] private int maxTokens = 100;

    [Header("디버그 설정")]
    [Tooltip("API 요청/응답 로그 출력 여부")]
    [SerializeField] private bool enableLogging = true;

    private string _loadedApiKey;

    private void Awake()
    {
        LoadSecrets();
    }

    /// <summary>
    /// 시크릿 JSON 파일에서 API 키 로드
    /// </summary>
    private void LoadSecrets()
    {
        TextAsset secretsFile = Resources.Load<TextAsset>("secrets");
        if (secretsFile != null)
        {
            try
            {
                SecretsData secrets = JsonUtility.FromJson<SecretsData>(secretsFile.text);
                if (secrets != null && !string.IsNullOrEmpty(secrets.apiKey))
                {
                    _loadedApiKey = secrets.apiKey;
                    if (enableLogging)
                    {
                        Debug.Log("[AIAPIService] secrets.json에서 API 키 로드 완료");
                    }
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AIAPIService] secrets.json 파싱 실패: {e.Message}");
            }
        }

        // secrets.json이 없거나 파싱 실패 시 인스펙터 값 사용
        _loadedApiKey = apiKey;
        if (string.IsNullOrEmpty(_loadedApiKey))
        {
            Debug.LogWarning("[AIAPIService] API 키가 설정되지 않았습니다. secrets.json 파일을 생성하거나 인스펙터에서 설정하세요.");
        }
    }

    /// <summary>
    /// 현재 사용 중인 API 키 반환
    /// </summary>
    private string GetApiKey()
    {
        return !string.IsNullOrEmpty(_loadedApiKey) ? _loadedApiKey : apiKey;
    }

    /// <summary>
    /// AI API를 호출하여 채팅 메시지 생성
    /// </summary>
    /// <param name="prompt">AI에게 전달할 프롬프트</param>
    /// <param name="npcName">NPC 이름 (시스템 메시지용)</param>
    /// <param name="onComplete">성공 시 콜백 (생성된 메시지)</param>
    /// <param name="onError">실패 시 콜백 (에러 메시지)</param>
    public void GenerateChatMessage(string prompt, string npcName, System.Action<string> onComplete, System.Action<string> onError)
    {
        StartCoroutine(GenerateChatMessageCoroutine(prompt, npcName, onComplete, onError));
    }

    /// <summary>
    /// AI API 호출 코루틴
    /// </summary>
    private IEnumerator GenerateChatMessageCoroutine(string prompt, string npcName, System.Action<string> onComplete, System.Action<string> onError)
    {
        // API 요청 본문 생성
        string requestBody = BuildAPIRequest(prompt, npcName);

        // 송신 로그 출력
        if (enableLogging)
        {
            Debug.Log($"[AIAPIService] 송신 - NPC: {npcName}\n엔드포인트: {apiEndpoint}\n요청 본문:\n{requestBody}");
        }

        using (UnityWebRequest request = new UnityWebRequest(apiEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {GetApiKey()}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                
                // 수신 로그 출력
                if (enableLogging)
                {
                    Debug.Log($"[AIAPIService] 수신 - NPC: {npcName}\n응답 본문:\n{responseText}");
                }

                string chatMessage = ParseAPIResponse(responseText);

                if (!string.IsNullOrEmpty(chatMessage))
                {
                    if (enableLogging)
                    {
                        Debug.Log($"[AIAPIService] 파싱 완료 - NPC: {npcName}\n생성된 메시지: {chatMessage}");
                    }
                    onComplete?.Invoke(chatMessage);
                }
                else
                {
                    Debug.LogWarning($"[AIAPIService] 파싱 실패 - NPC: {npcName}\n응답 본문:\n{responseText}");
                    onError?.Invoke("API 응답 파싱 실패");
                }
            }
            else
            {
                Debug.LogError($"[AIAPIService] 요청 실패 - NPC: {npcName}\n에러: {request.error}\n응답 코드: {request.responseCode}");
                onError?.Invoke($"API 요청 실패: {request.error}");
            }
        }
    }

    /// <summary>
    /// API 요청 본문 생성
    /// </summary>
    private string BuildAPIRequest(string prompt, string npcName)
    {
        // OpenAI API 형식 JSON 문자열 직접 생성
        StringBuilder json = new StringBuilder();
        json.Append("{");
        json.Append($"\"model\":\"{modelName}\",");
        json.Append("\"messages\":[");
        json.Append($"{{\"role\":\"system\",\"content\":\"당신은 '{npcName}'라는 게임 NPC입니다. 자연스럽고 짧은 채팅 메시지를 작성합니다.\"}},");
        json.Append($"{{\"role\":\"user\",\"content\":\"{EscapeJsonString(prompt)}\"}}");
        json.Append("],");
        json.Append($"\"max_tokens\":{maxTokens},");
        json.Append("\"temperature\":0.8");
        json.Append("}");

        return json.ToString();
    }

    /// <summary>
    /// JSON 문자열 이스케이프
    /// </summary>
    private string EscapeJsonString(string str)
    {
        if (string.IsNullOrEmpty(str))
            return "";

        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("\n", "\\n")
                  .Replace("\r", "\\r")
                  .Replace("\t", "\\t");
    }

    /// <summary>
    /// API 응답 파싱
    /// </summary>
    private string ParseAPIResponse(string responseJson)
    {
        try
        {
            // OpenAI API 응답 형식 파싱
            // JSON 형식: {"choices": [{"message": {"role": "assistant", "content": "..."}}]}
            var response = JsonUtility.FromJson<OpenAIResponse>(responseJson);
            if (response != null && response.choices != null && response.choices.Length > 0)
            {
                var choice = response.choices[0];
                if (choice.message != null && !string.IsNullOrEmpty(choice.message.content))
                {
                    return choice.message.content.Trim();
                }
            }

            // 파싱 실패 시 직접 문자열 검색 (폴백)
            int contentIndex = responseJson.IndexOf("\"content\":");
            if (contentIndex >= 0)
            {
                int startIndex = responseJson.IndexOf("\"", contentIndex + 10) + 1;
                int endIndex = responseJson.IndexOf("\"", startIndex);
                if (startIndex > 0 && endIndex > startIndex)
                {
                    string content = responseJson.Substring(startIndex, endIndex - startIndex);
                    return content.Trim().Replace("\\n", "\n").Replace("\\\"", "\"");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AIAPIService] API 응답 파싱 오류: {e.Message}\n응답: {responseJson}");
        }

        return null;
    }
}

/// <summary>
/// 시크릿 데이터 구조
/// </summary>
[System.Serializable]
public class SecretsData
{
    public string apiKey;
}

/// <summary>
/// OpenAI API 응답 구조
/// </summary>
[System.Serializable]
public class OpenAIResponse
{
    public OpenAIChoice[] choices;
}

[System.Serializable]
public class OpenAIChoice
{
    public OpenAIMessage message;
}

[System.Serializable]
public class OpenAIMessage
{
    public string role;
    public string content;
}
