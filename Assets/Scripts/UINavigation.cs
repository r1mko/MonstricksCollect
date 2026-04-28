using UnityEngine;
using UnityEngine.UI;
using YG;

public class UINavigation : MonoBehaviour
{
    [SerializeField] private GameObject collectionScreen;
    [SerializeField] private GameObject packsScreen;
    [SerializeField] private Button collectionButton;
    [SerializeField] private RectTransform cardAnimationTarget;
    [SerializeField] private Button packsButton;

    private void Start()
    {
        collectionButton.onClick.AddListener(ShowCollectionScreen);
        packsButton.onClick.AddListener(ShowPacksScreen);
    }

    private void ShowCollectionScreen()
    {
        YG2.InterstitialAdvShow();
        SoundManager.Instance.PlayClick();
        collectionScreen.SetActive(true);
        packsScreen.SetActive(false);
    }

    private void ShowPacksScreen()
    {
        YG2.InterstitialAdvShow();
        SoundManager.Instance.PlayClick();
        collectionScreen.SetActive(false);
        packsScreen.SetActive(true);
    }

    public void CollectionButtonVisibility(bool value)
    {
        collectionButton.gameObject.SetActive(value);
    }

    public Button GetCollectionButton()
    {
        return collectionButton;
    }

    public RectTransform GetTarget()
    {
        return cardAnimationTarget;
    }

    private void OnDestroy()
    {
        collectionButton.onClick.RemoveListener(ShowCollectionScreen);
        packsButton.onClick.RemoveListener(ShowPacksScreen);
    }
}