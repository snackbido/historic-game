using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Cháy rừng (M6/D4): sét đánh cháy vài cây gần trại, lửa lan sang cây/nhà/ruộng xung quanh. Dân làng gánh nước
    /// dập lửa, người chơi đập lửa (E). Ruộng còn ướt không bắt lửa.
    /// Hết lửa thì đợt cháy kết thúc sớm; hết giờ thì mưa xuống dập nốt phần còn lại.
    /// </summary>
    public class WildfireDisaster : DisasterEvent
    {
        [SerializeField] private int lightningStrikes = 2;
        [Tooltip("Sét đánh vào cây trong bán kính này quanh đống lửa trại (để đám cháy liên quan tới làng)")]
        [SerializeField] private float strikeRadiusAroundCamp = 16f;
        [SerializeField] private Material flameMaterial;
        [SerializeField] private Material flameCoreMaterial;
        [SerializeField] private Material smokeMaterial;
        [SerializeField] private Material charredMaterial;

        private Fire.Settings settings;
        private bool started;

        public Fire.Settings FireSettings => settings ??= new Fire.Settings
        {
            flameMaterial = flameMaterial,
            coreMaterial = flameCoreMaterial,
            smokeMaterial = smokeMaterial,
            charredMaterial = charredMaterial
        };

        public override bool CanHappen() => Trees().Count > 0;

        public override bool IsFinishedEarly => started && Fire.All.Count == 0;

        private static List<ResourceNode> Trees()
        {
            var trees = new List<ResourceNode>();
            foreach (var node in InteractableRegistry.All<ResourceNode>())
                if (Fire.CanBurn(node)) trees.Add(node);
            return trees;
        }

        public override void OnBegin()
        {
            started = false;
            List<ResourceNode> trees = Trees();
            Vector3 camp = Campfire.All.Count > 0 ? Campfire.All[0].transform.position : Vector3.zero;
            var near = trees.FindAll(t => InteractableRegistry.GroundDistance(t.transform.position, camp) <= strikeRadiusAroundCamp);
            if (near.Count > 0) trees = near;

            for (int i = 0; i < lightningStrikes && trees.Count > 0; i++)
            {
                int pick = Random.Range(0, trees.Count);
                Strike(trees[pick]);
                trees.RemoveAt(pick);
            }
        }

        /// <summary>Sét đánh (hoặc lửa bén) vào đối tượng này.</summary>
        public Fire Strike(MonoBehaviour target)
        {
            Fire fire = Fire.Ignite(target, FireSettings);
            if (fire != null) VfxManager.Play(VfxKind.Lightning, target.transform.position + Vector3.up * 1.5f);
            if (fire != null) started = true;
            return fire;
        }

        public override void OnTick(float deltaTime, float progress) { }

        public override void OnEnd()
        {
            PutOutAll();
            started = false;
        }

        private static void PutOutAll()
        {
            foreach (var fire in new List<Fire>(Fire.All))
                if (fire != null) fire.PutOut();
        }
    }
}
