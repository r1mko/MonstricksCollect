using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PackManager : MonoBehaviour
{
    // --- ДАТА ПАКОВ (Упрощённая по ТЗ) ---
    [System.Serializable]
    public struct PackData
    {
        public int packIndex;
        public int[] characterIndices;
    }

    // --- ДАТА КНОПОК (Новая) ---
    [System.Serializable]
    public struct ButtonData
    {
        public Button button;
        public int packIndex;
        public RectTransform rectTransform;
        public Vector3 initialPosition;
        public Vector3 initialRotation; // Euler angles (Z для 2D)
    }

    [Header("Пакеты")]
    [SerializeField] private PackData[] packData;

    [Header("Инициализация кнопок")]
    [SerializeField] private GameObject[] buttonGameObjects;
    [SerializeField] private ButtonData[] packsButton;

    /// <summary>
    /// ПКМ по компоненту в инспекторе -> "Initialize Buttons From GameObjects"
    /// Автоматически собирает компоненты, сохраняет позиции/повороты и определяет packIndex по имени спрайта.
    /// </summary>
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

            if (btn == null || rt == null)
            {
                Debug.LogWarning($"[PackManager] У объекта '{go.name}' отсутствует Button или RectTransform. Пропуск.");
                continue;
            }

            // Сохраняем начальную позицию и поворот (Z для 2D UI)
            Vector3 initialPos = rt.position;
            Vector3 initialRot = rt.eulerAngles;

            // Определяем packIndex по спрайту в Image
            int detectedPackIndex = GetPackIndexFromSprite(go);

            packsButton[i] = new ButtonData
            {
                button = btn,
                rectTransform = rt,
                initialPosition = initialPos,
                initialRotation = initialRot,
                packIndex = detectedPackIndex
            };
        }

        Debug.Log($"[PackManager] Успешно инициализировано {packsButton.Length} кнопок. Индексы паков определены по спрайтам.");
    }

    [ContextMenu("Randomize Rotations")]
    public void RandomizeRotations()
    {
        if (packsButton == null || packsButton.Length == 0) return;

        foreach (var data in packsButton)
        {
            if (data.rectTransform != null)
            {
                float randomZ;
                if (Random.value < 0.6f)
                {
                    int range = Random.Range(0, 2);
                    if (range == 0)
                    {
                        randomZ = Random.Range(-45f, 45f);
                    }
                    else
                    {
                        randomZ = Random.Range(-220f, -140f);
                    }
                }
                else
                {
                    randomZ = Random.Range(0f, 360f);
                }

                Vector3 newRotation = new Vector3(data.rectTransform.eulerAngles.x, data.rectTransform.eulerAngles.y, randomZ);
                data.rectTransform.rotation = Quaternion.Euler(newRotation);
            }
        }
    }

    /// <summary>
    /// Ищет Image на объекте, проверяет имя спрайта и возвращает соответствующий packIndex.
    /// Ожидает имена спрайтов: pack1, pack2, pack3, pack4 (регистр не важен).
    /// </summary>
    private int GetPackIndexFromSprite(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        if (img == null || img.sprite == null)
        {
            Debug.LogWarning($"[PackManager] У объекта '{go.name}' нет Image или спрайта. packIndex = 0 (по умолчанию).");
            return 0;
        }

        string spriteName = img.sprite.name.ToLowerInvariant();

        // Ищем паттерн "packX" где X - цифра
        if (spriteName.Contains("pack"))
        {
            foreach (char c in spriteName)
            {
                if (char.IsDigit(c))
                {
                    int index = int.Parse(c.ToString());
                    // Debug.Log($"[PackManager] Объект '{go.name}' -> спрайт '{img.sprite.name}' -> packIndex = {index}");
                    return index;
                }
            }
        }

        Debug.LogWarning($"[PackManager] Не удалось распознать packIndex по спрайту '{img.sprite.name}' на объекте '{go.name}'. Установлен 0.");
        return 0;
    }

    /// <summary>
    /// ПКМ -> "Reset Pack Indexes to Default" - сбросит все packIndex в 0, если нужно переинициализировать.
    /// </summary>
    [ContextMenu("Reset Pack Indexes to Default")]
    private void ResetPackIndexes()
    {
        for (int i = 0; i < packsButton.Length; i++)
        {
            packsButton[i].packIndex = 0;
        }
        Debug.Log("[PackManager] packIndex сброшен на 0 для всех кнопок.");
    }
}