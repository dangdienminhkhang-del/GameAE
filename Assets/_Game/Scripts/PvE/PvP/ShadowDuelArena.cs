using System.Collections;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.PvP;
using Game.PvE.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PvE.PvP
{
    /// <summary>
    /// Khu vực Võ Đài Tỉ Thí (Duel Arena theo SAO).
    /// Cho phép người chơi thách đấu với AI Bóng Ma Đối Thủ trong 3 chế độ:
    /// First-Strike, Half-Loss, Total-Loss.
    /// Hoàn toàn không kích hoạt Permadeath nếu thua trong Total-Loss.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ShadowDuelArena : MonoBehaviour
    {
        private bool _playerInRange;
        private DuelState _state = DuelState.None;
        private DuelMode _currentMode = DuelMode.HalfLoss;

        private PlayerController _player;
        private HealthComponent _playerHealth;
        private double _playerInitialHp;
        private int _playerHitsDealt;
        private int _shadowHitsDealt;

        private GameObject _shadowOpponentGo;
        private ShadowOpponentController _shadowController;

        private void Awake()
        {
            if (TryGetComponent(out Collider2D col))
            {
                col.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnDuelStartRequested += StartDuel;
            GameEvents.OnHitResolved += HandleHitResolved;
        }

        private void OnDisable()
        {
            GameEvents.OnDuelStartRequested -= StartDuel;
            GameEvents.OnHitResolved -= HandleHitResolved;
            EndDuel(-1, forced: true);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInRange = true;
                if (_state == DuelState.None)
                {
                    GameEvents.RaiseCombatNotice("[E] Thách đấu Bóng Ma Kiếm Sĩ (Duel SAO)", Color.cyan);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInRange = false;
                if (_state == DuelState.InProgress)
                {
                    EndDuel(2, forced: false); // Rời võ đài = xử thua
                    GameEvents.RaiseCombatNotice("Bạn đã rời võ đài - Xử thua cuộc!", Color.red);
                }
            }
        }

        private void Update()
        {
            if (_playerInRange && _state == DuelState.None)
            {
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.eKey.wasPressedThisFrame)
                {
                    GameEvents.RaiseDuelRequested();
                }
            }

            if (_state == DuelState.InProgress)
            {
                CheckDuelConditions();
            }
        }

        public void StartDuel(DuelMode mode)
        {
            if (_state != DuelState.None) return;

            _currentMode = mode;
            _player = FindAnyObjectByType<PlayerController>();
            if (_player == null) return;

            _playerHealth = _player.GetComponent<HealthComponent>();
            if (_playerHealth != null)
            {
                _playerInitialHp = _playerHealth.CurrentHealth;
            }

            _playerHitsDealt = 0;
            _shadowHitsDealt = 0;

            StartCoroutine(DuelCountdownRoutine());
        }

        private IEnumerator DuelCountdownRoutine()
        {
            _state = DuelState.Countdown;
            string modeName = DuelRules.GetModeName(_currentMode);
            GameEvents.RaiseCombatNotice($"Chuẩn bị tỉ thí: {modeName}!", Color.yellow);

            // Spawn Bóng Ma Đối Thủ
            SpawnShadowOpponent();

            yield return new WaitForSeconds(1.0f);
            GameEvents.RaiseCombatNotice("3...", Color.yellow);
            yield return new WaitForSeconds(1.0f);
            GameEvents.RaiseCombatNotice("2...", Color.yellow);
            yield return new WaitForSeconds(1.0f);
            GameEvents.RaiseCombatNotice("1...", Color.yellow);
            yield return new WaitForSeconds(0.5f);
            GameEvents.RaiseCombatNotice("⚔️ BẮT ĐẦU TỈ THÍ (DUEL START)! ⚔️", Color.green);

            _state = DuelState.InProgress;
            if (_shadowController != null)
            {
                _shadowController.EnableCombat(true);
            }
        }

        private void SpawnShadowOpponent()
        {
            if (_shadowOpponentGo != null) Destroy(_shadowOpponentGo);

            Vector3 spawnPos = transform.position + new Vector3(2.5f, 0f, 0f);
            _shadowOpponentGo = new GameObject("Shadow_Opponent");
            _shadowOpponentGo.transform.position = spawnPos;

            var col = _shadowOpponentGo.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            var sr = _shadowOpponentGo.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.4f, 0.4f, 0.6f, 0.9f); // Bóng ma màu xám tím
            sr.sortingOrder = 3;

            _shadowController = _shadowOpponentGo.AddComponent<ShadowOpponentController>();
            _shadowController.Init(this, _player != null ? _player.transform : null);
        }

        private void HandleHitResolved(object sender, HitResolvedArgs args)
        {
            if (_state != DuelState.InProgress || args == null) return;

            if (ReferenceEquals(args.Target, _shadowController))
            {
                _playerHitsDealt++;
            }
            else if (ReferenceEquals(args.Target, _playerHealth))
            {
                _shadowHitsDealt++;
            }

            CheckDuelConditions();
        }

        private void CheckDuelConditions()
        {
            if (_playerHealth == null || _shadowController == null) return;

            double p1Hp = _playerHealth.CurrentHealth;
            double p1MaxHp = _playerHealth.MaxHealth;
            double p2Hp = _shadowController.CurrentHealth;
            double p2MaxHp = _shadowController.MaxHealth;

            bool ended = DuelRules.CheckDuelEnd(
                _currentMode,
                p1Hp, p1MaxHp,
                p2Hp, p2MaxHp,
                _playerHitsDealt, _shadowHitsDealt,
                out int winnerIndex);

            if (ended)
            {
                EndDuel(winnerIndex, forced: false);
            }
        }

        private void EndDuel(int winnerIndex, bool forced)
        {
            if (_state == DuelState.None) return;

            _state = DuelState.None;

            if (_shadowController != null)
            {
                _shadowController.EnableCombat(false);
            }

            if (_shadowOpponentGo != null)
            {
                Destroy(_shadowOpponentGo, 0.5f);
                _shadowOpponentGo = null;
            }

            if (forced) return;

            // BẢO VỆ SINH TỬ SAO: Không bao giờ kích hoạt permadeath trong Duel!
            if (_playerHealth != null && _playerHealth.CurrentHealth <= 1.0)
            {
                _playerHealth.Heal(Mathf.Max(10f, (float)_playerInitialHp * 0.3f));
            }

            if (winnerIndex == 1)
            {
                GameEvents.RaiseCombatNotice("🏆 CHIẾN THẮNG! Bạn đã giành thắng lợi trong trận Duel!", Color.green);
            }
            else if (winnerIndex == 2)
            {
                GameEvents.RaiseCombatNotice("💀 THẤT BẠI! Bạn đã thua trong trận Duel. (Hệ thống bảo hộ can thiệp)", new Color(1f, 0.4f, 0.4f));
            }
            else if (winnerIndex == 0)
            {
                GameEvents.RaiseCombatNotice("🤝 HOÀ NHAU! Trận đấu kết thúc bất phân thắng bại.", Color.yellow);
            }
        }
    }

    /// <summary>
    /// Điều khiển đối thủ Bóng Ma trong trận Duel.
    /// </summary>
    public class ShadowOpponentController : MonoBehaviour, IDamageable
    {
        private ShadowDuelArena _arena;
        private Transform _target;
        private double _maxHealth = 200.0;
        private double _currentHealth = 200.0;
        private bool _canFight;
        private float _attackCooldown;

        public double CurrentHealth => _currentHealth;
        public double MaxHealth => _maxHealth;
        public bool IsDead => _currentHealth <= 0.0;

        public void Init(ShadowDuelArena arena, Transform target)
        {
            _arena = arena;
            _target = target;
            _currentHealth = _maxHealth;
        }

        public void EnableCombat(bool enable)
        {
            _canFight = enable;
        }

        private void Update()
        {
            if (!_canFight || _target == null) return;

            if (_attackCooldown > 0f) _attackCooldown -= Time.deltaTime;

            float dist = Vector2.Distance(transform.position, _target.position);
            if (dist > 1.2f)
            {
                transform.position = Vector2.MoveTowards(transform.position, _target.position, 2.8f * Time.deltaTime);
            }
            else if (_attackCooldown <= 0f)
            {
                _attackCooldown = 1.2f;
                // Đánh người chơi
                var health = _target.GetComponent<HealthComponent>();
                if (health != null)
                {
                    health.TakeDamage(new DamageInfo(12.0) { SourceId = "shadow_opponent" });
                    GameEvents.RaiseCombatNotice("Bóng Ma Kiếm Sĩ phản kích! (-12 HP)", new Color(1f, 0.6f, 0.6f));
                }
            }
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (damage == null) return;
            _currentHealth -= damage.Amount;
            if (_currentHealth < 0.0) _currentHealth = 0.0;
        }

        public void Heal(double amount)
        {
            _currentHealth = System.Math.Min(_maxHealth, _currentHealth + amount);
        }

        public void SetMaxHealth(double maxHp, bool resetCurrent = false)
        {
            _maxHealth = maxHp;
            if (resetCurrent) _currentHealth = _maxHealth;
        }
    }
}
