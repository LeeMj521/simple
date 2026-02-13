using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// 데미지 텍스트 팝업. 위로 올라가면서 페이드아웃
/// </summary>
public class DamageText : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("위로 이동하는 거리 (캔버스 로컬 Y, 픽셀 단위 권장)")]
    [SerializeField] private float moveDistance = 50f;
    [Tooltip("애니메이션 시간 (초)")]
    [SerializeField] private float duration = 1f;
    
    private TextMeshProUGUI _text;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// 데미지 텍스트 표시
    /// </summary>
    public void Show(int damage, Vector3 worldPosition, Camera targetCamera = null)
    {
        if (_text == null)
        {
            _text = GetComponent<TextMeshProUGUI>();
            if (_text == null)
            {
                Debug.LogWarning("[DamageText] TextMeshProUGUI가 없습니다.");
                Destroy(gameObject);
                return;
            }
        }

        // 텍스트 설정
        _text.text = damage.ToString();
        
        // 월드 좌표를 스크린 좌표로 변환 후, 부모(캔버스) 로컬 좌표로 설정
        if (targetCamera == null)
            targetCamera = Camera.main;

        Vector3 screenPos = targetCamera != null
            ? targetCamera.WorldToScreenPoint(worldPosition)
            : worldPosition; // 카메라 없을 때 폴백

        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 부모가 곧 배치 대상 캔버스이므로 root가 아닌 parent 기준으로 변환
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect != null)
            {
                Canvas canvas = parentRect.GetComponent<Canvas>();
                // Screen Space Overlay일 때는 카메라에 null 전달
                Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    ? null
                    : (targetCamera ?? Camera.main);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, screenPos, cam, out Vector2 localPoint);
                rectTransform.anchoredPosition = localPoint;
            }
        }

        // 초기 알파값 설정
        Color color = _text.color;
        color.a = 1f;
        _text.color = color;

        // 부모 기준 위쪽(로컬 Y)으로 이동하면서 페이드아웃
        if (rectTransform != null)
        {
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, moveDistance);
            rectTransform.DOAnchorPos(endPos, duration).SetEase(Ease.OutQuad);
        }
        else
        {
            Vector3 endPos = transform.position + Vector3.up * moveDistance;
            transform.DOMove(endPos, duration).SetEase(Ease.OutQuad);
        }

        _text.DOFade(0f, duration).SetEase(Ease.OutQuad)
            .OnComplete(() => Destroy(gameObject));
    }
}
