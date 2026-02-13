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
    [Tooltip("위로 이동하는 거리")]
    [SerializeField] private float moveDistance = 1f;
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
        
        // 월드 좌표를 스크린 좌표로 변환
        if (targetCamera == null)
            targetCamera = Camera.main;
        
        if (targetCamera != null)
        {
            Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPosition);
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                RectTransform canvasRect = rectTransform.root.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, screenPos, targetCamera, out Vector2 localPoint);
                    rectTransform.anchoredPosition = localPoint;
                }
            }
        }

        // 초기 알파값 설정
        Color color = _text.color;
        color.a = 1f;
        _text.color = color;

        // 위로 이동하면서 페이드아웃
        Vector3 endPos = transform.position + Vector3.up * moveDistance;
        transform.DOMove(endPos, duration).SetEase(Ease.OutQuad);
        _text.DOFade(0f, duration).SetEase(Ease.OutQuad)
            .OnComplete(() => Destroy(gameObject));
    }
}
