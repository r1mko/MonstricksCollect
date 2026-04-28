using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using YG;
using System.Collections.Generic;

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
        public Vector2 initialAnchoredPosition;
        public Vector3 initialRotation;
        public Vector2 initialSizeDelta;
    }

    [SerializeField] private CardManager cardManager;
    [SerializeField] private UINavigation uINavigation;
    [SerializeField] private PackData[] packData;
    [SerializeField] private ButtonData[] packsButton;

    [Header("UI References")]
    [SerializeField] private GameObject collectionButton;
    [SerializeField] private Button shuffleButton;

    [Header("Settings")]
    [SerializeField] private float moveToCenterDuration = 0.5f;
    [SerializeField] private float hideSelectedPackDuration = 0.3f;

    private bool isAnimating = false;
    private RectTransform canvasRect;

    private void Awake()
    {
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        if (cardManager == null) cardManager = GetComponent<CardManager>();
        if (uINavigation == null) uINavigation = GetComponent<UINavigation>();
    }

    private void Start()
    {
        CacheLayoutData();

        for (int i = 0; i < packsButton.Length; i++)
        {
            int index = i;
            if (packsButton[index].button != null)
            {
                packsButton[index].button.onClick.AddListener(() => OnPackClick(packsButton[index]));
            }
        }

        if (shuffleButton != null)
        {
            shuffleButton.onClick.AddListener(RefreshAllPacks);
        }

        RefreshAllPacks();
    }

    private void CacheLayoutData()
    {
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < packsButton.Length; i++)
        {
            if (packsButton[i].rectTransform != null)
            {
                RectTransform rt = packsButton[i].rectTransform;
                packsButton[i].initialAnchoredPosition = rt.anchoredPosition;
                packsButton[i].initialRotation = rt.eulerAngles;
                packsButton[i].initialSizeDelta = rt.sizeDelta;
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
        if (shuffleButton != null) shuffleButton.gameObject.SetActive(false);

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

        Vector2 startPos = rt.anchoredPosition;
        Vector2 startSize = rt.sizeDelta;
        Quaternion startRot = rt.rotation;

        Vector2 targetPos = Vector2.zero;
        Vector2 targetSize = startSize * 2f;
        Quaternion targetRot = Quaternion.Euler(0, 0, 0);

        float elapsed = 0f;
        while (elapsed < moveToCenterDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveToCenterDuration;

            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            float currentWidth = Mathf.Lerp(startSize.x, targetSize.x, t);
            float currentHeight = Mathf.Lerp(startSize.y, targetSize.y, t);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);

            rt.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        rt.anchoredPosition = targetPos;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize.y);
        rt.rotation = targetRot;

        SoundManager.Instance.PlayOpenPack();
        yield return StartCoroutine(ScaleToZero(rt, hideSelectedPackDuration));

        int randomIndex = Random.Range(0, packData.characterIndices.Length);
        int randomCharacterIndex = packData.characterIndices[randomIndex];

        if (cardManager != null)
        {
            yield return StartCoroutine(cardManager.ShowCardByCharacterIndex(randomCharacterIndex));
        }

        yield return new WaitUntil(() => !cardManager.IsProcessing());

        rt.gameObject.SetActive(false);

        CheckAndRefreshAllPacks();

        isAnimating = false;
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
            if (shuffleButton != null) shuffleButton.gameObject.SetActive(true);
        }
    }

    public void RefreshAllPacks()
    {
        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < packsButton.Length; i++)
        {
            if (packsButton[i].rectTransform != null)
            {
                RectTransform rt = packsButton[i].rectTransform;
                rt.gameObject.SetActive(true);

                rt.localScale = Vector3.one;
                rt.anchoredPosition = packsButton[i].initialAnchoredPosition;
                rt.rotation = Quaternion.Euler(packsButton[i].initialRotation);
                rt.sizeDelta = packsButton[i].initialSizeDelta;

                rt.SetSiblingIndex(i);
            }
            if (packsButton[i].button != null) packsButton[i].button.interactable = true;
        }

        RandomizeRotations();
        ShufflePackPositionsOnly();

        if (collectionButton != null) collectionButton.SetActive(true);
        if (shuffleButton != null) shuffleButton.gameObject.SetActive(true);

        CacheLayoutData();
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

        Vector2 startSize = target.sizeDelta;
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

                packsButton[i].rectTransform.rotation = Quaternion.Euler(0, 0, randomZ);
            }
        }
    }

    [ContextMenu("Shuffle Pack Positions Only")]
    public void ShufflePackPositionsOnly()
    {
        if (packsButton == null || packsButton.Length < 2) return;

        List<Vector2> currentPositions = new List<Vector2>();
        foreach (var data in packsButton)
        {
            if (data.rectTransform != null)
                currentPositions.Add(data.rectTransform.anchoredPosition);
            else
                currentPositions.Add(Vector2.zero);
        }

        for (int i = currentPositions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2 temp = currentPositions[i];
            currentPositions[i] = currentPositions[j];
            currentPositions[j] = temp;
        }

        for (int i = 0; i < packsButton.Length; i++)
        {
            if (packsButton[i].rectTransform != null)
            {
                packsButton[i].rectTransform.anchoredPosition = currentPositions[i];
            }
        }

        ShuffleSiblingOrder();
    }

    [ContextMenu("Shuffle Sibling Order")]
    public void ShuffleSiblingOrder()
    {
        if (packsButton == null || packsButton.Length < 2) return;

        List<int> indices = new List<int>();
        for (int i = 0; i < packsButton.Length; i++)
        {
            indices.Add(i);
        }

        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[j];
            indices[j] = temp;
        }

        for (int i = 0; i < packsButton.Length; i++)
        {
            if (packsButton[i].rectTransform != null)
            {
                packsButton[i].rectTransform.SetSiblingIndex(indices[i]);
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