using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CollectionManager : MonoBehaviour
{
    [System.Serializable]
    public struct CollectionItem
    {
        public int index;
        public Image characterImage;
    }

    [SerializeField] private GameObject[] collectionItemsObj;
    [SerializeField] private List<CollectionItem> collectionItems = new List<CollectionItem>();

    private const string UNLOCKED_PREFIX = "UnlockedChar_";

    private void Start()
    {
        LoadUnlockedCharacters();
    }

    public void RevealCharacter(int characterIndex)
    {
        SaveCharacterUnlock(characterIndex);
        UpdateCharacterVisual(characterIndex);
    }

    private void SaveCharacterUnlock(int index)
    {
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + index, 1);
        PlayerPrefs.Save();
    }

    private void LoadUnlockedCharacters()
    {
        foreach (var item in collectionItems)
        {
            if (PlayerPrefs.GetInt(UNLOCKED_PREFIX + item.index, 0) == 1)
            {
                UpdateCharacterVisual(item.index);
            }
        }
    }

    private void UpdateCharacterVisual(int index)
    {
        foreach (var item in collectionItems)
        {
            if (item.index == index && item.characterImage != null)
            {
                Color currentColor = item.characterImage.color;
                currentColor.a = 1f;
                currentColor.r = 1f;
                currentColor.g = 1f;
                currentColor.b = 1f;

                item.characterImage.color = currentColor;
                break;
            }
        }
    }

    [ContextMenu("Initialize Collection")]
    private void InitializeCollection()
    {
        collectionItems.Clear();

        if (collectionItemsObj == null || collectionItemsObj.Length == 0)
        {
            Debug.LogWarning("Collection items array is empty.");
            return;
        }

        for (int i = 0; i < collectionItemsObj.Length; i++)
        {
            if (collectionItemsObj[i] == null) continue;

            Image charImage = null;

            Transform childTransform = collectionItemsObj[i].transform.Find("Character");
            if (childTransform != null)
            {
                charImage = childTransform.GetComponent<Image>();
            }

            if (charImage != null)
            {
                CollectionItem item = new CollectionItem
                {
                    index = i,
                    characterImage = charImage
                };

                collectionItems.Add(item);
            }
            else
            {
                Debug.LogWarning($"'Character' Image not found in {collectionItemsObj[i].name}");
            }
        }

        Debug.Log($"Collection initialized with {collectionItems.Count} items.");
    }
}