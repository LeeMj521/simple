using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 채팅창 윗쪽을 드래그하여 높이를 조절하는 컴포넌트
/// </summary>
public class ChatWindowResizer : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
  [Header("참조")]
  [Tooltip("높이를 조절할 채팅창 (RectTransform)")]
  [SerializeField] private RectTransform chatWindow;

  [Tooltip("드래그 핸들 영역")]
  [SerializeField] private RectTransform dragHandle;

  [Header("제한 설정")]
  [Tooltip("최소 높이")]
  [SerializeField] private float minHeight = 100f;

  [Tooltip("최대 높이")]
  [SerializeField] private float maxHeight = 800f;

  [Tooltip("기본 높이 (초기화 시 사용)")]
  [SerializeField] private float defaultHeight = 300f;

  private RectTransform _canvasRectTransform;
  private Canvas _canvas;
  private Vector2 _initialMousePosition;
  private float _initialHeight;
  private bool _isDragging = false;

  private void Awake(){
    // Canvas 찾기
    _canvas = GetComponentInParent<Canvas>();
    if (_canvas != null){
      _canvasRectTransform = _canvas.GetComponent<RectTransform>();
    }

    // 초기 높이 설정
    if (chatWindow != null && defaultHeight > 0f){
      chatWindow.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, defaultHeight);
    }
  }

  public void OnBeginDrag(PointerEventData eventData){
    if (chatWindow == null) return;

    _isDragging = true;
    _initialMousePosition = GetMousePositionInCanvas();
    _initialHeight = chatWindow.rect.height;

    // if (dragHandle != null){
    //   Image image = dragHandle.GetComponent<Image>();
    //   if (image != null){
    //     image.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // 반투명 회색
    //   }
    // }
  }

  public void OnDrag(PointerEventData eventData){
    if (!_isDragging || chatWindow == null) return;

    Vector2 currentMousePosition = GetMousePositionInCanvas();
    float deltaY = currentMousePosition.y - _initialMousePosition.y;

    float newHeight = Mathf.Clamp(_initialHeight + deltaY, minHeight, maxHeight);
    chatWindow.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
  }

  public void OnEndDrag(PointerEventData eventData){
    _isDragging = false;

    // if (dragHandle != null){
    //   Image image = dragHandle.GetComponent<Image>();
    //   if (image != null){
    //     image.color = new Color(1f, 1f, 1f, 1f); // 투명하게 복원
    //   }
    // }
  }

  /// <summary>
  /// Canvas 좌표계에서 마우스 위치를 가져옵니다
  /// </summary>
  private Vector2 GetMousePositionInCanvas(){
    if (_canvas == null || _canvasRectTransform == null){
      return Input.mousePosition;
    }

    Vector2 mousePosition = Input.mousePosition;
    Vector2 localPoint;
      
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
      _canvasRectTransform,
      mousePosition,
      _canvas.worldCamera,
      out localPoint
    );
    
    return localPoint;
  }

  /// <summary>
  /// 채팅창 높이를 설정합니다 (프로그래밍 방식)
  /// </summary>
  public void SetHeight(float height){
    if (chatWindow != null){
      float clampedHeight = Mathf.Clamp(height, minHeight, maxHeight);
      chatWindow.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, clampedHeight);
    }
  }

  /// <summary>
  /// 현재 채팅창 높이를 가져옵니다
  /// </summary>
  public float GetHeight(){
    return chatWindow != null ? chatWindow.rect.height : 0f;
  }
}
