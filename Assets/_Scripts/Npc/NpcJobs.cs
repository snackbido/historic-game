using System.Collections.Generic;
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

        /// <summary>Việc này đang "giữ" đối tượng này (để người khác không tranh làm).</summary>
        public virtual bool Claims(MonoBehaviour target) => Target == target;

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

                case RiceMortar mortar when profession.Can(NpcCapability.Farm):
                    return new MortarJob(mortar);

                case AnimalController animal when animal.State == AnimalState.Wild && profession.Can(NpcCapability.Hunt):
                    return new HuntJob(animal, profession);

                case AnimalController animal when profession.Can(NpcCapability.TendAnimals):
                    return new TendAnimalJob(animal);

                default:
                    return null;
            }
        }

        private const float GuardRadius = 10f;

        /// <summary>
        /// Việc tự tìm khi rảnh, theo <see cref="ProfessionData.autoWork"/>, trong bán kính quanh NPC.
        /// Không tranh việc người khác đang làm. Trả về null nếu không có gì để làm.
        /// </summary>
        public static NpcJob FindAutoJob(NpcController npc, float radius)
        {
            ProfessionData profession = npc.Profession;
            if (profession == null || profession.autoWork == NpcCapability.None) return null;
            Vector3 position = npc.transform.position;

            // Canh gác: sói đang đuổi/cắn ai đó gần đây thì lao vào.
            if ((profession.autoWork & NpcCapability.Hunt) != 0)
            {
                foreach (var predator in PredatorAI.All)
                {
                    if (predator.State == PredatorState.Patrol || predator.Health.IsDead) continue;
                    if (InteractableRegistry.GroundDistance(position, predator.transform.position) <= GuardRadius)
                        return new AttackJob(predator, profession);
                }
            }

            if ((profession.autoWork & NpcCapability.Farm) != 0)
            {
                FarmManager farm = FarmManager.Instance;
                FarmPlot plot = Nearest(InteractableRegistry.All<FarmPlot>(), position, radius, p =>
                    p.State == FarmPlotState.ReadyToHarvest || p.State == FarmPlotState.Withered ||
                    p.State == FarmPlotState.Wild || p.State == FarmPlotState.Unplowed || p.IsThirsty ||
                    p.NeedsWeeding || (p.CanFertilize && farm != null && farm.HasFertilizer) ||
                    (p.State == FarmPlotState.Empty && farm != null && FarmPlot.HasSeedFor(farm.CropFor(p))));
                if (plot != null) return new FarmJob(plot, continuous: false);

                // Ruộng ổn rồi thì ra cối: phơi, tuốt lúa, giã gạo.
                RiceMortar mortar = Nearest(InteractableRegistry.All<RiceMortar>(), position, radius, m => m.HasWork);
                if (mortar != null) return new MortarJob(mortar);
            }

            if ((profession.autoWork & NpcCapability.TendAnimals) != 0)
            {
                AnimalController animal = Nearest(InteractableRegistry.All<AnimalController>(), position, radius,
                    a => a.State == AnimalState.Tamed && a.ProductReady);
                if (animal != null) return new TendAnimalJob(animal);
            }

            if ((profession.autoWork & NpcCapability.Gather) != 0)
            {
                ResourceNode node = Nearest(InteractableRegistry.All<ResourceNode>(), position, radius, n => !n.IsDepleted);
                if (node != null) return new GatherJob(node);
            }

            return null;
        }

        private static T Nearest<T>(IEnumerable<T> candidates, Vector3 position, float radius, System.Func<T, bool> wanted)
            where T : MonoBehaviour
        {
            T best = null;
            float bestDistance = radius;
            foreach (var candidate in candidates)
            {
                if (!wanted(candidate) || IsClaimed(candidate)) continue;
                float distance = InteractableRegistry.GroundDistance(position, candidate.transform.position);
                if (distance > bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        /// <summary>Đã có NPC khác nhận việc tại đối tượng này chưa.</summary>
        private static bool IsClaimed(MonoBehaviour target)
        {
            foreach (var npc in NpcController.All)
                if (npc.CurrentJob != null && npc.CurrentJob.Claims(target)) return true;
            return false;
        }

        /// <summary>Biết chiến đấu: có Fight (dân làng, trinh sát) hoặc Hunt (thợ săn).</summary>
        public static bool CanFight(ProfessionData profession) =>
            profession != null && (profession.Can(NpcCapability.Fight) || profession.Can(NpcCapability.Hunt));
    }

    /// <summary>
    /// Đi ngủ ban đêm (Milestone 5c): người có lều về lều và vào trong (ẩn khỏi bản đồ, thú dữ không với tới);
    /// người chưa có nhà nằm ngủ quanh đống lửa. Ngủ thì hồi máu. Trời sáng thì việc kết thúc → thức dậy.
    /// </summary>
    public class SleepJob : NpcJob
    {
        private const float HealPerSecond = 1.5f;

        private readonly MonoBehaviour place;
        private readonly bool insideHut;

        public SleepJob(BuildingInstance hut)
        {
            place = hut;
            insideHut = true;
        }

        public SleepJob(Campfire fire)
        {
            place = fire;
            insideHut = false;
        }

        public bool InsideHut => insideHut;
        public override MonoBehaviour Target => place;
        public override string Description => "đi ngủ";
        public override float WorkRange => insideHut ? 1.2f : 2.2f; // quanh đống lửa thì nằm cách lửa một khoảng
        public override float Interval => 1f;

        public override bool DoWork(NpcController npc)
        {
            if (DayNightCycle.Instance == null || !DayNightCycle.Instance.IsNight) return false; // sáng rồi → dậy

            if (!npc.IsSleeping) npc.FallAsleep(insideHut);
            npc.Health.Heal(HealPerSecond * Interval);
            return true;
        }
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

    /// <summary>Khai thác (chặt cây, đánh cá…) đến khi nguồn cạn.</summary>
    public class GatherJob : NpcJob
    {
        private readonly ResourceNode node;

        public GatherJob(ResourceNode node) => this.node = node;

        public override MonoBehaviour Target => node;
        public override string Description => node.ActionName.ToLowerInvariant();
        public override float WorkRange => node.WorkRange;
        public override float Interval => 3f; // 1 đơn vị / 3s → một cây 10 gỗ mất ~30s

        public override bool DoWork(NpcController npc)
        {
            if (node.IsDepleted) return false; // ao cạn: thôi, chờ cá sinh sôi lại
            node.Harvest(); // cây hết gỗ thì tự Destroy → IsValid = false ở lượt sau
            return !node.IsDepleted;
        }
    }

    /// <summary>
    /// Làm ruộng: gieo hạt đang chọn → chờ lớn → thu hoạch → gieo lại; dọn cây héo.
    /// Lệnh của người chơi làm liên tục; tự làm khi rảnh (continuous = false) chỉ làm 1 thao tác rồi thôi.
    /// </summary>
    public class FarmJob : NpcJob
    {
        private readonly FarmPlot plot;
        private readonly bool continuous;

        // Gánh nước (M5d/F2): đi tới nguồn nước múc → mang về đổ vào ruộng.
        private WaterSource source;
        private bool fetching;
        private bool carrying;
        private bool weeding;

        public FarmJob(FarmPlot plot, bool continuous = true)
        {
            this.plot = plot;
            this.continuous = continuous;
        }

        public FarmPlot Plot => plot;
        public bool IsCarryingWater => carrying;
        public override MonoBehaviour Target => fetching ? source : plot;
        public override bool Claims(MonoBehaviour target) => target == plot;
        public override bool IsValid => plot != null && plot.isActiveAndEnabled && (!fetching || source != null);

        public override string Description =>
            fetching || carrying ? "gánh nước" :
            plot.State == FarmPlotState.Wild ? $"khai hoang {plot.FieldName}" :
            plot.State == FarmPlotState.Unplowed ? "cày/xới đất" :
            weeding || plot.NeedsWeeding ? "làm cỏ" : "làm ruộng";

        public override float WorkRange => fetching ? source.DrawRange : 0.7f;
        public override float Interval => 1f;

        public override bool DoWork(NpcController npc)
        {
            if (fetching)
            {
                fetching = false; // múc đầy gàu → quay về ruộng
                carrying = true;
                return true;
            }

            if (carrying)
            {
                carrying = false;
                plot.AddWater(plot.WaterPerTrip);
                // Tự làm: gánh đến khi đủ nước rồi thôi; lệnh: làm tiếp các bước khác.
                return continuous || plot.IsThirsty;
            }

            switch (plot.State)
            {
                case FarmPlotState.Wild:
                    // Khai hoang/đắp bờ: làm đến khi xong rồi mới thôi (kể cả khi tự làm).
                    return !plot.DoClearWork() || continuous;

                case FarmPlotState.Unplowed:
                    return !plot.DoPlowWork() || continuous;

                case FarmPlotState.ReadyToHarvest:
                    string verb = plot.Crop != null ? plot.Crop.harvestVerb.ToLowerInvariant() : "thu hoạch";
                    string what = plot.Crop != null ? plot.Crop.displayName.ToLowerInvariant() : "ruộng";
                    FarmManager.Instance.TryInteract(plot);
                    EventBus.RaiseNotification($"{npc.NpcName} {verb} {what}");
                    return continuous;

                case FarmPlotState.Withered:
                    plot.ClearWithered();
                    return continuous;
            }

            if (plot.IsThirsty) return StartFetchingWater(npc);

            // Chăm sóc (F4): làm cỏ đến khi sạch; có phân trong kho thì bón (mỗi vụ một lần).
            if (plot.NeedsWeeding || (weeding && plot.Weeds > 0f))
            {
                weeding = !plot.DoWeedWork();
                return weeding || continuous;
            }
            weeding = false;
            if (plot.CanFertilize && FarmManager.Instance.TryFertilize(plot))
            {
                EventBus.RaiseNotification($"{npc.NpcName} bón phân cho {plot.Crop.displayName.ToLowerInvariant()}");
                return continuous;
            }

            switch (plot.State)
            {
                case FarmPlotState.Growing:
                    return continuous; // lệnh: đứng chờ cây lớn; tự làm: đi tìm việc khác

                default: // Empty
                    if (FarmManager.Instance.TryInteract(plot)) return continuous;
                    if (continuous)
                    {
                        CropData seed = FarmManager.Instance.CropFor(plot);
                        EventBus.RaiseNotification(seed == null
                            ? $"{npc.NpcName}: chưa chọn hạt giống để gieo"
                            : $"{npc.NpcName}: {plot.PlantBlocker(seed)}");
                    }
                    return false;
            }
        }

        private bool StartFetchingWater(NpcController npc)
        {
            source = WaterSource.Nearest(plot.transform.position);
            if (source == null)
            {
                if (continuous) EventBus.RaiseNotification($"{npc.NpcName}: không có nguồn nước để gánh");
                return false;
            }
            fetching = true;
            return true;
        }
    }

    /// <summary>Làm ở cối giã (F5): phơi bó lúa, tuốt lấy thóc, giã gạo — làm tới khi hết việc (phơi thì chờ nắng).</summary>
    public class MortarJob : NpcJob
    {
        private readonly RiceMortar mortar;
        private string lastTask;

        public MortarJob(RiceMortar mortar) => this.mortar = mortar;

        public override MonoBehaviour Target => mortar;
        public override string Description => (lastTask ?? mortar.NextTask ?? "giã gạo").ToLowerInvariant();
        public override float WorkRange => 1.2f;

        public override bool DoWork(NpcController npc)
        {
            lastTask = mortar.NextTask;
            if (!mortar.DoWork()) return false;
            return mortar.HasWork;
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
