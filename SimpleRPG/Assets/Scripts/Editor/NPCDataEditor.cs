using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 채팅 데이터 에디터 - NPC 추가 및 수치 조절
/// </summary>
public class NPCDataEditor : EditorWindow
{
    private Vector2 scrollPosition;
    private NPCJsonData npcData;
    private string jsonFilePath;
    private bool isDirty = false;
    
    [MenuItem("DataManager/NPC 데이터 관리")]
    public static void ShowWindow()
    {
        GetWindow<NPCDataEditor>("NPC 데이터 관리");
    }
    
    private void OnEnable()
    {
        LoadNPCData();
    }
    
    private void OnGUI()
    {
        if (npcData == null)
        {
            EditorGUILayout.HelpBox("NPC 데이터를 로드할 수 없습니다.", MessageType.Error);
            if (GUILayout.Button("데이터 로드"))
            {
                LoadNPCData();
            }
            return;
        }
        
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"NPC 수: {npcData.npcs?.Count ?? 0}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("새 NPC 추가", EditorStyles.toolbarButton))
        {
            AddNewNPC();
        }
        if (GUILayout.Button("저장", EditorStyles.toolbarButton))
        {
            SaveNPCData();
        }
        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton))
        {
            LoadNPCData();
        }
        EditorGUILayout.EndHorizontal();
        
        if (isDirty)
        {
            EditorGUILayout.HelpBox("변경사항이 있습니다. 저장해주세요.", MessageType.Warning);
        }
        
        GUILayout.Space(5);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        if (npcData.npcs == null || npcData.npcs.Count == 0)
        {
            EditorGUILayout.HelpBox("NPC가 없습니다. '새 NPC 추가' 버튼을 눌러 추가하세요.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < npcData.npcs.Count; i++)
            {
                DrawNPCEditor(i);
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawNPCEditor(int index)
    {
        NPCJson npc = npcData.npcs[index];
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // 헤더
        EditorGUILayout.BeginHorizontal();
        bool foldout = EditorPrefs.GetBool($"NPC_Foldout_{npc.npcId}", false);
        foldout = EditorGUILayout.Foldout(foldout, $"{npc.npcName} ({npc.npcId})", true);
        EditorPrefs.SetBool($"NPC_Foldout_{npc.npcId}", foldout);
        
        GUILayout.FlexibleSpace();
        
        GUI.color = Color.red;
        if (GUILayout.Button("삭제", GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("NPC 삭제", $"{npc.npcName}을(를) 삭제하시겠습니까?", "삭제", "취소"))
            {
                npcData.npcs.RemoveAt(index);
                isDirty = true;
                return;
            }
        }
        GUI.color = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        if (!foldout)
        {
            EditorGUILayout.EndVertical();
            return;
        }
        
        EditorGUI.BeginChangeCheck();
        
        // 기본 정보
        EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
        npc.npcId = EditorGUILayout.TextField("NPC ID", npc.npcId);
        npc.npcName = EditorGUILayout.TextField("NPC 이름", npc.npcName);
        
        EditorGUILayout.LabelField("성격 설명", EditorStyles.miniLabel);
        npc.behaviorType = EditorGUILayout.TextArea(npc.behaviorType ?? "", GUILayout.Height(40), GUILayout.ExpandHeight(false));
        
        EditorGUILayout.LabelField("대사 예시", EditorStyles.miniLabel);
        npc.behaviorExample = EditorGUILayout.TextArea(npc.behaviorExample ?? "", GUILayout.Height(40), GUILayout.ExpandHeight(false));
        
        EditorGUILayout.Space(5);
        
        // 스프라이트 설정
        EditorGUILayout.LabelField("스프라이트 설정", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        npc.spritePath = EditorGUILayout.TextField("스프라이트 경로 (Resources 기준)", npc.spritePath ?? "");
        
        // 스프라이트 선택 버튼
        if (GUILayout.Button("스프라이트 선택", GUILayout.Width(120)))
        {
            string path = EditorUtility.OpenFilePanel("NPC 스프라이트 선택", "Assets/Resources", "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(path))
            {
                // Assets/Resources/ 기준으로 경로 변환
                if (path.Contains("Assets/Resources/"))
                {
                    int resourcesIndex = path.IndexOf("Resources/") + "Resources/".Length;
                    npc.spritePath = path.Substring(resourcesIndex);
                    // 확장자 제거
                    npc.spritePath = System.IO.Path.ChangeExtension(npc.spritePath, null);
                }
                else
                {
                    EditorUtility.DisplayDialog("경로 오류", "스프라이트는 Assets/Resources/ 폴더 내에 있어야 합니다.", "확인");
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // 스프라이트 미리보기
        if (!string.IsNullOrEmpty(npc.spritePath))
        {
            Sprite sprite = Resources.Load<Sprite>(npc.spritePath);
            if (sprite != null)
            {
                EditorGUILayout.LabelField("스프라이트 미리보기", EditorStyles.miniLabel);
                Texture2D texture = sprite.texture;
                Rect rect = sprite.textureRect;
                Texture2D croppedTexture = new Texture2D((int)rect.width, (int)rect.height);
                Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
                croppedTexture.SetPixels(pixels);
                croppedTexture.Apply();
                
                GUILayout.Box(croppedTexture, GUILayout.Width(100), GUILayout.Height(100));
            }
            else
            {
                EditorGUILayout.HelpBox($"스프라이트를 찾을 수 없습니다: {npc.spritePath}", MessageType.Warning);
            }
        }
        
        EditorGUILayout.Space(5);
        
        // 확률 및 시간 설정
        EditorGUILayout.LabelField("채팅 확률 설정", EditorStyles.boldLabel);
        npc.speakProbability = EditorGUILayout.Slider("말할 확률", npc.speakProbability, 0f, 1f);
        npc.responseProbability = EditorGUILayout.Slider("답장 확률", npc.responseProbability, 0f, 1f);
        
        EditorGUILayout.Space(5);
        
        // 관계 보너스
        EditorGUILayout.LabelField("관계 보너스", EditorStyles.boldLabel);
        npc.relationshipBonus = EditorGUILayout.Slider("호감도 보너스", npc.relationshipBonus, 0f, 1f);
        
        EditorGUILayout.Space(5);
        
        // 전투 설정
        EditorGUILayout.LabelField("전투 설정", EditorStyles.boldLabel);
        npc.attackPower = EditorGUILayout.IntField("공격력", npc.attackPower);
        npc.attackCooldown = EditorGUILayout.FloatField("공격 쿨타임 초", npc.attackCooldown);
        
        EditorGUILayout.Space(5);
        
        // 접속 시간대 (게임 시간)
        EditorGUILayout.LabelField("접속 시간대 (게임 시간)", EditorStyles.boldLabel);
        if (npc.onlineSchedule == null)
            npc.onlineSchedule = new List<OnlineWindowJson>();
        npc.randomOffsetMinutes = EditorGUILayout.IntSlider("랜덤 오프셋 (분)", npc.randomOffsetMinutes, 0, 60);
        for (int i = 0; i < npc.onlineSchedule.Count; i++)
        {
            OnlineWindowJson w = npc.onlineSchedule[i];
            EditorGUILayout.BeginHorizontal();
            w.startHour = EditorGUILayout.IntField("시작 시", Mathf.Clamp(w.startHour, 0, 23));
            w.startMinute = EditorGUILayout.IntField("분", Mathf.Clamp(w.startMinute, 0, 59));
            GUILayout.Label("~", GUILayout.Width(10));
            w.endHour = EditorGUILayout.IntField("끝 시", Mathf.Clamp(w.endHour, 0, 24));
            w.endMinute = EditorGUILayout.IntField("분", Mathf.Clamp(w.endMinute, 0, 59));
            if (GUILayout.Button("삭제", GUILayout.Width(50)))
            {
                npc.onlineSchedule.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("접속 시간대 추가"))
            npc.onlineSchedule.Add(new OnlineWindowJson { startHour = 9, startMinute = 0, endHour = 12, endMinute = 0 });
        EditorGUILayout.Space(5);
        
        // 초기 관계 설정
        EditorGUILayout.LabelField("초기 관계", EditorStyles.boldLabel);
        if (npc.initialRelationships == null)
        {
            npc.initialRelationships = new List<RelationshipJson>();
        }
        
        for (int i = 0; i < npc.initialRelationships.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            string oldTargetId = npc.initialRelationships[i].targetId;
            npc.initialRelationships[i].targetId = EditorGUILayout.TextField("대상 ID", npc.initialRelationships[i].targetId);
            
            // 대상 ID 변경 시 중복 체크
            if (npc.initialRelationships[i].targetId != oldTargetId)
            {
                string newTargetId = npc.initialRelationships[i].targetId;
                bool isDuplicate = false;
                for (int j = 0; j < npc.initialRelationships.Count; j++)
                {
                    if (j != i && npc.initialRelationships[j].targetId == newTargetId)
                    {
                        isDuplicate = true;
                        break;
                    }
                }
                if (isDuplicate)
                {
                    EditorUtility.DisplayDialog("중복 관계", $"'{newTargetId}'와의 관계가 이미 존재합니다.", "확인");
                    npc.initialRelationships[i].targetId = oldTargetId;
                }
            }
            
            npc.initialRelationships[i].value = EditorGUILayout.Slider("호감도", npc.initialRelationships[i].value, -100f, 100f);
            if (GUILayout.Button("삭제", GUILayout.Width(50)))
            {
                npc.initialRelationships.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        if (GUILayout.Button("관계 추가"))
        {
            string newTargetId = "player";
            // 중복 체크
            if (npc.initialRelationships != null && npc.initialRelationships.Exists(r => r.targetId == newTargetId))
            {
                EditorUtility.DisplayDialog("중복 관계", $"'{newTargetId}'와의 관계가 이미 존재합니다.", "확인");
            }
            else
            {
                npc.initialRelationships.Add(new RelationshipJson { targetId = newTargetId, value = 0f });
            }
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            isDirty = true;
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }
    
    private void AddNewNPC()
    {
        if (npcData.npcs == null)
        {
            npcData.npcs = new List<NPCJson>();
        }
        
        NPCJson newNPC = new NPCJson
        {
            npcId = $"npc_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
            npcName = "새 NPC",
            behaviorType = "",
            behaviorExample = "",
            speakProbability = 0.3f,
            responseProbability = 0.4f,
            relationshipBonus = 0.3f,
            attackPower = 0,
            attackCooldown = 0f,
            spritePath = "",
            onlineSchedule = new List<OnlineWindowJson>(),
            randomOffsetMinutes = 15,
            initialRelationships = new List<RelationshipJson>
            {
                new RelationshipJson { targetId = "player", value = 0f }
            }
        };
        
        npcData.npcs.Add(newNPC);
        isDirty = true;
    }
    
    private void LoadNPCData()
    {
        jsonFilePath = "Assets/Resources/Data/NPCs.json";
        
        if (!File.Exists(jsonFilePath))
        {
            // 파일이 없으면 새로 생성
            npcData = new NPCJsonData
            {
                npcs = new List<NPCJson>()
            };
            SaveNPCData();
            Debug.Log("새 NPC JSON 파일 생성: " + jsonFilePath);
            return;
        }
        
        try
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            npcData = JsonUtility.FromJson<NPCJsonData>(jsonContent);
            
            if (npcData == null)
            {
                npcData = new NPCJsonData { npcs = new List<NPCJson>() };
            }
            
            if (npcData.npcs == null)
            {
                npcData.npcs = new List<NPCJson>();
            }
            
            isDirty = false;
            Debug.Log($"NPC 데이터 로드 완료: {npcData.npcs.Count}개");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NPC 데이터 로드 실패: {e.Message}");
            npcData = new NPCJsonData { npcs = new List<NPCJson>() };
        }
    }
    
    private void SaveNPCData()
    {
        if (npcData == null)
        {
            Debug.LogError("저장할 데이터가 없습니다.");
            return;
        }
        
        if (string.IsNullOrEmpty(jsonFilePath))
        {
            jsonFilePath = "Assets/Resources/Data/NPCs.json";
        }
        
        // 디렉토리 생성
        string directory = Path.GetDirectoryName(jsonFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        try
        {
            // JSON 포맷팅 (Unity의 JsonUtility는 포맷팅을 지원하지 않으므로 수동 포맷팅)
            string json = FormatJson(npcData);
            File.WriteAllText(jsonFilePath, json);
            AssetDatabase.Refresh();
            
            isDirty = false;
            Debug.Log($"NPC 데이터 저장 완료: {jsonFilePath}");
            EditorUtility.DisplayDialog("저장 완료", "NPC 데이터가 저장되었습니다.", "확인");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NPC 데이터 저장 실패: {e.Message}");
            EditorUtility.DisplayDialog("저장 실패", $"저장 중 오류가 발생했습니다:\n{e.Message}", "확인");
        }
    }
    
    private string FormatJson(NPCJsonData data)
    {
        // Unity의 JsonUtility.ToJson(data, true)는 이미 들여쓰기 포맷팅을 지원합니다.
        return JsonUtility.ToJson(data, true);
    }
    
    private void OnDestroy()
    {
        if (isDirty)
        {
            if (EditorUtility.DisplayDialog("저장하지 않은 변경사항", "저장하지 않은 변경사항이 있습니다. 저장하시겠습니까?", "저장", "취소"))
            {
                SaveNPCData();
            }
        }
    }
}
