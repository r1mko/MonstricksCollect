using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using YG;

public class PackManager : MonoBehaviour
{
    [System.Serializable]
    public struct PackData
    {
        public Button packButton;
        public int packIndex;
        public int[] characterIndices;
        public RectTransform packRect;
        public Vector3 initialPosition;
    }

    [SerializeField] private CardManager cardManager;
    [SerializeField] private UINavigation uINavigation;
    [SerializeField] private PackData[] packs;
    [SerializeField] private float hideOtherPacksDuration;
    [SerializeField] private float moveToCenterDuration;
    [SerializeField] private float hideSelectedPackDuration;

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
        for (int i = 0; i < packs.Length; i++)
        {
            if (packs[i].packRect != null)
            {
                packs[i].initialPosition = packs[i].packRect.position;
            }

            int index = i;
            if (packs[index].packButton != null)
            {
                packs[index].packButton.onClick.AddListener(() => OnPackClick(packs[index]));
            }
        }
    }

    private void OnPackClick(PackData selectedPack)
    {
        if (isAnimating) return;
        SoundManager.Instance.PlayClick();
        YG2.InterstitialAdvShow();
        StartCoroutine(OpenPackSequence(selectedPack));
    }

    private IEnumerator OpenPackSequence(PackData selectedPack)
    {
        isAnimating = true;
        uINavigation.CollectionButtonVisibility(false);

        foreach (PackData pack in packs)
        {
            if (pack.packButton != null)
                pack.packButton.interactable = false;
        }

        foreach (PackData pack in packs)
        {
            if (pack.packRect != null && pack.packIndex != selectedPack.packIndex)
            {
                SoundManager.Instance.PlayWhoosh();
                yield return StartCoroutine(ScaleToZero(pack.packRect, hideOtherPacksDuration));
            }
        }

        if (selectedPack.characterIndices == null || selectedPack.characterIndices.Length == 0)
        {
            Debug.Log($"Pack {selectedPack.packIndex} has no characters!");
            ResetPackState();
            yield break;
        }

        int randomIndex = Random.Range(0, selectedPack.characterIndices.Length);
        int randomCharacterIndex = selectedPack.characterIndices[randomIndex];

        Debug.Log($"Pack {selectedPack.packIndex} - Random Character Index: {randomCharacterIndex}");

        if (selectedPack.packRect != null)
        {
            yield return StartCoroutine(MoveToCenter(selectedPack.packRect, moveToCenterDuration));
            SoundManager.Instance.PlayOpenPack();
            yield return StartCoroutine(ScaleToZero(selectedPack.packRect, hideSelectedPackDuration));
        }

        if (cardManager != null)
        {
            yield return StartCoroutine(cardManager.ShowCardByCharacterIndex(randomCharacterIndex));
        }

        yield return new WaitUntil(() => !cardManager.IsProcessing());

        ResetPackState();
        isAnimating = false;
    }

    private void ResetPackState()
    {
        foreach (PackData pack in packs)
        {
            if (pack.packRect != null)
            {
                pack.packRect.position = pack.initialPosition;
                pack.packRect.localScale = Vector3.one;
            }

            if (pack.packButton != null)
            {
                pack.packButton.interactable = true;
            }
        }
    }

    private IEnumerator ScaleToZero(RectTransform target, float duration)
    {
        if (target == null) yield break;

        Vector3 startScale = target.localScale;
        Vector3 endScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        target.localScale = endScale;
    }

    private IEnumerator MoveToCenter(RectTransform target, float duration)
    {
        if (target == null || canvasRect == null) yield break;

        Vector3 startPosition = target.position;
        Vector3 targetPosition = canvasRect.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        target.position = targetPosition;
    }

    private void OnDestroy()
    {
        foreach (PackData pack in packs)
        {
            if (pack.packButton != null)
                pack.packButton.onClick.RemoveAllListeners();
        }
    }
}