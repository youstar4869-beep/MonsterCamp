using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 누르는 동안 버튼을 살짝 작게 (손을 떼거나 버튼 밖으로 나가면 원래 크기).
// Update 없이 터치 이벤트로만 동작. 크리처 칸은 합성 반짝임이 크기를 쓰므로 붙이지 않는다
[RequireComponent(typeof(Button))]
public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private const float PressedScale = 0.94f;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnDisable()
    {
        transform.localScale = Vector3.one;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_button.IsInteractable())
        {
            transform.localScale = Vector3.one * PressedScale;
            AudioManager.Play(Sfx.Button);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }
}
