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
        public Vector3 initialPosition;
        public Vector3 initialRotation;
        public Vector2 initialSize;
    }

    [SerializeField] private CardManager cardManager;
    [SerializeField] private UINavigation uINavigation;
    [SerializeField] private GameObject[] buttonGameObjects; // контейнер для ссылок
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
        for (int i = 0; i < packsButton.Length; i++)
        {
            int index = i;
            if (packsButton[index].button != null)
            {
                packsButton[index].button.onClick.AddListener(() => OnPackClick(packsButton[index]));
            }
        }

        shuffleButton.onClick.AddListener(RefreshAllPacks);

        RandomizeRotations();
        ShufflePackPositionsOnly();
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

        Vector3 startPos = rt.position;
        Vector2 startSize = new Vector2(rt.rect.width, rt.rect.height);
        Quaternion startRot = rt.rotation; // Запоминаем текущее (случайное) вращение

        Vector3 targetPos = canvasRect.position;
        Vector2 targetSize = startSize * 2f;
        Quaternion targetRot = Quaternion.Euler(0, 0, 0); // Целевое вращение - 0 градусов

        float elapsed = 0f;
        while (elapsed < moveToCenterDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveToCenterDuration;

            // Позиция
            rt.position = Vector3.Lerp(startPos, targetPos, t);

            // Размер
            float currentWidth = Mathf.Lerp(startSize.x, targetSize.x, t);
            float currentHeight = Mathf.Lerp(startSize.y, targetSize.y, t);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);

            // Вращение (выравнивание к 0)
            rt.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        // Фиксация финальных значений
        rt.position = targetPos;
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

        yield return StartCoroutine(ResetPackState(buttonData));

        CheckAndRefreshAllPacks();

        isAnimating = false;
    }

    private IEnumerator ResetPackState(ButtonData data)
    {
        RectTransform rt = data.rectTransform;
        if (rt == null) yield break;

        rt.gameObject.SetActive(false);
        rt.position = data.initialPosition;
        rt.rotation = Quaternion.Euler(data.initialRotation);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, data.initialSize.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, data.initialSize.y);


        yield return null;
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
        ShufflePackPositionsOnly();

        if (collectionButton != null) collectionButton.SetActive(true);
        if (shuffleButton != null) shuffleButton.gameObject.SetActive(true);
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

    [ContextMenu("Shuffle Pack Positions Only")]
    public void ShufflePackPositionsOnly()
    {
        if (packsButton == null || packsButton.Length < 2) return;

        // 1. Собираем все изначальные позиции в отдельный список
        List<Vector3> positions = new List<Vector3>();
        foreach (var data in packsButton)
        {
            positions.Add(data.initialPosition);
        }

        // 2. Перемешиваем список позиций алгоритмом Фишера-Йетса
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector3 temp = positions[i];
            positions[i] = positions[j];
            positions[j] = temp;
        }

        // 3. Присваиваем перемешанные позиции обратно в наши данные
        for (int i = 0; i < packsButton.Length; i++)
        {
            packsButton[i].initialPosition = positions[i];

            if (packsButton[i].rectTransform != null)
            {
                packsButton[i].rectTransform.position = packsButton[i].initialPosition;
            }
        }

        ShuffleSiblingOrder();
        Debug.Log("[PackManager] Позиции паков перемешаны.");
    }

    [ContextMenu("Shuffle Sibling Order")]
    public void ShuffleSiblingOrder()
    {
        if (packsButton == null || packsButton.Length < 2) return;

        // Создаем список индексов от 0 до N-1
        List<int> indices = new List<int>();
        for (int i = 0; i < packsButton.Length; i++)
        {
            indices.Add(i);
        }

        // Перемешиваем список индексов (Фишер-Йетс)
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[j];
            indices[j] = temp;
        }

        // Применяем новые порядковые номера (слои) к RectTransform
        for (int i = 0; i < packsButton.Length; i++)
        {
            if (packsButton[i].rectTransform != null)
            {
                packsButton[i].rectTransform.SetSiblingIndex(indices[i]);
            }
        }

        Debug.Log("[PackManager] Порядок слоев (Sibling Index) перемешан.");
    }

    [ContextMenu("Initialize Buttons From GameObjects")]
    private void InitializeButtons()
    {
        if (buttonGameObjects == null || buttonGameObjects.Length == 0)
        {
            Debug.LogWarning("[PackManager] Массив buttonGameObjects пуст. Перетащите кнопки в инспекторе.");
            return;
        }

        packsButton = new ButtonData[buttonGameObjects.Length];

        for (int i = 0; i < buttonGameObjects.Length; i++)
        {
            GameObject go = buttonGameObjects[i];
            if (go == null) continue;

            Button btn = go.GetComponent<Button>();
            RectTransform rt = go.GetComponent<RectTransform>();
            Image img = go.GetComponent<Image>();

            if (btn == null || rt == null)
            {
                Debug.LogWarning($"[PackManager] У объекта '{go.name}' отсутствует Button или RectTransform. Пропуск.");
                continue;
            }

            Vector3 initialPos = rt.position;
            Vector3 initialRot = rt.eulerAngles;
            Vector2 initialSz = new Vector2(rt.rect.width, rt.rect.height);

            int detectedPackIndex = GetPackIndexFromSprite(go);

            packsButton[i] = new ButtonData
            {
                button = btn,
                rectTransform = rt,
                initialPosition = initialPos,
                initialRotation = initialRot,
                initialSize = initialSz,
                packIndex = detectedPackIndex
            };
        }

        Debug.Log($"[PackManager] Успешно инициализировано {packsButton.Length} кнопок.");
    }

    private int GetPackIndexFromSprite(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        if (img == null || img.sprite == null)
        {
            return 0;
        }

        string spriteName = img.sprite.name.ToLowerInvariant();

        if (spriteName.Contains("pack"))
        {
            foreach (char c in spriteName)
            {
                if (char.IsDigit(c))
                {
                    return int.Parse(c.ToString());
                }
            }
        }

        return 0;
    }

    private void OnDestroy()
    {
        foreach (var data in packsButton)
        {
            if (data.button != null) data.button.onClick.RemoveAllListeners();
        }
    }
}