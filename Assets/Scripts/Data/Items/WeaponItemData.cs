using UnityEngine;

namespace SurvivalGame.Data.Items
{
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "SurvivalGame/Items/Weapon Item")]
    public class WeaponItemData : ItemData
    {
        [Header("Weapon Properties")]
        [SerializeField] private WeaponType _weaponType = WeaponType.Melee;
        [SerializeField] private float _baseDamage = 10f;
        [SerializeField] private float _attackSpeed = 1f;
        [SerializeField] private float _range = 2f;
        [SerializeField] private float _durability = 100f;
        [SerializeField] private float _maxDurability = 100f;

        [Header("Ranged Weapon")]
        [SerializeField] private AmmoType _ammoType = AmmoType.None;
        [SerializeField] private float _projectileSpeed = 20f;

        [Header("Effects")]
        [SerializeField] private GameObject _hitEffect;
        [SerializeField] private AudioClip _attackSound;
        [SerializeField] private StatusEffectData[] _onHitEffects;

        #region Properties

        public WeaponType WeaponType => _weaponType;
        public float BaseDamage => _baseDamage;
        public float AttackSpeed => _attackSpeed;
        public float Range => _range;
        public float Durability => _durability;
        public float MaxDurability => _maxDurability;

        public AmmoType AmmoType => _ammoType;
        public float ProjectileSpeed => _projectileSpeed;

        public GameObject HitEffect => _hitEffect;
        public AudioClip AttackSound => _attackSound;
        public StatusEffectData[] OnHitEffects => _onHitEffects;

        #endregion

        public float GetAttackCooldown()
        {
            return 1f / _attackSpeed;
        }
    }

    public enum WeaponType
    {
        Melee,
        Ranged,
        Magic
    }

    public enum AmmoType
    {
        None,
        Arrow,
        Bolt,
        Bullet
    }
}
