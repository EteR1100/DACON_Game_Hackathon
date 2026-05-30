using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] private UIDocument shopUIDocument;

    private VisualElement _shopRoot;
    private VisualElement _itemContainer;
    private Button _btnClose;
    private Label _coinValue;

    private bool _isOpen;

    private class ShopItem
    {
        public string Name;
        public int Price;
        public Color Color;
        public bool IsPurchased;
        public bool IsEquipped;
    }

    private readonly List<ShopItem> _items = new List<ShopItem>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (shopUIDocument == null)
        {
            shopUIDocument = GetComponent<UIDocument>();
        }

        var root = shopUIDocument.rootVisualElement;
        _shopRoot = root.Q<VisualElement>("ShopRoot");
        _itemContainer = root.Q<VisualElement>("ItemContainer");
        _btnClose = root.Q<Button>("BtnCloseShop");
        _coinValue = root.Q<Label>("ShopCoinValue");

        if (_btnClose != null)
        {
            _btnClose.clicked += CloseShop;
        }

        InitializeMockItems();
        RefreshUI();
    }

    private void InitializeMockItems()
    {
        _items.Clear();
        _items.Add(new ShopItem { Name = "Default Core", Price = 0, Color = new Color(0, 0.95f, 1), IsPurchased = true, IsEquipped = true });
        _items.Add(new ShopItem { Name = "Crimson Core", Price = 500, Color = new Color(1, 0.2f, 0), IsPurchased = false, IsEquipped = false });
        _items.Add(new ShopItem { Name = "Solar Core", Price = 1000, Color = new Color(1, 0.9f, 0), IsPurchased = false, IsEquipped = false });
        _items.Add(new ShopItem { Name = "Frost Core", Price = 1500, Color = new Color(0, 0.5f, 1), IsPurchased = false, IsEquipped = false });
        _items.Add(new ShopItem { Name = "Neon Pink", Price = 2000, Color = new Color(1, 0, 0.5f), IsPurchased = false, IsEquipped = false });
        _items.Add(new ShopItem { Name = "Violet Core", Price = 3000, Color = new Color(0.5f, 0, 1), IsPurchased = false, IsEquipped = false });
    }

    public void OpenShop()
    {
        if (_shopRoot == null)
        {
            return;
        }

        _isOpen = true;
        _shopRoot.style.display = DisplayStyle.Flex;
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
        if (_shopRoot == null)
        {
            return;
        }

        _isOpen = false;
        _shopRoot.RemoveFromClassList("shop-root--open");
        _shopRoot.schedule.Execute(() =>
        {
            if (!_isOpen)
            {
                _shopRoot.style.display = DisplayStyle.None;
            }
        }).StartingIn(500);
    }

    private void UpdateCoinDisplay()
    {
        int coins = PlayerPrefs.GetInt("Coins", 1250);
        if (_coinValue != null)
        {
            _coinValue.text = coins.ToString();
        }
    }

    private void RefreshUI()
    {
        if (_itemContainer == null)
        {
            return;
        }

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
        if (item.IsEquipped)
        {
            card.AddToClassList("shop-item-card--equipped");
        }

        VisualElement preview = new VisualElement();
        preview.AddToClassList("item-preview");
        preview.style.backgroundColor = item.Color;
        preview.style.borderLeftColor = item.Color;
        preview.style.borderRightColor = item.Color;
        preview.style.borderTopColor = item.Color;
        preview.style.borderBottomColor = item.Color;
        card.Add(preview);

        Label name = new Label(item.Name);
        name.AddToClassList("item-name");
        card.Add(name);

        if (item.IsEquipped)
        {
            Label status = new Label("EQUIPPED");
            status.AddToClassList("item-status");
            card.Add(status);
        }
        else if (item.IsPurchased)
        {
            Button btnEquip = new Button(() => EquipItem(item));
            btnEquip.text = "EQUIP";
            btnEquip.AddToClassList("btn-buy");
            card.Add(btnEquip);
        }
        else
        {
            VisualElement priceBox = new VisualElement();
            priceBox.AddToClassList("item-price-box");

            Label price = new Label(item.Price.ToString());
            price.AddToClassList("item-price-value");
            priceBox.Add(price);
            card.Add(priceBox);

            Button btnBuy = new Button(() => BuyItem(item));
            btnBuy.text = "PURCHASE";
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
        foreach (var shopItem in _items)
        {
            shopItem.IsEquipped = false;
        }

        item.IsEquipped = true;
        Debug.Log($"Equipped {item.Name}!");
        RefreshUI();
    }
}
