// Editor/NoiseTextureGeneratorEditor.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using AnoGame.Application;

[CustomEditor(typeof(NoiseTextureGenerator))]
public class NoiseTextureGeneratorEditor : Editor
{
    private const string SavePathBase = "Assets/AnoGmae/NoiseTextures/GeneratedNoise";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        NoiseTextureGenerator generator = (NoiseTextureGenerator)target;
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Generate & Save Noise Texture"))
        {
            GenerateAndSaveNoiseTexture(generator);
        }
    }

    private void GenerateAndSaveNoiseTexture(NoiseTextureGenerator generator)
    {
        // テクスチャの生成
        Texture3D noiseTexture = new Texture3D(generator.resolution, generator.resolution, generator.resolution, TextureFormat.R8, false);
        noiseTexture.wrapMode = TextureWrapMode.Repeat;
        noiseTexture.filterMode = FilterMode.Bilinear;
        
        Color[] colors = new Color[generator.resolution * generator.resolution * generator.resolution];
        
        // ランダムシードの設定
        Vector3 offset = generator.seedOffset;
        if (generator.randomizeSeed)
        {
            offset += new Vector3(
                Random.Range(0f, 1000f),
                Random.Range(0f, 1000f),
                Random.Range(0f, 1000f)
            );
        }

        // 3Dノイズの生成
        float maxNoiseVal = 0f;
        float minNoiseVal = float.MaxValue;
        
        for (int z = 0; z < generator.resolution; z++)
        {
            for (int y = 0; y < generator.resolution; y++)
            {
                for (int x = 0; x < generator.resolution; x++)
                {
                    float noise = 0f;
                    float frequency = 1f;
                    float amplitude = 1f;
                    float maxValue = 0f;
                    
                    for (int o = 0; o < generator.octaves; o++)
                    {
                        float sampleX = (x + offset.x) * generator.noiseScale * frequency / generator.resolution;
                        float sampleY = (y + offset.y) * generator.noiseScale * frequency / generator.resolution;
                        float sampleZ = (z + offset.z) * generator.noiseScale * frequency / generator.resolution;
                        
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) *
                                          Mathf.PerlinNoise(sampleY, sampleZ) *
                                          Mathf.PerlinNoise(sampleZ, sampleX);
                        
                        noise += perlinValue * amplitude;
                        maxValue += amplitude;
                        amplitude *= generator.persistence;
                        frequency *= 2f;
                    }
                    
                    noise /= maxValue;
                    
                    maxNoiseVal = Mathf.Max(maxNoiseVal, noise);
                    minNoiseVal = Mathf.Min(minNoiseVal, noise);
                    
                    int index = x + y * generator.resolution + z * generator.resolution * generator.resolution;
                    colors[index] = new Color(noise, noise, noise, 1f);
                }
            }
        }
        
        // 値の正規化
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i].r = Mathf.InverseLerp(minNoiseVal, maxNoiseVal, colors[i].r);
            colors[i].g = colors[i].r;
            colors[i].b = colors[i].r;
        }

        noiseTexture.SetPixels(colors);
        noiseTexture.Apply();

        // テクスチャをアセットとして保存
        string savePath = SavePathBase + System.DateTime.Now.Ticks + ".asset";
        
        // 保存先ディレクトリの作成
        string directoryPath = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // 既存のアセットを削除
        if (generator.savedNoiseTexture != null)
        {
            string existingPath = AssetDatabase.GetAssetPath(generator.savedNoiseTexture);
            if (!string.IsNullOrEmpty(existingPath))
            {
                AssetDatabase.DeleteAsset(existingPath);
            }
        }

        // 新しいアセットとして保存
        AssetDatabase.CreateAsset(noiseTexture, savePath);
        AssetDatabase.SaveAssets();
        
        // Generatorの参照を更新
        generator.savedNoiseTexture = AssetDatabase.LoadAssetAtPath<Texture3D>(savePath);
        EditorUtility.SetDirty(generator); // Generatorの変更を保存
        
        // マテリアルに適用
        if (generator.targetMaterial != null)
        {
            generator.targetMaterial.SetTexture("_NoiseTex", generator.savedNoiseTexture);
            EditorUtility.SetDirty(generator.targetMaterial);
            Debug.Log("Noise texture generated, saved, and applied to material.");
        }
    }
}