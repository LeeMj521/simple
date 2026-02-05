using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 아이템 데이터 에디터 - JSON 추가/수정 및 저장
/// </summary>
public class ItemDataEditor : EditorWindow
{
    private Vector2 scrollPosition;
    private ItemJsonData itemData;
    private string jsonFilePath;
    private bool isDirty = false;

    private static readonly string[] RarityOptions = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };

    [MenuItem("DataManager/아이템 데이터 관리")]
    public static void ShowWindow()
    {
        GetWindow<ItemDataEditor>("아이템 데이터 관리");
    }

    private void OnEnable()
    {
        LoadItemData();
    }

    private void OnGUI()
    {
        if (itemData == null)
        {
            EditorGUILayout.HelpBox("아이템 데이터를 로드할 수 없습니다.", MessageType.Error);
            if (GUILayout.Button("데이터 로드"))
                LoadItemData();
            return;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"아이템 수: {itemData.items?.Count ?? 0}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("새 아이템 추가", EditorStyles.toolbarButton))
            AddNewItem();
        if (GUILayout.Button("저장", EditorStyles.toolbarButton))
            SaveItemData();
        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton))
            LoadItemData();
        EditorGUILayout.EndHorizontal();

        if (isDirty)
            EditorGUILayout.HelpBox("변경사항이 있습니다. 저장해주세요.", MessageType.Warning);

        GUILayout.Space(5);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (itemData.items == null || itemData.items.Count == 0)
        {
            EditorGUILayout.HelpBox("아이템이 없습니다. '새 아이템 추가' 버튼을 눌러 추가하세요.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < itemData.items.Count; i++)
                DrawItemEditor(i);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawItemEditor(int index)
    {
        ItemJson item = itemData.items[index];

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        bool foldout = EditorPrefs.GetBool($"Item_Foldout_{item.itemId}", false);
        foldout = EditorGUILayout.Foldout(foldout, $"{item.itemName} ({item.itemId})", true);
        EditorPrefs.SetBool($"Item_Foldout_{item.itemId}", foldout);
        GUILayout.FlexibleSpace();
        GUI.color = Color.red;
        if (GUILayout.Button("삭제", GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("아이템 삭제", $"{item.itemName}을(를) 삭제하시겠습니까?", "삭제", "취소"))
            {
                itemData.items.RemoveAt(index);
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

        item.itemId = EditorGUILayout.TextField("아이템 ID", item.itemId ?? "");
        item.itemName = EditorGUILayout.TextField("아이템 이름", item.itemName ?? "");

        int rarityIndex = System.Array.IndexOf(RarityOptions, item.rarity ?? "Common");
        if (rarityIndex < 0) rarityIndex = 0;
        rarityIndex = EditorGUILayout.Popup("등급", rarityIndex, RarityOptions);
        item.rarity = RarityOptions[rarityIndex];

        EditorGUILayout.LabelField("설명", EditorStyles.miniLabel);
        item.description = EditorGUILayout.TextArea(item.description ?? "", GUILayout.Height(40), GUILayout.ExpandHeight(false));

        EditorGUILayout.BeginHorizontal();
        item.iconPath = EditorGUILayout.TextField("아이콘 경로 (Resources 기준)", item.iconPath ?? "");
        if (GUILayout.Button("아이콘 선택", GUILayout.Width(120)))
        {
            string path = EditorUtility.OpenFilePanel("아이콘 선택", "Assets/Resources", "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(path) && path.Contains("Assets/Resources/"))
            {
                int start = path.IndexOf("Resources/") + "Resources/".Length;
                item.iconPath = path.Substring(start);
                item.iconPath = Path.ChangeExtension(item.iconPath, null);
            }
            else if (!string.IsNullOrEmpty(path))
                EditorUtility.DisplayDialog("경로 오류", "아이콘은 Assets/Resources/ 폴더 내에 있어야 합니다.", "확인");
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(item.iconPath))
        {
            Sprite sprite = Resources.Load<Sprite>(item.iconPath);
            if (sprite != null)
            {
                Texture2D tex = sprite.texture;
                Rect rect = sprite.textureRect;
                Texture2D cropped = new Texture2D((int)rect.width, (int)rect.height);
                cropped.SetPixels(tex.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height));
                cropped.Apply();
                GUILayout.Box(cropped, GUILayout.Width(64), GUILayout.Height(64));
            }
            else
                EditorGUILayout.HelpBox($"아이콘을 찾을 수 없습니다: {item.iconPath}", MessageType.Warning);
        }

        if (EditorGUI.EndChangeCheck())
            isDirty = true;

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void AddNewItem()
    {
        if (itemData.items == null)
            itemData.items = new List<ItemJson>();

        itemData.items.Add(new ItemJson
        {
            itemId = $"item_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
            itemName = "새 아이템",
            rarity = "Common",
            description = "",
            iconPath = ""
        });
        isDirty = true;
    }

    private void LoadItemData()
    {
        jsonFilePath = "Assets/Resources/Data/Items.json";

        if (!File.Exists(jsonFilePath))
        {
            itemData = new ItemJsonData { items = new List<ItemJson>() };
            SaveItemData();
            Debug.Log("새 아이템 JSON 파일 생성: " + jsonFilePath);
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            itemData = JsonUtility.FromJson<ItemJsonData>(jsonContent);
            if (itemData?.items == null)
                itemData.items = new List<ItemJson>();
            isDirty = false;
            Debug.Log($"아이템 데이터 로드 완료: {itemData.items.Count}개");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"아이템 데이터 로드 실패: {e.Message}");
            itemData = new ItemJsonData { items = new List<ItemJson>() };
        }
    }

    private void SaveItemData()
    {
        if (itemData == null) return;
        if (string.IsNullOrEmpty(jsonFilePath))
            jsonFilePath = "Assets/Resources/Data/Items.json";

        string directory = Path.GetDirectoryName(jsonFilePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(jsonFilePath, JsonUtility.ToJson(itemData, true));
            AssetDatabase.Refresh();
            isDirty = false;
            Debug.Log($"아이템 데이터 저장 완료: {jsonFilePath}");
            EditorUtility.DisplayDialog("저장 완료", "아이템 데이터가 저장되었습니다.", "확인");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"아이템 데이터 저장 실패: {e.Message}");
            EditorUtility.DisplayDialog("저장 실패", $"저장 중 오류가 발생했습니다:\n{e.Message}", "확인");
        }
    }

    private void OnDestroy()
    {
        if (isDirty && EditorUtility.DisplayDialog("저장하지 않은 변경사항", "저장하지 않은 변경사항이 있습니다. 저장하시겠습니까?", "저장", "취소"))
            SaveItemData();
    }
}
