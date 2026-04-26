using UnityEngine;
using UnityEngine.UI;
using IdleGame.Data;
using IdleGame.Managers;
using IdleGame.Utils;

namespace IdleGame.UI.Panels
{
    public class EquipmentPanelUI : MonoBehaviour
    {
        private Transform _listContent;
        private bool _built = false;

        private void Start()
        {
            UIHelper.MakeText(transform, "장비", 36, TextAnchor.UpperCenter,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(0, -70), offsetMax: Vector2.zero);

            GameObject scrollObj = UIHelper.MakeScrollView(transform, out _listContent);
            RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(10, 10);
            scrollRt.offsetMax = new Vector2(-10, -75);

            _built = true;
            Refresh();
            InventoryManager.Instance.OnInventoryChanged += Refresh;
            InventoryManager.Instance.OnEquipChanged += Refresh;
        }

        private void OnEnable()
        {
            if (_built) Refresh();
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance == null) return;
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
            InventoryManager.Instance.OnEquipChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_listContent == null || InventoryManager.Instance == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            var items = InventoryManager.Instance.ShopItems;
            if (items == null) return;

            bool hasEquip = false;
            foreach (var item in items)
            {
                if (item == null || item.isStackable || !InventoryManager.Instance.IsOwned(item)) continue;
                hasEquip = true;
                CreateEquipRow(item);
            }

            if (!hasEquip)
            {
                UIHelper.MakeText(_listContent, "보유 장비 없음", 24, TextAnchor.MiddleCenter,
                    anchorMin: new Vector2(0, 0.4f), anchorMax: new Vector2(1, 0.6f),
                    offsetMin: Vector2.zero, offsetMax: Vector2.zero,
                    color: new Color(0.5f, 0.5f, 0.5f));
            }
        }

        private void CreateEquipRow(ItemData item)
        {
            bool equipped = InventoryManager.Instance.IsEquipped(item);

            GameObject row = new GameObject(item.name + "_Row");
            row.transform.SetParent(_listContent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 125);

            Image bg = row.AddComponent<Image>();
            bg.color = equipped
                ? new Color(0.1f, 0.25f, 0.15f, 1f)
                : new Color(0.15f, 0.15f, 0.2f, 1f);

            // 장착 여부 표시
            string nameLabel = equipped ? $"[장착중] {item.itemName}" : item.itemName;
            Color nameColor = equipped ? new Color(0.4f, 1f, 0.5f) : Color.white;
            UIHelper.MakeText(row.transform, nameLabel, 28, TextAnchor.MiddleLeft,
                new Vector2(15, 14), new Vector2(-155, 14), nameColor);
            UIHelper.MakeText(row.transform, item.description, 22, TextAnchor.MiddleLeft,
                new Vector2(15, -20), new Vector2(-155, -20), new Color(0.7f, 0.85f, 0.7f));

            // 장착 / 장착해제 버튼
            string btnLabel = equipped ? "장착해제" : "장착";
            Color btnColor = equipped ? new Color(0.55f, 0.2f, 0.2f) : new Color(0.15f, 0.5f, 0.25f);
            GameObject equipBtn = UIHelper.MakeButton(row.transform, btnLabel, 24, btnColor);
            RectTransform btnRt = equipBtn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1, 0.5f);
            btnRt.anchorMax = new Vector2(1, 0.5f);
            btnRt.pivot = new Vector2(1, 0.5f);
            btnRt.anchoredPosition = new Vector2(-10, 0);
            btnRt.sizeDelta = new Vector2(140, 84);

            var btn = equipBtn.GetComponent<Button>();
            if (equipped)
                btn.onClick.AddListener(() => InventoryManager.Instance.Unequip(item));
            else
                btn.onClick.AddListener(() => InventoryManager.Instance.Equip(item));
        }
    }
}
