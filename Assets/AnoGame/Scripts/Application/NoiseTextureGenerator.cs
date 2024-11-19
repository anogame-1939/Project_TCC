using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Application
{
    public class NoiseTextureGenerator : MonoBehaviour
    {
        [Header("Texture Settings")]
        public Material targetMaterial;
        public Texture3D savedNoiseTexture;
        public int resolution = 32;
        
        [Header("Noise Settings")]
        [Range(1f, 10f)]
        public float noiseScale = 4f;
        [Range(1, 8)]
        public int octaves = 4;
        [Range(0f, 1f)]
        public float persistence = 0.5f;
        
        [Header("Randomization")]
        public bool randomizeSeed = true;
        public Vector3 seedOffset;

        private void Start()
        {
            if (savedNoiseTexture != null && targetMaterial != null)
            {
                targetMaterial.SetTexture("_NoiseTex", savedNoiseTexture);
            }
        }
    }
}