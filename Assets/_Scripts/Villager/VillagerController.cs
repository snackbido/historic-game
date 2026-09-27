using UnityEngine;

namespace PrehistoricTribe
{
    public enum VillagerJob
    {
        None,
        Gathering,
        Guarding
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class VillagerController : MonoBehaviour
    {
        [SerializeField] private VillagerData data;
        [SerializeField] private SpriteRenderer selectionRing;

        private Rigidbody2D body;

        private float hungerTimer;
        private float sleepTimer;
        private float warmthTimer;
        private bool warnedSleep;
        private bool warnedWarmth;

        private VillagerJob job = VillagerJob.None;
        private ResourceNode gatheringTarget;
        private Vector2 guardPosition;
        private float gatherCooldown;

        private bool hasMoveTarget;
        private Vector2 moveTarget;

        public VillagerData Data => data;
        public float Hunger { get; private set; } = 100f;
        public float Sleep { get; private set; } = 100f;
        public float Warmth { get; private set; } = 100f;
        public VillagerJob CurrentJob => job;
        public bool IsSelected { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            UpdateNeeds();
            UpdateJob();
        }

        private void FixedUpdate()
        {
            if (!hasMoveTarget) return;

            Vector2 direction = moveTarget - body.position;
            if (direction.magnitude <= 0.05f) return;

            body.MovePosition(body.position + direction.normalized * data.moveSpeed * Time.fixedDeltaTime);
        }

        public void AssignGathering(ResourceNode target)
        {
            job = VillagerJob.Gathering;
            gatheringTarget = target;
        }

        public void AssignGuarding(Vector2 position)
        {
            job = VillagerJob.Guarding;
            guardPosition = position;
            gatheringTarget = null;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (selectionRing != null) selectionRing.enabled = selected;
        }

        public void LoadState(VillagerData source, float hunger, float sleep, float warmth)
        {
            data = source;
            Hunger = hunger;
            Sleep = sleep;
            Warmth = warmth;
            hungerTimer = 0f;
            sleepTimer = 0f;
            warmthTimer = 0f;
            warnedSleep = sleep <= data.lowNeedWarningThreshold;
            warnedWarmth = warmth <= data.lowNeedWarningThreshold;
            job = VillagerJob.None;
            gatheringTarget = null;
            hasMoveTarget = false;
        }

        private void UpdateNeeds()
        {
            hungerTimer += Time.deltaTime;
            if (hungerTimer >= data.hungerDecayInterval)
            {
                hungerTimer = 0f;
                Hunger = Mathf.Max(0f, Hunger - 1f);
                if (Hunger <= data.lowNeedWarningThreshold)
                    TryEat();
            }

            sleepTimer += Time.deltaTime;
            if (sleepTimer >= data.sleepDecayInterval)
            {
                sleepTimer = 0f;
                Sleep = Mathf.Max(0f, Sleep - 1f);
                if (Sleep <= data.lowNeedWarningThreshold && !warnedSleep)
                {
                    warnedSleep = true;
                    EventBus.RaiseNotification($"{data.displayName} đang buồn ngủ");
                }
            }

            warmthTimer += Time.deltaTime;
            if (warmthTimer >= data.warmthDecayInterval)
            {
                warmthTimer = 0f;
                Warmth = Mathf.Max(0f, Warmth - 1f);
                if (Warmth <= data.lowNeedWarningThreshold && !warnedWarmth)
                {
                    warnedWarmth = true;
                    EventBus.RaiseNotification($"{data.displayName} đang bị lạnh");
                }
            }
        }

        private void TryEat()
        {
            if (ResourceManager.Instance.TrySpend(data.foodResource, data.foodPerMeal))
                Hunger = 100f;
            else
                EventBus.RaiseNotification($"{data.displayName} đói nhưng không đủ {data.foodResource.displayName}");
        }

        private void UpdateJob()
        {
            switch (job)
            {
                case VillagerJob.Gathering:
                    UpdateGathering();
                    break;

                case VillagerJob.Guarding:
                    hasMoveTarget = Vector2.Distance(body.position, guardPosition) > 0.15f;
                    moveTarget = guardPosition;
                    break;

                default:
                    hasMoveTarget = false;
                    break;
            }
        }

        private void UpdateGathering()
        {
            if (gatheringTarget == null || gatheringTarget.IsDepleted)
            {
                hasMoveTarget = false;
                return;
            }

            Vector2 targetPos = gatheringTarget.transform.position;
            float distance = Vector2.Distance(body.position, targetPos);

            if (distance > 0.6f)
            {
                hasMoveTarget = true;
                moveTarget = targetPos;
                return;
            }

            hasMoveTarget = false;
            gatherCooldown -= Time.deltaTime;
            if (gatherCooldown <= 0f)
            {
                gatherCooldown = 1f;
                gatheringTarget.Harvest();
            }
        }
    }
}
