using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using YG;

public class PackManager : MonoBehaviour
{
    [System.Serializable]
    public struct PackData
    {
        public int packIndex;
        public int[] characterIndices;
    }

    [System.Serializable]
    public struct ButtonData
    {
        public Button button;
        public int packIndex;
        public RectTransform rectTransform;
        public Vector3 initialPosition;
        public Vector3 initialRotation;
    }

    [SerializeField] private CardManager cardManager;
    [SerializeField] private UINavigation uINavigation;
    [SerializeField] private PackData[] packData;
    [SerializeField] private ButtonData[] packsButton;

    [Header("UI References")]
    [SerializeField] private GameObject collectionButton;
    [SerializeField] private GameObject shuffleButton;

    [Header("Settings")]
    [SerializeField] private float moveToCenterDuration = 0.5f;
    [SerializeField] private float hideSelectedPackDuration = 0.3f;
    [SerializeField] private float resetDuration = 0.3f;

    private bool isAnimating = false;
    private RectTransform canvasRect;
    private Transform parentTransform;

    private void Awake()
    {
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        if (cardManager == null) cardManager = GetComponent<CardManager>();
        if (uINavigation == null) uINavigation = GetComponent<UINavigation>();

        if (packsButton != null && packsButton.Length > 0)
        {
            parentTransform = packsButton[0].rectTransform.parent;
        }
    }

    private void Start()
    {
        for (int i = 0; i < packsButton.Length; i++)
        {
            int index = i;
            if (packsButton[index].button != null)
            {
                packsButton[index].button.onClick.AddListener(() => OnPackClick(packsButton[index]));
            }
        }
    }

    private void OnPackClick(ButtonData selectedPackData)
    {
        if (isAnimating) return;

        PackData selectedPack = GetPackDataByIndex(selectedPackData.packIndex);
        if (selectedPack.characterIndices == null || selectedPack.characterIndices.Length == 0) return;

        isAnimating = true;
        SoundManager.Instance.PlayClick();
        YG2.InterstitialAdvShow();

        if (collectionButton != null) collectionButton.SetActive(false);
        if (shuffleButton != null) shuffleButton.SetActive(false);

        foreach (var data in packsButton)
        {
            if (data.button != null) data.button.interactable = false;
        }

        selectedPackData.rectTransform.SetAsLastSibling();

        StartCoroutine(OpenPackSequence(selectedPackData, selectedPack));
    }

    private IEnumerator OpenPackSequence(ButtonData buttonData, PackData packData)
    {
        RectTransform rt = buttonData.rectTransform;
        Vector3 startPos = rt.position;
        Vector3 startScale = new Vector3(rt.rect.width, rt.rect.height, 1f);
        Vector3 targetPos = canvasRect.position;
        Vector3 targetScale = startScale * 2f;

        float elapsed = 0f;
        while (elapsed < moveToCenterDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveToCenterDuration;

            rt.position = Vector3.Lerp(startPos, targetPos, t);

            float currentWidth = Mathf.Lerp(startScale.x, targetScale.x, t);
            float currentHeight = Mathf.Lerp(startScale.y, targetScale.y, t);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);

            yield return null;
        }

        rt.position = targetPos;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetScale.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetScale.y);

        SoundManager.Instance.PlayOpenPack();
        yield return StartCoroutine(ScaleToZero(rt, hideSelectedPackDuration));

        int randomIndex = Random.Range(0, packData.characterIndices.Length);
        int randomCharacterIndex = packData.characterIndices[randomIndex];

        if (cardManager != null)
        {
            yield return StartCoroutine(cardManager.ShowCardByCharacterIndex(randomCharacterIndex));
        }

        yield return new WaitUntil(() => !cardManager.IsProcessing());

        ResetPackState(buttonData);

        CheckAndRefreshAllPacks();

        isAnimating = false;
    }

    private IEnumerator ResetPackState(ButtonData data)
    {
        RectTransform rt = data.rectTransform;
        if (rt == null) yield break;

        Vector3 startPos = rt.position;
        Quaternion startRot = rt.rotation;
        Vector2 startSize = new Vector2(rt.rect.width, rt.rect.height);

        // Предполагаем, что начальный размер был нормальным (или можно сохранить его в ButtonData при инициализации)
        // Для простоты возвращаем масштаб 1 через localScale, если анимация размера не критична, 
        // но раз мы меняли sizeDelta, нужно вернуть и её. 
        // Однако, чаще всего проще вернуть позицию и ротацию, а скейл/размер сбросить мгновенно или тоже плавно.
        // Сделаем плавный возврат позиции и ротации.

        float elapsed = 0f;
        while (elapsed < resetDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / resetDuration;

            rt.position = Vector3.Lerp(startPos, data.initialPosition, t);
            rt.rotation = Quaternion.Slerp(startRot, Quaternion.Euler(data.initialRotation), t);

            yield return null;
        }

        rt.position = data.initialPosition;
        rt.rotation = Quaternion.Euler(data.initialRotation);
        rt.localScale = Vector3.one;

        // Если важно вернуть и размер (width/height), нужно добавить лерпание SetSizeWithCurrentAnchors аналогично позиции
    }

    private void CheckAndRefreshAllPacks()
    {
        bool allInactive = true;
        foreach (var data in packsButton)
        {
            if (data.rectTransform != null && data.rectTransform.gameObject.activeSelf)
            {
                allInactive = false;
                break;
            }
        }

        if (allInactive)
        {
            RefreshAllPacks();
        }
        else
        {
            foreach (var data in packsButton)
            {
                if (data.button != null) data.button.interactable = true;
            }
            if (collectionButton != null) collectionButton.SetActive(true);
            if (shuffleButton != null) shuffleButton.SetActive(true);
        }
    }

    public void RefreshAllPacks()
    {
        foreach (var data in packsButton)
        {
            if (data.rectTransform != null)
            {
                data.rectTransform.gameObject.SetActive(true);
                data.rectTransform.position = data.initialPosition;
                data.rectTransform.rotation = Quaternion.Euler(data.initialRotation);
                data.rectTransform.localScale = Vector3.one;
            }
            if (data.button != null) data.button.interactable = true;
        }

        RandomizeRotations();

        if (collectionButton != null) collectionButton.SetActive(true);
        if (shuffleButton != null) shuffleButton.SetActive(true);
    }

    private PackData GetPackDataByIndex(int index)
    {
        foreach (var pack in packData)
        {
            if (pack.packIndex == index) return pack;
        }
        return default(PackData);
    }

    private IEnumerator ScaleToZero(RectTransform target, float duration)
    {
        if (target == null) yield break;

        Vector2 startSize = new Vector2(target.rect.width, target.rect.height);
        Vector2 endSize = Vector2.zero;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float w = Mathf.Lerp(startSize.x, endSize.x, t);
            float h = Mathf.Lerp(startSize.y, endSize.y, t);

            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);

            yield return null;
        }

        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);
    }

    [ContextMenu("Randomize Rotations")]
    public void RandomizeRotations()
    {
        if (packsButton == null || packsButton.Length == 0) return;

        for (int i = 0; i < packsButton.Length; i++)
        {
            if (packsButton[i].rectTransform != null)
            {
                float randomZ;
                if (Random.value < 0.6f)
                {
                    int range = Random.Range(0, 2);
                    if (range == 0) randomZ = Random.Range(-45f, 45f);
                    else randomZ = Random.Range(-220f, -140f);
                }
                else
                {
                    randomZ = Random.Range(0f, 360f);
                }

                // Обновляем сохраненное начальное вращение
                packsButton[i].initialRotation = new Vector3(packsButton[i].rectTransform.eulerAngles.x, packsButton[i].rectTransform.eulerAngles.y, randomZ);

                // Применяем вращение
                packsButton[i].rectTransform.rotation = Quaternion.Euler(packsButton[i].initialRotation);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var data in packsButton)
        {
            if (data.button != null) data.button.onClick.RemoveAllListeners();
        }
    }
}