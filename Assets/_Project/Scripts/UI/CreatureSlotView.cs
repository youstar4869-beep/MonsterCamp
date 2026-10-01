using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 크리처 한 칸 (상점 칸, 대기석 칸, 팀 칸)
public class CreatureSlotView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _priceText;
    [SerializeField] private Text _levelText;
    [Tooltip("합성 시 반짝이는 흰 덮개")]
    [SerializeField] private Image _flash;
    [SerializeField] private float _flashDuration = 0.4f;
    [Tooltip("비어 있을 때 보여줄 글자 (예: 앞줄)")]
    [SerializeField] private string _emptyLabel;

    // level 0이면 별을 표시하지 않는다 (상점 칸)
    public void Show(CreatureData creature, int level, string priceLabel)
    {
        _icon.sprite = creature.Sprite;
        _icon.enabled = true;
        SetFaded(false);
        _nameText.text = creature.DisplayName;
        _priceText.text = priceLabel;
        _levelText.text = CreatureInstance.ToStars(level);
        _button.interactable = true;
    }

    public void Clear()
    {
        _icon.enabled = false;
        _nameText.text = _emptyLabel;
        _priceText.text = "";
        _levelText.text = "";
        _button.interactable = false;
    }

    // 끌고 있는 동안 원래 칸의 그림을 흐리게
    public void SetFaded(bool faded)
    {
        _icon.color = new Color(1f, 1f, 1f, faded ? 0.35f : 1f);
    }

    // 합성됐을 때 잠깐 반짝이며 커졌다 돌아온다
    public void Flash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (float time = 0f; time < _flashDuration; time += Time.deltaTime)
        {
            float t = time / _flashDuration;
            _flash.color = new Color(1f, 1f, 0.8f, 0.8f * (1f - t));
            transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, t);
            yield return null;
        }
        _flash.color = Color.clear;
        transform.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        // 반짝이는 도중 화면을 떠나도 원래 상태로
        _flash.color = Color.clear;
        transform.localScale = Vector3.one;
    }
}
