using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 스킬 데이터 에디터 - Skills.json 로드/저장/편집
/// </summary>
public class SkillDataEditor : EditorWindow
{
    private Vector2 scrollPosition;
    private SkillJsonData skillData;
    private string jsonFilePath;
    private bool isDirty = false;

    private const string SkillsPrefabFolder = "Assets/Resources/Prefabs/Skill";

    [MenuItem("DataManager/스킬 데이터 관리")]
    public static void ShowWindow()
    {
        GetWindow<SkillDataEditor>("스킬 데이터 관리");
    }

    private void OnEnable()
    {
        LoadSkillData();
    }

    private void OnGUI()
    {
        if (skillData == null)
        {
            EditorGUILayout.HelpBox("스킬 데이터를 로드할 수 없습니다.", MessageType.Error);
            if (GUILayout.Button("데이터 로드"))
                LoadSkillData();
            return;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"스킬 수: {skillData.skills?.Count ?? 0}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("새 스킬 추가", EditorStyles.toolbarButton))
            AddNewSkill();
        if (GUILayout.Button("저장", EditorStyles.toolbarButton))
            SaveSkillData();
        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton))
            LoadSkillData();
        EditorGUILayout.EndHorizontal();

        if (isDirty)
            EditorGUILayout.HelpBox("변경사항이 있습니다. 저장해주세요.", MessageType.Warning);

        GUILayout.Space(5);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (skillData.skills == null || skillData.skills.Count == 0)
        {
            EditorGUILayout.HelpBox("스킬이 없습니다. '새 스킬 추가' 버튼을 눌러 추가하세요.\n프리팹은 Resources/Prefabs/Skill 에 두면 선택할 수 있습니다.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < skillData.skills.Count; i++)
                DrawSkillEditor(i);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSkillEditor(int index)
    {
        SkillJson skill = skillData.skills[index];

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        bool foldout = EditorPrefs.GetBool($"Skill_Foldout_{skill.skillId}", false);
        foldout = EditorGUILayout.Foldout(foldout, $"{skill.skillName} ({skill.skillId})", true);
        EditorPrefs.SetBool($"Skill_Foldout_{skill.skillId}", foldout);
        GUILayout.FlexibleSpace();
        GUI.color = Color.red;
        if (GUILayout.Button("삭제", GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("스킬 삭제", $"{skill.skillName}을(를) 삭제하시겠습니까?", "삭제", "취소"))
            {
                skillData.skills.RemoveAt(index);
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

        EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
        skill.skillId = EditorGUILayout.TextField("스킬 ID", skill.skillId ?? "");
        skill.skillName = EditorGUILayout.TextField("스킬 이름", skill.skillName ?? "");
        skill.cooldown = Mathf.Max(0.1f, EditorGUILayout.FloatField("쿨타임 (초)", skill.cooldown));
        skill.maxSkillLevel = Mathf.Max(1, EditorGUILayout.IntField("최대 스킬 레벨", skill.maxSkillLevel));
        skill.damage = Mathf.Max(0, EditorGUILayout.IntField("데미지 (0이면 유저 기본 공격력)", skill.damage));

        EditorGUILayout.LabelField("설명", EditorStyles.miniLabel);
        skill.description = EditorGUILayout.TextArea(skill.description ?? "", GUILayout.Height(36));
        skill.iconPath = EditorGUILayout.TextField("아이콘 경로 (Resources)", skill.iconPath ?? "");

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("이펙트 프리팹 (Resources)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        skill.prefabPath = EditorGUILayout.TextField("프리팹 경로", skill.prefabPath ?? "");
        if (GUILayout.Button("프리팹 선택", GUILayout.Width(120)))
        {
            string path = EditorUtility.OpenFilePanel("스킬 이펙트 프리팹 선택", SkillsPrefabFolder, "prefab");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.Contains("Assets/Resources/"))
                {
                    int start = path.IndexOf("Resources/") + "Resources/".Length;
                    skill.prefabPath = path.Substring(start);
                    skill.prefabPath = Path.ChangeExtension(skill.prefabPath, null);
                }
                else
                    EditorUtility.DisplayDialog("경로 오류", "프리팹은 Assets/Resources/ 폴더 내에 있어야 합니다.", "확인");
            }
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(skill.prefabPath))
        {
            GameObject prefab = Resources.Load<GameObject>(skill.prefabPath);
            if (prefab != null)
                EditorGUILayout.HelpBox($"프리팹 로드됨: {skill.prefabPath}", MessageType.None);
            else
                EditorGUILayout.HelpBox($"프리팹을 찾을 수 없습니다: {skill.prefabPath}", MessageType.Warning);
        }

        if (EditorGUI.EndChangeCheck())
            isDirty = true;

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void AddNewSkill()
    {
        if (skillData.skills == null)
            skillData.skills = new List<SkillJson>();

        skillData.skills.Add(new SkillJson
        {
            skillId = $"skill_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
            skillName = "새 스킬",
            cooldown = 1f,
            description = "",
            iconPath = "",
            maxSkillLevel = 5,
            prefabPath = "",
            damage = 0
        });
        isDirty = true;
    }

    private void LoadSkillData()
    {
        jsonFilePath = "Assets/Resources/Data/Skills.json";

        if (!File.Exists(jsonFilePath))
        {
            skillData = new SkillJsonData { skills = new List<SkillJson>() };
            SaveSkillData();
            Debug.Log("새 스킬 JSON 파일 생성: " + jsonFilePath);
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            skillData = JsonUtility.FromJson<SkillJsonData>(jsonContent);
            if (skillData?.skills == null)
                skillData.skills = new List<SkillJson>();
            isDirty = false;
            Debug.Log($"스킬 데이터 로드 완료: {skillData.skills.Count}개");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"스킬 데이터 로드 실패: {e.Message}");
            skillData = new SkillJsonData { skills = new List<SkillJson>() };
        }
    }

    private void SaveSkillData()
    {
        if (skillData == null) return;
        if (string.IsNullOrEmpty(jsonFilePath))
            jsonFilePath = "Assets/Resources/Data/Skills.json";

        string directory = Path.GetDirectoryName(jsonFilePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(jsonFilePath, JsonUtility.ToJson(skillData, true));
            AssetDatabase.Refresh();
            isDirty = false;
            Debug.Log($"스킬 데이터 저장 완료: {jsonFilePath}");
            EditorUtility.DisplayDialog("저장 완료", "스킬 데이터가 저장되었습니다.", "확인");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"스킬 데이터 저장 실패: {e.Message}");
            EditorUtility.DisplayDialog("저장 실패", $"저장 중 오류가 발생했습니다:\n{e.Message}", "확인");
        }
    }

    private void OnDestroy()
    {
        if (isDirty && EditorUtility.DisplayDialog("저장하지 않은 변경사항", "저장하지 않은 변경사항이 있습니다. 저장하시겠습니까?", "저장", "취소"))
            SaveSkillData();
    }
}
