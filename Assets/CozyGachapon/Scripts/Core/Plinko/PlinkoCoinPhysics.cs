using UnityEngine;

namespace CozyGachapon.Plinko
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class PlinkoCoinPhysics : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private CircleCollider2D _collider;
        [SerializeField] private float _maxCoinSpeed = 6f;

        public Rigidbody2D Rigidbody => _rigidbody;
        public CircleCollider2D Collider => _collider;
        public float MaxCoinSpeed => _maxCoinSpeed;
        public Vector2 LinearVelocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector2.zero;

        private void Awake()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }

            if (_collider == null)
            {
                _collider = GetComponent<CircleCollider2D>();
            }
        }

        public void Initialize(PlinkoConfig config)
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }

            if (_collider == null)
            {
                _collider = GetComponent<CircleCollider2D>();
            }

            _maxCoinSpeed = config.MaxCoinSpeed;
            if (_rigidbody != null)
            {
                _rigidbody.gravityScale = config.CoinGravityScale;
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
            }

            if (_collider != null)
            {
                _collider.radius = config.CoinRadius;
                if (_collider.sharedMaterial == null)
                {
                    _collider.sharedMaterial = EffectiveBouncinessCalculator.GetOrCreateCoinMaterial();
                }
            }

            int coinLayer = LayerMask.NameToLayer(PlinkoPhysicsConstants.CoinLayerName);
            if (coinLayer >= 0)
            {
                gameObject.layer = coinLayer;
            }
        }

        public void ClampVelocity()
        {
            if (_rigidbody == null)
            {
                return;
            }

            Vector2 velocity = _rigidbody.linearVelocity;
            float sqrSpeed = velocity.sqrMagnitude;
            float maxSpeedSq = _maxCoinSpeed * _maxCoinSpeed;

            if (sqrSpeed > maxSpeedSq)
            {
                float speed = Mathf.Sqrt(sqrSpeed);
                _rigidbody.linearVelocity = velocity * (_maxCoinSpeed / speed);
            }
        }

        private void FixedUpdate()
        {
            ClampVelocity();
        }
    }
}
