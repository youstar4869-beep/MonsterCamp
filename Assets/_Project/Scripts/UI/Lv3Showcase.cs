using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 크리처가 처음 ★★★가 되면 보여 주는 화면 꽉 찬 일러스트 연출 (크리처마다 한 번, meta.json에 기록)
// 일러스트는 Art/Illustrations/Resources/{에셋 이름}_Lv3.png. 없으면 연출 없이 넘어간다
// 보여 줄 때만 불러오고 닫으면 해제한다. 여러 마리면 순서대로 하나씩
public class Lv3Showcase : MonoBehaviour
{
    private const string FileSuffix = "_Lv3";
    private const float DimSeconds = 0.2f;
    // 닫기는 일러스트가 다 나온 뒤부터 (바로 눌려서 놓치지 않게)
    private const float CloseDelay = 0.1f;

    [SerializeField] private MetaProgress _meta;
    [Tooltip("어두운 바탕 (터치하면 닫힘)")]
    [SerializeField] private Image _dim;
    [SerializeField] private Image _rays;
    [SerializeField] private Image _art;
    [Tooltip("이름과 \"★★★ 달성!\" (일러스트 뒤에 나타남)")]
    [SerializeField] private CanvasGroup _caption;
    [SerializeField] private Text _nameText;
    [SerializeField] private float _appearSeconds = 0.4f;
    [SerializeField] private float _raysDegreesPerSecond = 25f;

    private static Sprite _raySprite;

    private readonly Queue<(CreatureData creature, bool record)> _queue = new Queue<(CreatureData, bool)>();
    private Sprite _loaded;
    private float _dimAlpha = -1f;
    private float _openedAt;

    public bool IsOpen => _loaded != null;

    // 처음 ★★★가 된 크리처 (이미 봤으면 무시)
    public void Enqueue(CreatureData creature)
    {
        if (creature == null || _meta.IsLv3IllustrationSeen(creature))
        {
            return;
        }
        foreach ((CreatureData queued, bool _) in _queue)
        {
            if (queued == creature)
            {
                return;
            }
        }
        _queue.Enqueue((creature, true));
        if (!IsOpen)
        {
            ShowNext();
        }
    }

    // 테스트용: 본 기록과 상관없이 보여 주고 기록하지 않는다
    public void Preview(CreatureData creature)
    {
        _queue.Enqueue((creature, false));
        if (!IsOpen)
        {
            ShowNext();
        }
    }

    // 화면 아무 곳이나 터치
    public void OnTap()
    {
        if (!IsOpen || Time.unscaledTime - _openedAt < _appearSeconds + CloseDelay)
        {
            return;
        }
        Unload();
        ShowNext();
    }

    private void ShowNext()
    {
        while (_queue.Count > 0)
        {
            (CreatureData creature, bool record) = _queue.Dequeue();
            var sprite = Resources.Load<Sprite>(creature.name + FileSuffix);
            if (sprite == null)
            {
                continue;
            }
            if (record)
            {
                _meta.SetLv3IllustrationSeen(creature);
            }
            Open(creature, sprite);
            return;
        }
        gameObject.SetActive(false);
    }

    private void Open(CreatureData creature, Sprite sprite)
    {
        if (_dimAlpha < 0f)
        {
            _dimAlpha = _dim.color.a;
        }
        _rays.sprite = _raySprite ??= CreateRaySprite();
        _loaded = sprite;
        _art.sprite = sprite;
        _nameText.text = creature.DisplayName;
        _openedAt = Time.unscaledTime;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    // 어둡게 → 빛줄기 → 일러스트가 살짝 커지며 나타남 → 이름. 빛줄기는 열려 있는 동안 계속 돈다
    private IEnumerator Animate()
    {
        bool soundPlayed = false;
        float time = 0f;
        while (true)
        {
            float dim = Mathf.Clamp01(time / DimSeconds);
            float appear = Mathf.Clamp01((time - DimSeconds * 0.5f) / _appearSeconds);
            float eased = 1f - (1f - appear) * (1f - appear);
            float caption = Mathf.Clamp01((time - DimSeconds * 0.5f - _appearSeconds) / 0.2f);

            SetAlpha(_dim, _dimAlpha * dim);
            SetAlpha(_rays, dim);
            SetAlpha(_art, eased);
            _art.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, eased);
            _caption.alpha = caption;
            _rays.rectTransform.Rotate(0f, 0f, -_raysDegreesPerSecond * Time.unscaledDeltaTime);

            if (!soundPlayed && appear >= 1f)
            {
                soundPlayed = true;
                AudioManager.Play(Sfx.Merge);
            }
            yield return null;
            time += Time.unscaledDeltaTime;
        }
    }

    private void Unload()
    {
        StopAllCoroutines();
        _art.sprite = null;
        if (_loaded != null)
        {
            Resources.UnloadAsset(_loaded.texture);
            _loaded = null;
        }
    }

    private void OnDisable()
    {
        Unload();
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    // 가운데에서 퍼지는 빛줄기 (흰색, Image 색으로 물들인다). 처음 한 번만 만든다
    private static Sprite CreateRaySprite()
    {
        const int size = 256;
        const int rayCount = 12;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - half + 0.5f;
                float dy = y - half + 0.5f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy) / half;
                float ray = Mathf.SmoothStep(0.55f, 1f, Mathf.Cos(Mathf.Atan2(dy, dx) * rayCount) * 0.5f + 0.5f);
                float fade = Mathf.Clamp01(1f - radius);
                byte alpha = (byte)(Mathf.Clamp01(ray * fade * 1.4f + fade * fade * 0.35f) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
