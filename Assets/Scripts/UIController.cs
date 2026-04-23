using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class UIController : MonoBehaviour
{
    private Label _breadCountLabel;
    private Label _breadPerSecondLabel;
    private Button _breadButton;
    private VisualElement _upgradeList;

    [Header("Effects")]
    [SerializeField] private Sprite _breadParticleSprite;

    [Header("Offline Earnings Popup")]
private VisualElement _offlinePopup;
    private Label _offlineAmountText;
    private Button _closePopupButton;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _breadCountLabel = root.Q<Label>("bread-count");
        _breadPerSecondLabel = root.Q<Label>("bread-per-second");
        _breadButton = root.Q<Button>("bread-button");
        _upgradeList = root.Q<VisualElement>("upgrade-list");

        _offlinePopup = root.Q<VisualElement>("offline-popup");
        _offlineAmountText = root.Q<Label>("offline-amount-text");
        _closePopupButton = root.Q<Button>("close-popup-button");

        if (_breadButton != null)
        {
            // Use PointerDown to get click position
            _breadButton.RegisterCallback<PointerDownEvent>(OnBreadButtonPointerDown);
        }

        if (_closePopupButton != null) _closePopupButton.clicked += () => _offlinePopup.style.display = DisplayStyle.None;

        InitializeUpgrades();
        RefreshUI();

        GameManager.Instance.OnBreadChanged += RefreshUI;
        GameManager.Instance.OnOfflineEarningsCalculated += ShowOfflineEarnings;
    }

    private void OnBreadButtonPointerDown(PointerDownEvent evt)
    {
        GameManager.Instance.ClickBread();
        SpawnBreadParticle(evt.position);
        SpawnFloatingText(evt.position, $"+{GameManager.Instance.BreadPerClick:F0}");
        ApplyButtonThump();
    }

    private void SpawnFloatingText(Vector2 position, string text)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var label = new Label(text);
        label.AddToClassList("floating-text");
        label.pickingMode = PickingMode.Ignore;

        Vector2 localPos = root.WorldToLocal(position);
        label.style.left = localPos.x - 30; // Center roughly
        label.style.top = localPos.y - 30;

        root.Add(label);

        float duration = 1.0f;
        float startTime = Time.time;
        float targetY = localPos.y - 200;

        label.schedule.Execute(() => {
            float elapsed = Time.time - startTime;
            float t = elapsed / duration;

            if (t >= 1.0f)
            {
                label.RemoveFromHierarchy();
                return;
            }

            label.style.top = Mathf.Lerp(localPos.y - 30, targetY, t);
            label.style.opacity = 1.0f - t;
        }).Until(() => Time.time - startTime >= duration);
    }

    private void ApplyButtonThump()
    {
        if (_breadButton == null) return;

        // Reset scale first
        _breadButton.style.scale = new Scale(new Vector3(1.15f, 1.15f, 1));
        
        float duration = 0.15f;
        float startTime = Time.time;

        _breadButton.schedule.Execute(() => {
            float elapsed = Time.time - startTime;
            float t = elapsed / duration;

            if (t >= 1.0f)
            {
                _breadButton.style.scale = StyleKeyword.Null;
                return;
            }

            float scale = Mathf.Lerp(1.15f, 1.0f, t);
            _breadButton.style.scale = new Scale(new Vector3(scale, scale, 1));
        }).Until(() => Time.time - startTime >= duration);
    }

    private void SpawnBreadParticle(Vector2 position)
{
        var root = GetComponent<UIDocument>().rootVisualElement;
        var particle = new VisualElement();
        
        if (_breadParticleSprite != null)
        {
            particle.style.backgroundImage = new StyleBackground(_breadParticleSprite);
        }
        else
        {
            particle.AddToClassList("click-particle");
        }

        particle.style.position = Position.Absolute;
        particle.pickingMode = PickingMode.Ignore;

        // Center particle on click
        float size = 120f;
        particle.style.width = size;
        particle.style.height = size;
        
        // Convert screen-like position to root local coordinates
        Vector2 localPos = root.WorldToLocal(position);
        particle.style.left = localPos.x - (size / 2f);
        particle.style.top = localPos.y - (size / 2f);

        root.Add(particle);

        // Animate
        float duration = 0.8f;
        float startTime = Time.time;
        float randomX = UnityEngine.Random.Range(-150f, 150f);
        float targetY = localPos.y - UnityEngine.Random.Range(300f, 500f);
        float startRotation = UnityEngine.Random.Range(0f, 360f);
        float rotationSpeed = UnityEngine.Random.Range(-400f, 400f);

        particle.schedule.Execute(() => {
            float elapsed = Time.time - startTime;
            float t = elapsed / duration;

            if (t >= 1.0f)
            {
                particle.RemoveFromHierarchy();
                return;
            }

            // Path calculation
            float currentY = Mathf.Lerp(localPos.y - (size / 2f), targetY, t);
            float currentX = Mathf.Lerp(localPos.x - (size / 2f), localPos.x - (size / 2f) + randomX, t);

            particle.style.top = currentY;
            particle.style.left = currentX;
            particle.style.opacity = 1.0f - t;
            particle.style.rotate = new Rotate(new Angle(startRotation + rotationSpeed * t));
            
            float scale = t < 0.15f ? Mathf.Lerp(0.5f, 1.5f, t / 0.15f) : Mathf.Lerp(1.5f, 0.5f, (t - 0.15f) / 0.85f);
            particle.style.scale = new Scale(new Vector3(scale, scale, 1));

        }).Until(() => Time.time - startTime >= duration);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBreadChanged -= RefreshUI;
            GameManager.Instance.OnOfflineEarningsCalculated -= ShowOfflineEarnings;
        }
    }

    private void InitializeUpgrades()
    {
        _upgradeList.Clear();
        foreach (var upgrade in GameManager.Instance.Upgrades)
        {
            var item = CreateUpgradeUI(upgrade);
            _upgradeList.Add(item);
        }
    }

    private VisualElement CreateUpgradeUI(UpgradeData upgrade)
    {
        var container = new Button();
        container.AddToClassList("upgrade-item");
        
        var infoContainer = new VisualElement();
        infoContainer.AddToClassList("upgrade-info");

        var nameLabel = new Label(upgrade.Name);
        nameLabel.AddToClassList("upgrade-name");
        
        var costLabel = new Label($"Cost: {upgrade.CurrentCost:F0}");
        costLabel.AddToClassList("upgrade-cost");
        
        infoContainer.Add(nameLabel);
        infoContainer.Add(costLabel);
        
        var levelContainer = new VisualElement();
        levelContainer.AddToClassList("upgrade-level-container");

        var levelLabel = new Label($"{upgrade.Level}");
        levelLabel.AddToClassList("upgrade-level");
        
        levelContainer.Add(levelLabel);

        container.Add(infoContainer);
        container.Add(levelContainer);

        container.clicked += () => {
            if (GameManager.Instance.TryBuyUpgrade(upgrade))
            {
                costLabel.text = $"Cost: {upgrade.CurrentCost:F0}";
                levelLabel.text = $"{upgrade.Level}";
                RefreshUI();
            }
        };

        return container;
    }

    private void ShowOfflineEarnings(double amount)
    {
        if (_offlinePopup != null && _offlineAmountText != null)
        {
            _offlineAmountText.text = $"You earned {Mathf.Floor((float)amount)} bread while you were away!";
            _offlinePopup.style.display = DisplayStyle.Flex;
        }
    }

    private void RefreshUI()
{
        if (_breadCountLabel != null) _breadCountLabel.text = $"{Mathf.Floor((float)GameManager.Instance.TotalBread)} Bread";
        if (_breadPerSecondLabel != null) _breadPerSecondLabel.text = $"{GameManager.Instance.BreadPerSecond:F1} per second";
    }
}
