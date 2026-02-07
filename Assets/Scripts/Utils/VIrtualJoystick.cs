using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Components")]
    [SerializeField] private RectTransform joystickBackground; // 큰 원
    [SerializeField] private RectTransform joystickHandle;     // 작은 원
    
    [Header("Joystick Settings")]
    [SerializeField] private float handleRange = 50f; // 핸들이 움직일 수 있는 최대 거리
    
    private Vector2 inputVector;
    private Canvas canvas;
    
    void Start()
    {
        // Canvas 참조 가져오기
        canvas = GetComponentInParent<Canvas>();
        
        // handleRange 자동 설정 (background 크기의 절반)
        if (joystickBackground != null)
        {
            handleRange = joystickBackground.sizeDelta.x / 2f;
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        
        // 스크린 좌표를 RectTransform 로컬 좌표로 변환
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out position))
        {
            // 벡터 크기 제한 (원 범위 밖으로 나가지 않도록)
            // ClampMagnitude를 먼저 적용하여 실제 거리를 handleRange 이내로 제한
            Vector2 clampedPosition = Vector2.ClampMagnitude(position, handleRange);
            
            // 정규화된 입력 벡터 계산 (-1 ~ 1 범위, magnitude는 0 ~ 1)
            inputVector = clampedPosition / handleRange;
            
            // 핸들 위치 업데이트
            joystickHandle.anchoredPosition = clampedPosition;
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        // 조이스틱 초기화
        inputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// 정규화된 입력 벡터 반환 (크기 0~1)
    /// </summary>
    public Vector2 GetInputVector()
    {
        return inputVector;
    }
    
    /// <summary>
    /// 수평 입력값 반환 (-1 ~ 1)
    /// </summary>
    public float Horizontal()
    {
        return inputVector.x;
    }
    
    /// <summary>
    /// 수직 입력값 반환 (-1 ~ 1)
    /// </summary>
    public float Vertical()
    {
        return inputVector.y;
    }
}