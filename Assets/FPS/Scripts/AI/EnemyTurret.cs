using Unity.FPS.Game;
using UnityEngine;
using System.Collections;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(EnemyController))]
    public class EnemyTurret : MonoBehaviour
    {
        public enum AIState
        {
            Idle,
            Attack,
            Summon,
        }

        // Variation 0 = Normal, 1 = Sommoner
        [Header("Variation")]
        [Tooltip("Summom Verion")]
        public bool SummomVerion = false;
        [Tooltip("can do the summon")]
        public bool canSummon = false;
        [Tooltip("Summon Creature")]
        public GameObject SummonPrefab;
        [Tooltip("Summon Amount")]
        public int summonAmount = 0;

        public Transform TurretPivot;
        public Transform TurretAimPoint;
        public Animator Animator;
        public float AimRotationSharpness = 5f;
        public float LookAtRotationSharpness = 2.5f;
        public float DetectionFireDelay = 1f;
        public float AimingTransitionBlendTime = 1f;

        [Tooltip("The random hit damage effects")]
        public ParticleSystem[] RandomHitSparks;

        public ParticleSystem[] OnDetectVfx;
        public AudioClip OnDetectSfx;

        public AIState AiState { get; private set; }

        EnemyController m_EnemyController;
        Health m_Health;
        Quaternion m_RotationWeaponForwardToPivot;
        float m_TimeStartedDetection;
        float m_TimeLostDetection;
        Quaternion m_PreviousPivotAimingRotation;
        Quaternion m_PivotAimingRotation;

        const string k_AnimOnDamagedParameter = "OnDamaged";
        const string k_AnimIsActiveParameter = "IsActive";

        void Start()
        {
            m_Health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, EnemyTurret>(m_Health, this, gameObject);
            m_Health.OnDamaged += OnDamaged;

            m_EnemyController = GetComponent<EnemyController>();
            DebugUtility.HandleErrorIfNullGetComponent<EnemyController, EnemyTurret>(m_EnemyController, this,
                gameObject);

            m_EnemyController.onDetectedTarget += OnDetectedTarget;
            m_EnemyController.onLostTarget += OnLostTarget;

            // Remember the rotation offset between the pivot's forward and the weapon's forward
            m_RotationWeaponForwardToPivot =
                Quaternion.Inverse(m_EnemyController.GetCurrentWeapon().WeaponMuzzle.rotation) * TurretPivot.rotation;

            // Start with idle
            AiState = AIState.Idle;

            m_TimeStartedDetection = Mathf.NegativeInfinity;
            m_PreviousPivotAimingRotation = TurretPivot.rotation;
        }

        void Update()
        {
            UpdateCurrentAiState();
        }

        void LateUpdate()
        {
            UpdateTurretAiming();
        }

        void UpdateCurrentAiState()
        {
            // Handle logic 
            switch (AiState)
            {
                case AIState.Attack:
                    if (!SummomVerion && canSummon)
                    {
                        bool mustShoot = Time.time > m_TimeStartedDetection + DetectionFireDelay;
                        //bool doSummon = ...
                        // Calculate the desired rotation of our turret (aim at target)
                        Vector3 directionToTarget =
                            (m_EnemyController.KnownDetectedTarget.transform.position - TurretAimPoint.position).normalized;
                        Quaternion offsettedTargetRotation =
                            Quaternion.LookRotation(directionToTarget) * m_RotationWeaponForwardToPivot;
                        m_PivotAimingRotation = Quaternion.Slerp(m_PreviousPivotAimingRotation, offsettedTargetRotation,
                            (mustShoot ? AimRotationSharpness : LookAtRotationSharpness) * Time.deltaTime);

                        // shoot
                        if (mustShoot) {

                            int short_lucky_number = Random.Range(1, 6);
                            // Debug.Log("ShortLuckyNumber: " + short_lucky_number);
                            if (360 / 4 * 3 > Vector3.Distance(m_EnemyController.KnownDetectedTarget.transform.position, TurretAimPoint.position) || short_lucky_number != 1)
                            {
                                Vector3 correctedDirectionToTarget =
                                    (m_PivotAimingRotation * Quaternion.Inverse(m_RotationWeaponForwardToPivot)) *
                                    Vector3.forward;

                                m_EnemyController.TryAtack(TurretAimPoint.position + correctedDirectionToTarget);
                            } else
                            {  // ratio = 0.2
                                for (int i = 0; i < Random.Range(1, 2); i++)
                                {
                                    Vector3 summonPos = TurretPivot.position + new Vector3(Random.Range(90, 301) / 100 * Mathf.Pow(-1, Random.Range(0, 2)), Random.Range(10, 101) / 100, Random.Range(90, 301) / 100) * Mathf.Pow(-1, Random.Range(0, 2));
                                    Instantiate(SummonPrefab, summonPos, Quaternion.identity);
                                    summonAmount -= 1;
                                }
                            }

                            int long_lucky_number = Random.Range(0, 20);
                            // Debug.Log("LongLuckyNumber: " + long_lucky_number);
                            // summon
                            if (500 > Vector3.Distance(m_EnemyController.KnownDetectedTarget.transform.position, TurretAimPoint.position) && long_lucky_number == 7)
                            {
                                if (summonAmount > 0 && SummonPrefab != null)
                                {
                                    for (int i = 0; i < 2; i++)
                                    {
                                        Vector3 summonPos = TurretPivot.position + new Vector3(Random.Range(90, 301) / 100 * Mathf.Pow(-1, Random.Range(0, 2)), Random.Range(10, 101) / 100, Random.Range(90, 301) / 100) * Mathf.Pow(-1, Random.Range(0, 2));
                                        Instantiate(SummonPrefab, summonPos, Quaternion.identity);
                                        summonAmount -= 1;
                                    }
                                }
                            }
                        }

                    }
                    else if (!SummomVerion)
                    {
                        bool mustShoot = Time.time > m_TimeStartedDetection + DetectionFireDelay;
                        //bool doSummon = ...
                        // Calculate the desired rotation of our turret (aim at target)
                        Vector3 directionToTarget =
                            (m_EnemyController.KnownDetectedTarget.transform.position - TurretAimPoint.position).normalized;
                        Quaternion offsettedTargetRotation =
                            Quaternion.LookRotation(directionToTarget) * m_RotationWeaponForwardToPivot;
                        m_PivotAimingRotation = Quaternion.Slerp(m_PreviousPivotAimingRotation, offsettedTargetRotation,
                            (mustShoot ? AimRotationSharpness : LookAtRotationSharpness) * Time.deltaTime);

                        // shoot
                        if (mustShoot)
                        {
                            Vector3 correctedDirectionToTarget =
                                (m_PivotAimingRotation * Quaternion.Inverse(m_RotationWeaponForwardToPivot)) *
                                Vector3.forward;

                            m_EnemyController.TryAtack(TurretAimPoint.position + correctedDirectionToTarget);
                        }
                    }
                    else if (SummomVerion)
                    {
                        if (summonAmount > 0 && SummonPrefab != null)
                        {
                            if (Random.Range(0, 5) != 0) {
                                for (int i = 0; i < Random.Range(1, 5); i++)
                                {
                                    // float step = random.Next(10, 101)/100; // [10, 101)
                                    Vector3 summonPos = TurretPivot.position + new Vector3(Random.Range(10, 101) / 100 * Mathf.Pow(-1, Random.Range(0, 2)), Random.Range(10, 101) / 100, Random.Range(10, 101) / 100) * Mathf.Pow(-1, Random.Range(0, 2));
                                    Instantiate(SummonPrefab, summonPos, Quaternion.identity);
                                    summonAmount -= 1;
                                }
                            } else
                            {
                                this.m_Health.Heal(50f);
                            }
                        }
                    }
                        break;
            }
        }

        void UpdateTurretAiming()
        {
            switch (AiState)
            {
                case AIState.Attack:
                    TurretPivot.rotation = m_PivotAimingRotation;
                    break;
                default:
                    // Use the turret rotation of the animation
                    TurretPivot.rotation = Quaternion.Slerp(m_PivotAimingRotation, TurretPivot.rotation,
                        (Time.time - m_TimeLostDetection) / AimingTransitionBlendTime);
                    break;
            }

            m_PreviousPivotAimingRotation = TurretPivot.rotation;
        }

        void OnDamaged(float dmg, GameObject source)
        {
            if (RandomHitSparks.Length > 0)
            {
                int n = Random.Range(0, RandomHitSparks.Length - 1);
                RandomHitSparks[n].Play();
            }

            Animator.SetTrigger(k_AnimOnDamagedParameter);
        }

        void OnDetectedTarget()
        {
            if (AiState == AIState.Idle)
            {
                AiState = AIState.Attack;
            }

            for (int i = 0; i < OnDetectVfx.Length; i++)
            {
                OnDetectVfx[i].Play();
            }

            if (OnDetectSfx)
            {
                AudioUtility.CreateSFX(OnDetectSfx, transform.position, AudioUtility.AudioGroups.EnemyDetection, 1f);
            }

            Animator.SetBool(k_AnimIsActiveParameter, true);
            m_TimeStartedDetection = Time.time;
        }

        void OnLostTarget()
        {
            if (AiState == AIState.Attack)
            {
                AiState = AIState.Idle;
            }

            for (int i = 0; i < OnDetectVfx.Length; i++)
            {
                OnDetectVfx[i].Stop();
            }

            Animator.SetBool(k_AnimIsActiveParameter, false);
            m_TimeLostDetection = Time.time;
        }
    }
}