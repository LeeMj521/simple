using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵 판넬 열기/닫기. 스테이지 버튼은 수동 배치 후 OnClick에서 GoToStage(stageId) 호출.
/// </summary>
public class MapPanelController : MonoBehaviour
{
    [Header("맵 판넬")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private Button openMapButton;
    [SerializeField] private Button closeMapButton;

    [Header("참조 (비어 있으면 씬에서 찾음)")]
    [SerializeField] private MonsterManager monsterManager;

    private void Awake()
    {
        if (monsterManager == null) monsterManager = FindFirstObjectByType<MonsterManager>();
    }

    private void Start()
    {
        if (openMapButton != null)
            openMapButton.onClick.AddListener(OpenPanel);
        if (closeMapButton != null)
            closeMapButton.onClick.AddListener(ClosePanel);

        if (mapPanel != null)
            mapPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (openMapButton != null)
            openMapButton.onClick.RemoveListener(OpenPanel);
        if (closeMapButton != null)
            closeMapButton.onClick.RemoveListener(ClosePanel);
    }

    public void OpenPanel()
    {
        if (mapPanel != null)
            mapPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        if (mapPanel != null)
            mapPanel.SetActive(false);
    }

    /// <summary>
    /// 해당 스테이지로 이동. 수동으로 만든 버튼의 OnClick에서 호출 (인스펙터에 stageId 문자열 입력).
    /// </summary>
    public void GoToStage(string stageId)
    {
        if (monsterManager != null)
            monsterManager.SetStage(stageId ?? "");
        ClosePanel();
    }
}
