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

    public System.Action OnShopClosed;

    private class ShopItem
    {
        public string ID;
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

        InitializeItems();
        RefreshUI();
    }

    private void InitializeItems()
    {
        _items.Clear();
        // ID must be unique
        _items.Add(new ShopItem { ID = "default", Name = "Default Core", Price = 0, Color = new Color(0, 0.95f, 1) });
        _items.Add(new ShopItem { ID = "crimson", Name = "Crimson Core", Price = 500, Color = new Color(1, 0.2f, 0) });
        _items.Add(new ShopItem { ID = "solar", Name = "Solar Core", Price = 1000, Color = new Color(1, 0.9f, 0) });
        _items.Add(new ShopItem { ID = "frost", Name = "Frost Core", Price = 1500, Color = new Color(0, 0.5f, 1) });
        _items.Add(new ShopItem { ID = "pink", Name = "Neon Pink", Price = 2000, Color = new Color(1, 0, 0.5f) });
        _items.Add(new ShopItem { ID = "violet", Name = "Violet Core", Price = 3000, Color = new Color(0.5f, 0, 1) });

        string equippedID = PlayerPrefs.GetString("EquippedSkin", "default");

        foreach (var item in _items)
        {
            if (item.Price == 0) item.IsPurchased = true;
            else item.IsPurchased = PlayerPrefs.GetInt("Purchased_" + item.ID, 0) == 1;

            item.IsEquipped = (item.ID == equippedID);
        }
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
                OnShopClosed?.Invoke();
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
            
            PlayerPrefs.SetInt("Purchased_" + item.ID, 1);
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
        
        PlayerPrefs.SetString("EquippedSkin", item.ID);
        PlayerPrefs.SetString("EquippedSkinColor", "#" + ColorUtility.ToHtmlStringRGB(item.Color));
        PlayerPrefs.Save();

        Debug.Log($"Equipped {item.Name}!");
        RefreshUI();
    }
}
