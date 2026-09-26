using System.Collections;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    public class Milestone3AnimalTests : PlayModeTestBase
    {
        [UnityTest]
        public IEnumerator WildBoar_TameThenCollectProduct_AddsResourceAndResetsProductReady()
        {
            var food = AssetDatabase.LoadAssetAtPath<ResourceTypeData>("Assets/_Data/ResourceType_Food.asset");
            var boar = GameObject.Find("WildBoar").GetComponent<AnimalController>();
            Assert.AreEqual(AnimalState.Wild, boar.State);

            ResourceManager.Instance.AddResource(food, 100);

            string lastNotification = null;
            void Handler(string message) => lastNotification = message;
            EventBus.OnNotification += Handler;

            bool fed1 = TamingSystem.Instance.TryInteract(boar);
            Assert.IsTrue(fed1, "First feeding should succeed with enough Food");
            Assert.AreEqual(AnimalState.Wild, boar.State, "Should still be Wild before feedingsToTame is reached");
            StringAssert.Contains("1/", lastNotification, "First feeding notification should show taming progress");

            bool fed2 = TamingSystem.Instance.TryInteract(boar);
            Assert.IsTrue(fed2);
            Assert.AreEqual(AnimalState.Tamed, boar.State, "Should become Tamed after feedingsToTame feedings");
            StringAssert.Contains("thuần hóa", lastNotification, "Final feeding notification should announce taming");

            float deadline = Time.time + boar.Data.productionInterval + 5f;
            while (!boar.ProductReady && Time.time < deadline)
                yield return null;
            Assert.IsTrue(boar.ProductReady, "Product should become ready after productionInterval");

            int foodBefore = ResourceManager.Instance.GetAmount(food);
            bool collected = TamingSystem.Instance.TryInteract(boar);
            Assert.IsTrue(collected, "Interacting with a Tamed+ProductReady animal should collect the product");
            int foodAfter = ResourceManager.Instance.GetAmount(food);
            Assert.Greater(foodAfter, foodBefore, "Collecting product should add resources");
            Assert.IsFalse(boar.ProductReady, "ProductReady should reset after collection");
            StringAssert.Contains("Thu hoạch", lastNotification, "Collecting a product should notify the player, not silently feed");

            EventBus.OnNotification -= Handler;
        }

        [UnityTest]
        public IEnumerator Feeding_WithoutEnoughResources_Fails()
        {
            var boar = GameObject.Find("WildBoar").GetComponent<AnimalController>();

            string lastNotification = null;
            void Handler(string message) => lastNotification = message;
            EventBus.OnNotification += Handler;

            bool fed = TamingSystem.Instance.TryInteract(boar);
            Assert.IsFalse(fed, "Feeding should fail without enough Food resource (fresh scene starts at 0)");
            Assert.AreEqual(0, boar.TamingProgress);
            Assert.AreEqual(AnimalState.Wild, boar.State);
            StringAssert.Contains("Không đủ", lastNotification, "Failed feeding should notify the player why");

            EventBus.OnNotification -= Handler;
            yield return null;
        }
    }
}
