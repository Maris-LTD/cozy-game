using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Economy
{
    [CreateAssetMenu(fileName = "CurrencyDefinition", menuName = "CozyGachapon/Economy/CurrencyDefinition")]
    public class CurrencyDefinition : ScriptableObject
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private Sprite _icon;
        [SerializeField] private ReleaseTier _releaseTier = ReleaseTier.Mvp;
        [SerializeField] private bool _resetsOnNewShop;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public ReleaseTier ReleaseTier => _releaseTier;
        public bool ResetsOnNewShop => _resetsOnNewShop;

        public void SetIdForTest(string id)
        {
            _id = id;
        }

        public void SetReleaseTierForTest(ReleaseTier releaseTier)
        {
            _releaseTier = releaseTier;
        }

        public void SetResetsOnNewShopForTest(bool resetsOnNewShop)
        {
            _resetsOnNewShop = resetsOnNewShop;
        }
    }
}
