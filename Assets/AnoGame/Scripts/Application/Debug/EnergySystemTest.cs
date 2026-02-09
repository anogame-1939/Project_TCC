using UnityEngine;
using AnoGame.Application.Managers;
using AnoGame.Application.Interfaces;
using System.Collections;

namespace AnoGame.Application.DebugTests
{
    public class EnergySystemTest : MonoBehaviour
    {
        public bool testOnStart = true;

        private IEnumerator Start()
        {
            if (!testOnStart) yield break;

            yield return new WaitForSeconds(1.0f);

            Debug.Log("=== Starting Energy System Test ===");

            if (EnergyManager.Instance == null)
            {
                Debug.LogError("EnergyManager Instance is null!");
                yield break;
            }

            // 1. Activate System
            Debug.Log("1. Activating System (Simulating Key Acquisition)");
            EnergyManager.Instance.IsSystemActive = true;
            yield return null;

            if (!EnergyManager.Instance.IsSystemActive)
            {
                Debug.LogError("Failed to activate system.");
                yield break;
            }

            // 2. Find Consumers
            var consumers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            IEnergyConsumer streetlight = null;
            IEnergyConsumer door = null;

            foreach (var c in consumers)
            {
                if (c is IEnergyConsumer ec)
                {
                    if (ec.DeviceName.Contains("Spotlight")) streetlight = ec;
                    if (ec.DeviceName.Contains("Door")) door = ec;
                }
            }

            if (streetlight == null) Debug.LogWarning("No Streetlight found for test.");
            if (door == null) Debug.LogWarning("No Electronic Door found for test.");

            // 3. Test Toggle
            if (streetlight != null)
            {
                Debug.Log($"2. Requesting Power for Streetlight: {streetlight.DeviceName}");
                EnergyManager.Instance.RequestPower(streetlight);
                yield return new WaitForSeconds(0.5f);
                // Verify? (Visuals checks are hard, but we can assume log output from Consumer is correct)
            }

            if (door != null)
            {
                Debug.Log($"3. Requesting Power for Door: {door.DeviceName}");
                EnergyManager.Instance.RequestPower(door);
                yield return new WaitForSeconds(0.5f);
            }

            if (streetlight != null)
            {
                Debug.Log($"4. Requesting Power for Streetlight again (Should close Door): {streetlight.DeviceName}");
                EnergyManager.Instance.RequestPower(streetlight);
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("=== Energy System Test Completed ===");
        }
    }
}
