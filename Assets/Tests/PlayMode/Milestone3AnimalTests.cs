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

            bool fed1 = TamingSystem.Instance.TryInteract(boar);
            Assert.IsTrue(fed1, "First feeding should succeed with enough Food");
            Assert.AreEqual(AnimalState.Wild, boar.State, "Should still be Wild before feedingsToTame is reached");

            bool fed2 = TamingSystem.Instance.TryInteract(boar);
            Assert.IsTrue(fed2);
            Assert.AreEqual(AnimalState.Tamed, boar.State, "Should become Tamed after feedingsToTame feedings");

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
        }

        [UnityTest]
        public IEnumerator Feeding_WithoutEnoughResources_Fails()
        {
            var boar = GameObject.Find("WildBoar").GetComponent<AnimalController>();

            bool fed = TamingSystem.Instance.TryInteract(boar);
            Assert.IsFalse(fed, "Feeding should fail without enough Food resource (fresh scene starts at 0)");
            Assert.AreEqual(0, boar.TamingProgress);
            Assert.AreEqual(AnimalState.Wild, boar.State);
            yield return null;
        }
    }
}
