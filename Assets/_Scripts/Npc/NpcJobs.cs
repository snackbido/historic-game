using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Một việc NPC làm tại một đối tượng: đi tới trong tầm <see cref="WorkRange"/>, rồi cứ
    /// <see cref="Interval"/> giây gọi <see cref="DoWork"/> một lần cho đến khi nó trả về false.
    /// </summary>
    public abstract class NpcJob
    {
        public abstract MonoBehaviour Target { get; }
        /// <summary>Mô tả ngắn cho thông báo, vd "chặt cây".</summary>
        public abstract string Description { get; }
        public virtual float WorkRange => 1.1f;
        public virtual float Interval => 1.5f;
        public virtual bool IsValid => Target != null && Target.isActiveAndEnabled;

        /// <summary>Làm một lượt. Trả về false khi việc đã xong (hoặc không làm tiếp được).</summary>
        public abstract bool DoWork(NpcController npc);
    }

    /// <summary>Chọn việc phù hợp với nghề cho đối tượng bị chuột phải; null = nghề này không làm được → đi theo.</summary>
    public static class NpcJobFactory
    {
        public static NpcJob Create(NpcController npc, MonoBehaviour target)
        {
            ProfessionData profession = npc.Profession;
            if (profession == null) return null;

            switch (target)
            {
                case PredatorAI predator when CanFight(profession):
                    return new AttackJob(predator, profession);

                case ResourceNode node when profession.Can(NpcCapability.Gather):
                    return new GatherJob(node);

                case FarmPlot plot when profession.Can(NpcCapability.Farm):
                    return new FarmJob(plot);

                case AnimalController animal when animal.State == AnimalState.Wild && profession.Can(NpcCapability.Hunt):
                    return new HuntJob(animal, profession);

                case AnimalController animal when profession.Can(NpcCapability.TendAnimals):
                    return new TendAnimalJob(animal);

                default:
                    return null;
            }
        }

        /// <summary>Biết chiến đấu: có Fight (dân làng, trinh sát) hoặc Hunt (thợ săn).</summary>
        public static bool CanFight(ProfessionData profession) =>
            profession != null && (profession.Can(NpcCapability.Fight) || profession.Can(NpcCapability.Hunt));
    }

    /// <summary>Đánh thú dữ trong tầm đánh của nghề cho đến khi nó chết.</summary>
    public class AttackJob : NpcJob
    {
        private readonly PredatorAI predator;
        private readonly ProfessionData profession;

        public AttackJob(PredatorAI predator, ProfessionData profession)
        {
            this.predator = predator;
            this.profession = profession;
        }

        public PredatorAI Predator => predator;
        public override MonoBehaviour Target => predator;
        public override string Description => $"đánh {predator.Data.displayName}";
        public override float WorkRange => Mathf.Max(1f, profession.attackRange);
        public override float Interval => 1.2f;
        public override bool IsValid => base.IsValid && !predator.Health.IsDead;

        public override bool DoWork(NpcController npc)
        {
            predator.Health.TakeDamage(profession.attackDamage, npc.gameObject);
            return !predator.Health.IsDead;
        }
    }

    /// <summary>Chặt cây đến khi hết tài nguyên.</summary>
    public class GatherJob : NpcJob
    {
        private readonly ResourceNode node;

        public GatherJob(ResourceNode node) => this.node = node;

        public override MonoBehaviour Target => node;
        public override string Description => "chặt cây";

        public override bool DoWork(NpcController npc)
        {
            node.Harvest(); // hết gỗ thì cây tự Destroy → IsValid = false ở lượt sau
            return true;
        }
    }

    /// <summary>Làm ruộng liên tục: gieo hạt đang chọn → chờ lớn → thu hoạch → gieo lại; dọn cây héo.</summary>
    public class FarmJob : NpcJob
    {
        private readonly FarmPlot plot;

        public FarmJob(FarmPlot plot) => this.plot = plot;

        public override MonoBehaviour Target => plot;
        public override string Description => "làm ruộng";
        public override float WorkRange => 0.7f;
        public override float Interval => 1f;

        public override bool DoWork(NpcController npc)
        {
            switch (plot.State)
            {
                case FarmPlotState.Growing:
                    return true; // đứng chờ cây lớn

                case FarmPlotState.ReadyToHarvest:
                    FarmManager.Instance.TryInteract(plot);
                    EventBus.RaiseNotification($"{npc.NpcName} thu hoạch ruộng");
                    return true;

                case FarmPlotState.Withered:
                    plot.ClearWithered();
                    return true;

                default: // Empty
                    if (FarmManager.Instance.TryInteract(plot)) return true;
                    EventBus.RaiseNotification($"{npc.NpcName}: chưa chọn hạt giống để gieo");
                    return false;
            }
        }
    }

    /// <summary>Săn thú hoang: đánh từ xa trong tầm đánh của nghề cho đến khi thú chết.</summary>
    public class HuntJob : NpcJob
    {
        private readonly AnimalController animal;
        private readonly HealthComponent health;
        private readonly ProfessionData profession;

        public HuntJob(AnimalController animal, ProfessionData profession)
        {
            this.animal = animal;
            this.profession = profession;
            health = animal.GetComponent<HealthComponent>();
        }

        public override MonoBehaviour Target => animal;
        public override string Description => $"săn {animal.Data.displayName}";
        public override float WorkRange => Mathf.Max(1f, profession.attackRange);
        public override float Interval => 1.2f;
        public override bool IsValid => base.IsValid && health != null && !health.IsDead;

        public override bool DoWork(NpcController npc)
        {
            string animalName = animal.Data.displayName;
            health.TakeDamage(profession.attackDamage, npc.gameObject);
            if (!health.IsDead) return true;

            EventBus.RaiseNotification($"{npc.NpcName} săn được {animalName}!");
            return false;
        }
    }

    /// <summary>Chăm vật nuôi qua TamingSystem: thú hoang thì cho ăn đến khi thuần, thú thuần thì cho ăn/thu sản phẩm 1 lần.</summary>
    public class TendAnimalJob : NpcJob
    {
        private readonly AnimalController animal;

        public TendAnimalJob(AnimalController animal) => this.animal = animal;

        public override MonoBehaviour Target => animal;
        public override string Description => animal.State == AnimalState.Wild
            ? $"thuần hóa {animal.Data.displayName}"
            : $"chăm {animal.Data.displayName}";

        public override bool DoWork(NpcController npc)
        {
            bool wasWild = animal.State == AnimalState.Wild;
            if (!TamingSystem.Instance.TryInteract(animal)) return false; // không đủ thức ăn → dừng

            // Thú hoang: tiếp tục cho ăn tới khi thuần. Thú thuần: làm 1 lần là xong.
            return wasWild && animal.State == AnimalState.Wild;
        }
    }
}
