using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CardManager : MonoBehaviour
{
    [System.Serializable]
    public struct CardData
    {
        public int cardIndex;
        public GameObject card;
        public RectTransform cardRect;
        public Vector3 initialPosition;
    }

    [SerializeField] private GameObject[] cardsObj;
    [SerializeField] private CardData[] cards;
    [SerializeField] private UINavigation uINavigation;
    [SerializeField] private CollectionManager collectionManager;
    [SerializeField] private Button getRewardButton;
    [SerializeField] private float showCardDuration;
    [SerializeField] private float buttonAppearDuration;
    [SerializeField] private float moveToCollectionDuration;

    private int currentOpenedCardIndex = -1;
    private bool isProcessing = false;
    private RectTransform currentActiveCardRect;
    private Vector2 defaultCardAnchoredPosition;

    private void Awake()
    {
        if (uINavigation == null) uINavigation = GetComponent<UINavigation>();
        if (collectionManager == null) collectionManager = GetComponent<CollectionManager>();

        if (getRewardButton != null)
        {
            getRewardButton.gameObject.SetActive(false);
            getRewardButton.onClick.AddListener(OnRewardButtonClick);
        }

        if (cardsObj != null && cardsObj.Length > 0 && cardsObj[0] != null)
        {
            RectTransform rect = cardsObj[0].GetComponent<RectTransform>();
            if (rect != null)
            {
                defaultCardAnchoredPosition = rect.anchoredPosition;
            }
        }
    }

    public bool IsProcessing()
    {
        return isProcessing;
    }



    public IEnumerator ShowCardByCharacterIndex(int characterIndex)
    {
        isProcessing = true;
        currentOpenedCardIndex = characterIndex;

        if (getRewardButton != null)
        {
            getRewardButton.gameObject.SetActive(false);
        }

        yield return StartCoroutine(HideAllCards());

        CardData targetCard = FindCardByCharacterIndex(characterIndex);

        if (targetCard.card == null || targetCard.cardRect == null)
        {
            isProcessing = false;
            yield break;
        }

        currentActiveCardRect = targetCard.cardRect;
        targetCard.card.SetActive(true);
        targetCard.cardRect.localScale = Vector3.zero;

        float halfDuration = showCardDuration * 0.5f;

        yield return StartCoroutine(ScaleCard(targetCard.cardRect, Vector3.zero, Vector3.one * 1.2f, halfDuration));
        
        yield return StartCoroutine(ScaleCard(targetCard.cardRect, Vector3.one * 1.2f, Vector3.one, halfDuration));

        SoundManager.Instance.PlayOpenCard();

        if (getRewardButton != null)
        {
            getRewardButton.gameObject.SetActive(true);
            RectTransform btnRect = getRewardButton.GetComponent<RectTransform>();
            if (btnRect != null)
            {
                btnRect.localScale = Vector3.zero;
                float halfBtnDuration = buttonAppearDuration * 0.5f;
                yield return StartCoroutine(ScaleCard(btnRect, Vector3.zero, Vector3.one * 1.2f, halfBtnDuration));
                yield return StartCoroutine(ScaleCard(btnRect, Vector3.one * 1.2f, Vector3.one, halfBtnDuration));
            }
        }
    }

    private void OnRewardButtonClick()
    {
        Debug.Log($"Reward claimed for card index: {currentOpenedCardIndex}");

        SoundManager.Instance.PlayClick();

        if (getRewardButton != null)
        {
            getRewardButton.gameObject.SetActive(false);
        }

        if (currentActiveCardRect != null && uINavigation != null)
        {
            StartCoroutine(MoveCardToCollection(currentActiveCardRect));
        }
    }

    private IEnumerator MoveCardToCollection(RectTransform cardRect)
    {
        if (uINavigation == null) yield break;

        Button collectionBtn = uINavigation.GetCollectionButton();
        if (collectionBtn == null) yield break;

        RectTransform targetRect = uINavigation.GetTarget();
        if (targetRect == null) yield break;

        collectionBtn.gameObject.SetActive(true);
        collectionBtn.interactable = false;

        Vector3 startPos = cardRect.position;
        Vector3 endPos = targetRect.position;
        Vector3 startScale = cardRect.localScale;

        float elapsed = 0f;

        SoundManager.Instance.PlayWhoosh();

        while (elapsed < moveToCollectionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveToCollectionDuration;

            cardRect.position = Vector3.Lerp(startPos, endPos, t);
            cardRect.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        cardRect.position = endPos;
        cardRect.localScale = Vector3.zero;

        yield return null;

        if (cardRect != null)
        {
            cardRect.anchoredPosition = defaultCardAnchoredPosition;
            cardRect.localScale = Vector3.one;

            if (cardRect.gameObject != null)
            {
                cardRect.gameObject.SetActive(false);
            }
        }

        if (collectionManager != null)
        {
            collectionManager.RevealCharacter(currentOpenedCardIndex);
        }

        currentActiveCardRect = null;
        collectionBtn.interactable = true;
        isProcessing = false;
    }

    public IEnumerator HideAllCards()
    {
        foreach (CardData card in cards)
        {
            if (card.card != null && card.card.activeSelf && card.cardRect != null)
            {
                yield return StartCoroutine(ScaleCard(card.cardRect, card.cardRect.localScale, Vector3.zero, showCardDuration));
                card.card.SetActive(false);
            }
        }
    }

    private IEnumerator ScaleCard(RectTransform cardRect, Vector3 startScale, Vector3 endScale, float duration)
    {
        if (cardRect == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cardRect.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        cardRect.localScale = endScale;
    }

    private CardData FindCardByCharacterIndex(int characterIndex)
    {
        foreach (CardData card in cards)
        {
            if (card.card != null && card.cardIndex == characterIndex)
            {
                return card;
            }
        }

        return default(CardData);
    }




    [ContextMenu("Initialize Cards")]
    private void InitializeCards()
    {
        if (cardsObj == null || cardsObj.Length == 0)
        {
            return;
        }

        cards = new CardData[cardsObj.Length];

        for (int i = 0; i < cardsObj.Length; i++)
        {
            if (cardsObj[i] == null) continue;

            RectTransform rect = cardsObj[i].GetComponent<RectTransform>();

            cards[i] = new CardData
            {
                cardIndex = i,
                card = cardsObj[i],
                cardRect = rect,
                initialPosition = rect != null ? rect.position : Vector3.zero
            };

            if (cards[i].cardRect != null)
            {
                cards[i].cardRect.localScale = Vector3.zero;
            }

            if (cards[i].card != null)
            {
                cards[i].card.SetActive(false);
            }
        }
    }

    [ContextMenu("Refresh Card Rects")]
    private void RefreshCardRects()
    {
        if (cards == null || cards.Length == 0)
        {
            return;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i].card != null)
            {
                RectTransform rect = cards[i].card.GetComponent<RectTransform>();
                if (rect != null)
                {
                    cards[i].cardRect = rect;
                    cards[i].initialPosition = rect.position;
                }
            }
        }
    }
}