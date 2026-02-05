using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 몬스터 데이터 에디터 - JSON 추가/수정 및 Resources/Prefabs/Monsters 프리팹 연결
/// </summary>
public class MonsterDataEditor : EditorWindow
{
    private Vector2 scrollPosition;
    private MonsterJsonData monsterData;
    private string jsonFilePath;
    private bool isDirty = false;

    private const string MonstersPrefabFolder = "Assets/Resources/Prefabs/Monsters";

    [MenuItem("DataManager/몬스터 데이터 관리")]
    public static void ShowWindow()
    {
        GetWindow<MonsterDataEditor>("몬스터 데이터 관리");
    }

    private void OnEnable()
    {
        LoadMonsterData();
    }

    private void OnGUI()
    {
        if (monsterData == null)
        {
            EditorGUILayout.HelpBox("몬스터 데이터를 로드할 수 없습니다.", MessageType.Error);
            if (GUILayout.Button("데이터 로드"))
                LoadMonsterData();
            return;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"몬스터 수: {monsterData.monsters?.Count ?? 0}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("새 몬스터 추가", EditorStyles.toolbarButton))
            AddNewMonster();
        if (GUILayout.Button("저장", EditorStyles.toolbarButton))
            SaveMonsterData();
        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton))
            LoadMonsterData();
        EditorGUILayout.EndHorizontal();

        if (isDirty)
            EditorGUILayout.HelpBox("변경사항이 있습니다. 저장해주세요.", MessageType.Warning);

        GUILayout.Space(5);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (monsterData.monsters == null || monsterData.monsters.Count == 0)
        {
            EditorGUILayout.HelpBox("몬스터가 없습니다. '새 몬스터 추가' 버튼을 눌러 추가하세요.\n프리팹은 Resources/Prefabs/Monsters 에 두면 선택할 수 있습니다.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < monsterData.monsters.Count; i++)
                DrawMonsterEditor(i);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawMonsterEditor(int index)
    {
        MonsterJson monster = monsterData.monsters[index];

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        bool foldout = EditorPrefs.GetBool($"Monster_Foldout_{monster.monsterId}", false);
        foldout = EditorGUILayout.Foldout(foldout, $"{monster.monsterName} ({monster.monsterId})", true);
        EditorPrefs.SetBool($"Monster_Foldout_{monster.monsterId}", foldout);
        GUILayout.FlexibleSpace();
        GUI.color = Color.red;
        if (GUILayout.Button("삭제", GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("몬스터 삭제", $"{monster.monsterName}을(를) 삭제하시겠습니까?", "삭제", "취소"))
            {
                monsterData.monsters.RemoveAt(index);
                isDirty = true;
            }
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        if (!foldout)
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
            return;
        }

        EditorGUI.BeginChangeCheck();

        monster.monsterId = EditorGUILayout.TextField("몬스터 ID", monster.monsterId ?? "");
        monster.monsterName = EditorGUILayout.TextField("몬스터 이름", monster.monsterName ?? "");

        EditorGUILayout.LabelField("프리팹 (Resources/Prefabs/Monsters)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        monster.prefabPath = EditorGUILayout.TextField("프리팹 경로", monster.prefabPath ?? "");

        if (GUILayout.Button("프리팹 선택", GUILayout.Width(120)))
        {
            string path = EditorUtility.OpenFilePanel("몬스터 프리팹 선택", MonstersPrefabFolder, "prefab");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.Contains("Assets/Resources/"))
                {
                    int start = path.IndexOf("Resources/") + "Resources/".Length;
                    monster.prefabPath = path.Substring(start);
                    monster.prefabPath = Path.ChangeExtension(monster.prefabPath, null);
                }
                else
                    EditorUtility.DisplayDialog("경로 오류", "프리팹은 Assets/Resources/Prefabs/Monsters 폴더 내에 있어야 합니다.", "확인");
            }
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(monster.prefabPath))
        {
            GameObject prefab = Resources.Load<GameObject>(monster.prefabPath);
            if (prefab != null)
                EditorGUILayout.HelpBox($"프리팹 로드됨: {monster.prefabPath}", MessageType.None);
            else
                EditorGUILayout.HelpBox($"프리팹을 찾을 수 없습니다: {monster.prefabPath}\nResources/Prefabs/Monsters 에 해당 경로의 프리팹이 있는지 확인하세요.", MessageType.Warning);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("스탯", EditorStyles.boldLabel);
        monster.maxHP = Mathf.Max(1, EditorGUILayout.IntField("최대 HP", monster.maxHP));
        monster.level = Mathf.Max(1, EditorGUILayout.IntField("레벨", monster.level));
        monster.expReward = Mathf.Max(0, EditorGUILayout.IntField("경험치 보상", monster.expReward));
        monster.goldReward = Mathf.Max(0, EditorGUILayout.IntField("골드 보상", monster.goldReward));

        if (EditorGUI.EndChangeCheck())
            isDirty = true;

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void AddNewMonster()
    {
        if (monsterData.monsters == null)
            monsterData.monsters = new List<MonsterJson>();

        monsterData.monsters.Add(new MonsterJson
        {
            monsterId = $"monster_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
            monsterName = "새 몬스터",
            prefabPath = "",
            maxHP = 30,
            level = 1,
            expReward = 10,
            goldReward = 5
        });
        isDirty = true;
    }

    private void LoadMonsterData()
    {
        jsonFilePath = "Assets/Resources/Data/Monsters.json";

        if (!File.Exists(jsonFilePath))
        {
            monsterData = new MonsterJsonData { monsters = new List<MonsterJson>() };
            SaveMonsterData();
            Debug.Log("새 몬스터 JSON 파일 생성: " + jsonFilePath);
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            monsterData = JsonUtility.FromJson<MonsterJsonData>(jsonContent);
            if (monsterData?.monsters == null)
                monsterData.monsters = new List<MonsterJson>();
            isDirty = false;
            Debug.Log($"몬스터 데이터 로드 완료: {monsterData.monsters.Count}개");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"몬스터 데이터 로드 실패: {e.Message}");
            monsterData = new MonsterJsonData { monsters = new List<MonsterJson>() };
        }
    }

    private void SaveMonsterData()
    {
        if (monsterData == null) return;
        if (string.IsNullOrEmpty(jsonFilePath))
            jsonFilePath = "Assets/Resources/Data/Monsters.json";

        string directory = Path.GetDirectoryName(jsonFilePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(jsonFilePath, JsonUtility.ToJson(monsterData, true));
            AssetDatabase.Refresh();
            isDirty = false;
            Debug.Log($"몬스터 데이터 저장 완료: {jsonFilePath}");
            EditorUtility.DisplayDialog("저장 완료", "몬스터 데이터가 저장되었습니다.", "확인");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"몬스터 데이터 저장 실패: {e.Message}");
            EditorUtility.DisplayDialog("저장 실패", $"저장 중 오류가 발생했습니다:\n{e.Message}", "확인");
        }
    }

    private void OnDestroy()
    {
        if (isDirty && EditorUtility.DisplayDialog("저장하지 않은 변경사항", "저장하지 않은 변경사항이 있습니다. 저장하시겠습니까?", "저장", "취소"))
            SaveMonsterData();
    }
}
