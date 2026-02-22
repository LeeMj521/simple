using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 스테이지 버튼에 붙여서 사용. Inspector에 stageId 입력 후, Button OnClick에 이 컴포넌트의 GoToStage() 연결.
/// 호버 시 hoverTooltip에 스테이지 이름 표시.
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Button))]
public class StageButtonLink : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string stageId;
    [SerializeField] private MapPanelController mapPanelController;
    [Tooltip("호버 시 켜질 툴팁 GameObject. 자식에서 TextMeshProUGUI를 찾아 텍스트 변경. 비어 있으면 호버 표시 안 함.")]
    [SerializeField] private GameObject hoverTooltip;

    private DataManager _dataManager;
    private TextMeshProUGUI _tooltipText;

    private void Start()
    {
        if (mapPanelController == null)
            mapPanelController = FindFirstObjectByType<MapPanelController>();
        if (_dataManager == null)
            _dataManager = FindFirstObjectByType<DataManager>();

        if (hoverTooltip != null)
        {
            _tooltipText = hoverTooltip.GetComponentInChildren<TextMeshProUGUI>(true);
            if (_tooltipText != null)
            {
                string displayName = stageId;
                if (_dataManager != null)
                {
                    StageData stage = _dataManager.GetStage(stageId);
                    if (stage != null && !string.IsNullOrEmpty(stage.stageName))
                        displayName = stage.stageName;
                }
                _tooltipText.text = displayName;
            }
            hoverTooltip.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData){
        if (hoverTooltip != null)
            hoverTooltip.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData){
        if (hoverTooltip != null)
            hoverTooltip.SetActive(false);
    }

    /// <summary>Button OnClick에 이 메서드를 연결하세요.</summary>
    public void GoToStage(){
        if (mapPanelController != null)
            mapPanelController.GoToStage(stageId);
        if (hoverTooltip != null)
            hoverTooltip.SetActive(false);
    }
}
