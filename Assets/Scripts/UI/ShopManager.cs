using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] private UIDocument shopUIDocument;
    
    private VisualElement _root;
    private VisualElement _shopRoot;
    private VisualElement _itemContainer;
    private Button _btnClose;
    private Label _coinValue;

    private bool _isOpen = false;

    // Mock Data
    private class ShopItem
    {
        public string Name;
        public int Price;
        public Color Color;
        public bool IsPurchased;
        public bool IsEquipped;
    }

    private List<ShopItem> _items = new List<ShopItem>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (shopUIDocument == null) shopUIDocument = GetComponent<UIDocument>();
        
        _root = shopUIDocument.rootVisualElement;
        _shopRoot = _root.Q<VisualElement>("ShopRoot");
        _itemContainer = _root.Q<VisualElement>("ItemContainer");
        _btnClose = _root.Q<Button>("BtnCloseShop");
        _coinValue = _root.Q<Label>("ShopCoinValue");

        if (_btnClose != null) _btnClose.clicked += CloseShop;

        InitializeMockItems();
        RefreshUI();
    }

    private void InitializeMockItems()
    {
        _items.Add(new ShopItem { Name = "기본 공", Price = 0, Color = new Color(0, 0.95f, 1), IsPurchased = true, IsEquipped = true });
        _items.Add(new ShopItem { Name = "불꽃 공", Price = 500, Color = new Color(1, 0.2f, 0), IsPurchased = false, IsEquipped = false });
        _items.Add(new ShopItem { Name = "전기 공", Price = 1000, Color = new Color(1, 0.9f, 0), IsPurchased = false, IsEquipped = false });
        _items.Add(new ShopItem { Name = "얼음 공", Price = 1500, Color = new Color(0, 0.5f, 1), IsPurchased = false, IsEquipped = false });
        _items.Add(new ShopItem { Name = "네온 핑크", Price = 2000, Color = new Color(1, 0, 0.5f), IsPurchased = false, IsEquipped = false });
        _items.Add(new ShopItem { Name = "바이러스", Price = 3000, Color = new Color(0.5f, 0, 1), IsPurchased = false, IsEquipped = false });
    }

    public void OpenShop()
    {
        _isOpen = true;
        _shopRoot.style.display = DisplayStyle.Flex;
        // 약간의 지연 후 애니메이션 클래스 추가 (트랜지션 효과를 위해)
        _shopRoot.RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
    }

    private void OnFirstLayout(GeometryChangedEvent evt)
    {
        _shopRoot.UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);
        _shopRoot.AddToClassList("shop-root--open");
        UpdateCoinDisplay();
    }

    public void CloseShop()
    {
        _isOpen = false;
        _shopRoot.RemoveFromClassList("shop-root--open");
        // 애니메이션이 끝난 후 숨기기 (0.5초 뒤)
        _shopRoot.schedule.Execute(() => {
            if (!_isOpen) _shopRoot.style.display = DisplayStyle.None;
        }).StartingIn(500);
    }

    private void UpdateCoinDisplay()
    {
        // Get coins from PlayerPrefs or a GameManager
        int coins = PlayerPrefs.GetInt("Coins", 1250); // Default mock value
        if (_coinValue != null) _coinValue.text = coins.ToString();
    }

    private void RefreshUI()
    {
        _itemContainer.Clear();

        foreach (var item in _items)
        {
            _itemContainer.Add(CreateItemElement(item));
        }
    }

    private VisualElement CreateItemElement(ShopItem item)
    {
        VisualElement card = new VisualElement();
        card.AddToClassList("shop-item-card");
        if (item.IsEquipped) card.AddToClassList("shop-item-card--equipped");

        VisualElement preview = new VisualElement();
        preview.AddToClassList("item-preview");
        preview.style.backgroundColor = item.Color;
        // Simple glow simulation
        preview.style.borderLeftColor = item.Color;
        preview.style.borderRightColor = item.Color;
        preview.style.borderTopColor = item.Color;
        preview.style.borderBottomColor = item.Color;
        preview.style.borderLeftWidth = 2;
        preview.style.borderRightWidth = 2;
        preview.style.borderTopWidth = 2;
        preview.style.borderBottomWidth = 2;
        card.Add(preview);

        Label name = new Label(item.Name);
        name.AddToClassList("item-name");
        card.Add(name);

        if (item.IsEquipped)
        {
            Label status = new Label("현재 사용중");
            status.AddToClassList("item-status");
            card.Add(status);
        }
        else if (item.IsPurchased)
        {
            Button btnEquip = new Button(() => EquipItem(item));
            btnEquip.text = "[ EQUIP ]";
            btnEquip.AddToClassList("btn-buy");
            card.Add(btnEquip);
        }
        else
        {
            VisualElement priceBox = new VisualElement();
            priceBox.AddToClassList("item-price-box");
            
            Label price = new Label($"{item.Price}");
            price.AddToClassList("item-price-value");
            priceBox.Add(price);
            card.Add(priceBox);

            Button btnBuy = new Button(() => BuyItem(item));
            btnBuy.text = "[ PURCHASE ]";
            btnBuy.AddToClassList("btn-buy");
            card.Add(btnBuy);
        }

        return card;
    }

    private void BuyItem(ShopItem item)
    {
        int currentCoins = PlayerPrefs.GetInt("Coins", 1250);
        if (currentCoins >= item.Price)
        {
            currentCoins -= item.Price;
            PlayerPrefs.SetString("Coins", currentCoins.ToString()); // Using string for safety in some versions or int
            PlayerPrefs.SetInt("Coins", currentCoins);
            PlayerPrefs.Save();
            
            item.IsPurchased = true;
            Debug.Log($"Purchased {item.Name}!");
            UpdateCoinDisplay();
            RefreshUI();
        }
        else
        {
            Debug.Log("Not enough coins!");
        }
    }

    private void EquipItem(ShopItem item)
    {
        foreach (var i in _items) i.IsEquipped = false;
        item.IsEquipped = true;
        Debug.Log($"Equipped {item.Name}!");
        RefreshUI();
    }
}
