﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace DigitalOpus.MB.Core
{
    public class TextureBlenderStandardMetallic : TextureBlender
    {
        static Color NeutralNormalMap = new Color(.5f, .5f, 1f);

        private enum Prop{
            doColor,
            doMetallic,
            doEmission,
            doBump,
            doNone,
        }

        // This is used to cache the non texture property values. If all non-texutre property values are the same for a property for all source textures
        // then the source value will be re-used
        TextureBlenderMaterialPropertyCacheHelper sourceMaterialPropertyCache = new TextureBlenderMaterialPropertyCacheHelper();

        // These are cached values read in OnBeforeTintTexture and used when blending pixels.
        Color m_tintColor;
        bool m_doScaleAlphaCutoff;
        float m_alphaCutoff;
        float m_glossiness;
        float m_glossMapScale;
        float m_metallic;
        bool m_hasMetallicGlossMap;
        float m_bumpScale;
        bool m_shaderDoesEmission;
        Color m_emissionColor;

        // This just makes things more efficient so we arn't doing a string comparison for each pixel.
        Prop propertyToDo = Prop.doNone; 

        // These are the property values that will be assigned to the result material if
        // generating an atlas for those properties. 
        Color m_generatingTintedAtlasColor = Color.white;
        float m_generatingTintedAtlasMetallic = 0f;
        float m_generatingTintedAtlasGlossiness = 1f;
        float m_generatingTintedAtlasGlossMapScale = 1f;
        float m_generatingTintedAtlasBumpScale = 1f;
        Color m_generatingTintedAtlasEmission = Color.white;
        const float m_generatedAlphaCutoff = .5f;

        // These are the default property values that will be assigned to the result materials if 
        // none of the source materials have a value for these properties.
        Color m_notGeneratingAtlasDefaultColor = Color.white;
        float m_notGeneratingAtlasDefaultMetallic = 0;
        float m_notGeneratingAtlasDefaultGlossiness = .5f;
        Color m_notGeneratingAtlasDefaultEmisionColor = Color.black;

        public bool DoesShaderNameMatch(string shaderName)
        {
            return shaderName.Equals("Standard") || shaderName.EndsWith("StandardTextureArray");
        }

        public void OnBeforeTintTexture(Material sourceMat, string shaderTexturePropertyName)
        {
            if (shaderTexturePropertyName.Equals("_MainTex"))
            {
                propertyToDo = Prop.doColor;
                if (sourceMat.HasProperty("_Color"))
                {
                    m_tintColor = sourceMat.GetColor("_Color");
                } else
                {
                    m_tintColor = m_generatingTintedAtlasColor;
                }

                if (sourceMat.HasProperty("_Mode") && 
                    sourceMat.HasProperty("_Cutoff") &&
                    sourceMat.GetFloat("_Mode") == 1  // Standard shader Cutout mode
                    )
                {
                    m_doScaleAlphaCutoff = true;
                    m_alphaCutoff = sourceMat.GetFloat("_Cutoff");
                    m_alphaCutoff = Mathf.Clamp(m_alphaCutoff, .0001f, .9999f);
                } else
                {
                    m_doScaleAlphaCutoff = false;
                    m_alphaCutoff = .5f;
                }
            } else if (shaderTexturePropertyName.Equals("_MetallicGlossMap"))
            {
                propertyToDo = Prop.doMetallic;
                m_metallic = m_generatingTintedAtlasMetallic;
                if (sourceMat.GetTexture("_MetallicGlossMap") != null)
                {
                    m_hasMetallicGlossMap = true;
                } else
                {
                    m_hasMetallicGlossMap = false;
                }

                if (sourceMat.HasProperty("_Metallic"))
                {
                    m_metallic = sourceMat.GetFloat("_Metallic");
                } else
                {
                    m_metallic = 0f;
                }

                if (sourceMat.HasProperty("_GlossMapScale"))
                {
                    m_glossMapScale = sourceMat.GetFloat("_GlossMapScale");
                } else
                {
                    m_glossMapScale = 1f;
                }

                if (sourceMat.HasProperty("_Glossiness"))
                {
                    m_glossiness = sourceMat.GetFloat("_Glossiness");
                } else
                {
                    m_glossiness = 0f;
                }

            } else if (shaderTexturePropertyName.Equals("_BumpMap"))
            {
                propertyToDo = Prop.doBump;
                if (sourceMat.HasProperty(shaderTexturePropertyName))
                {
                    if (sourceMat.HasProperty("_BumpScale"))
                        m_bumpScale = sourceMat.GetFloat("_BumpScale");
                }
                else
                {
                    m_bumpScale = m_generatingTintedAtlasBumpScale;
                }

            } else if (shaderTexturePropertyName.Equals("_EmissionMap"))
            {
                propertyToDo = Prop.doEmission;
                m_shaderDoesEmission = sourceMat.IsKeywordEnabled("_EMISSION");
                if (sourceMat.HasProperty("_EmissionColor")) {
                    m_emissionColor = sourceMat.GetColor("_EmissionColor");
                } else
                {
                    m_emissionColor = m_notGeneratingAtlasDefaultEmisionColor;
                }

            } else
            {
                propertyToDo = Prop.doNone;
            } 
        }

        public Color OnBlendTexturePixel(string propertyToDoshaderPropertyName, Color pixelColor)
        {
            if (propertyToDo == Prop.doColor)
            {
                Color c = new Color(pixelColor.r * m_tintColor.r, pixelColor.g * m_tintColor.g, pixelColor.b * m_tintColor.b, pixelColor.a * m_tintColor.a);
                if (m_doScaleAlphaCutoff)
                {
                    // The source mat is in AlphaCutoff mode. We need to blend the alpha cutoff into the alpha channel.
                    // The result-material will have Alpha cutoff of .5f. Scale the alpha channel pixels to account for this.
                    if (c.a >= m_alphaCutoff)
                    {
                        c.a = m_generatedAlphaCutoff + (1f - m_generatedAlphaCutoff) * (c.a - m_alphaCutoff) / (1f - m_alphaCutoff);
                    } else
                    {
                        c.a = (m_generatedAlphaCutoff) * (c.a) / (m_alphaCutoff);
                    }
                }

                return c;
            }
            else if (propertyToDo == Prop.doMetallic)
            {
                if (m_hasMetallicGlossMap)
                {
                    return pixelColor = new Color(pixelColor.r, pixelColor.g, pixelColor.b, pixelColor.a * m_glossMapScale);
                }
                else
                {
                    return new Color(m_metallic, 0, 0, m_glossiness);
                }
            }
            else if (propertyToDo == Prop.doBump)
            {
                return Color.Lerp(NeutralNormalMap, pixelColor, m_bumpScale);
            }
            else if (propertyToDo == Prop.doEmission)
            {
                if (m_shaderDoesEmission)
                {
                    return new Color(pixelColor.r * m_emissionColor.r, pixelColor.g * m_emissionColor.g, pixelColor.b * m_emissionColor.b, pixelColor.a * m_emissionColor.a);
                }
                else
                {
                    return Color.black;
                }
            }
            return pixelColor;
        }

        public bool NonTexturePropertiesAreEqual(Material a, Material b)
        {
            if (!TextureBlenderFallback._compareColor(a, b, m_notGeneratingAtlasDefaultColor, "_Color"))
            {
                return false;
            }

            if (a.HasProperty("_Mode") && b.HasProperty("_Mode") &&
                a.GetFloat("_Mode") == 1 && b.GetFloat("_Mode") == 1 &&
                a.HasProperty("_Cutoff") && b.HasProperty("_Cutoff"))
            {
                // Compare alpha cutoff values
                if (a.HasProperty("_Cutoff") != b.HasProperty("_Cutoff"))
                {
                    return false;
                }
            }

            if (!TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultGlossiness, "_Glossiness"))
            {
                return false;
            }

            bool aHasMetallicTex = a.HasProperty("_MetallicGlossMap") && a.GetTexture("_MetallicGlossMap") != null;
            bool bHasMetallicTex = b.HasProperty("_MetallicGlossMap") && b.GetTexture("_MetallicGlossMap") != null;
            if (aHasMetallicTex && bHasMetallicTex)
            {
                if (!TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultMetallic, "_GlossMapScale"))
                {
                    return false;
                }
            } else if (!aHasMetallicTex && !bHasMetallicTex)
            {
                if (!TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultMetallic, "_Metallic"))
                {
                    return false;
                }
            } else
            {
                return false;
            }

            if (a.IsKeywordEnabled("_EMISSION") != b.IsKeywordEnabled("_EMISSION"))
            {
                return false;
            }
            if (a.IsKeywordEnabled("_EMISSION"))
            {
                if (!TextureBlenderFallback._compareColor(a, b, m_notGeneratingAtlasDefaultEmisionColor, "_EmissionColor"))
                {
                    return false;
                }
            }
            return true;
        }

        public void SetNonTexturePropertyValuesOnResultMaterial(Material resultMaterial)
        {
            if (resultMaterial.GetTexture("_MainTex")  != null)
            {
                resultMaterial.SetColor("_Color", m_generatingTintedAtlasColor);
                if (resultMaterial.GetFloat("_Mode") == 1) // Alpha cutoff
                {
                    resultMaterial.SetFloat("_Cutoff", m_generatedAlphaCutoff);
                }
            } else {
                resultMaterial.SetColor("_Color", (Color) sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_Color", m_notGeneratingAtlasDefaultColor));
            }

            if (resultMaterial.GetTexture("_MetallicGlossMap") != null)
            {
                resultMaterial.SetFloat("_Metallic", m_generatingTintedAtlasMetallic);
                resultMaterial.SetFloat("_GlossMapScale", m_generatingTintedAtlasGlossMapScale);
                resultMaterial.SetFloat("_Glossiness", m_generatingTintedAtlasGlossiness);
            } else
            {
                resultMaterial.SetFloat("_Metallic", (float) sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_Metallic", m_notGeneratingAtlasDefaultMetallic));
                resultMaterial.SetFloat("_Glossiness", (float) sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_Glossiness", m_notGeneratingAtlasDefaultGlossiness));
            }

            if (resultMaterial.GetTexture("_BumpMap") != null)
            {
                resultMaterial.SetFloat("_BumpScale", m_generatingTintedAtlasBumpScale);
            }

            if (resultMaterial.GetTexture("_EmissionMap") != null)
            {
                resultMaterial.EnableKeyword("_EMISSION");
                resultMaterial.SetColor("_EmissionColor", m_generatingTintedAtlasEmission);
            }
            else {
                resultMaterial.DisableKeyword("_EMISSION");
                resultMaterial.SetColor("_EmissionColor", (Color) sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_EmissionColor", m_notGeneratingAtlasDefaultEmisionColor));
            }
        }


        public Color GetColorIfNoTexture(Material mat, ShaderTextureProperty texPropertyName)
        {
            if (texPropertyName.name.Equals("_BumpMap"))
            {
                return TextureBlenderFallback.GetDefaultNormalMapColor();
            }
            else if (texPropertyName.name.Equals("_MainTex"))
            {
                if (mat != null && mat.HasProperty("_Color"))
                {
                    /*
                    try
                    { //need try because can't garantee _Color is a color
                        Color c = mat.GetColor("_Color");
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_Color", c);
                        return c;
                    }
                    catch (Exception) { }
                    */
                    // Why don't we return _Color here?
                    // Source object has:  no-texture,   _Color  ==>   look like _Color
                    // Atlas needs:
                    //   CORRECT:                whiteBlock,  _Color  ==>   look like _Color
                    //   WRONG:              solidTex_Color,  _Color ==>  look like _Color * _Color
                    return Color.white;
                }
            }
            else if (texPropertyName.name.Equals("_MetallicGlossMap"))
            {
                if (mat != null && mat.HasProperty("_Metallic"))
                {
                    try
                    { //need try because can't garantee _Metallic is a float
                        float v = mat.GetFloat("_Metallic");
                        Color c = new Color(v, v, v);
                        if (mat.HasProperty("_Glossiness"))
                        {
                            try
                            {
                                c.a = mat.GetFloat("_Glossiness");
                            }

                            catch (Exception) { }
                        }

                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_Metallic", v);
                        sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_Glossiness", c.a);
                        return c;
                    }
                    catch (Exception) { }
                    return new Color(0f, 0f, 0f, .5f);
                } else
                {
                    return new Color(0f,0f,0f,.5f);
                }
            }
            else if (texPropertyName.name.Equals("_ParallaxMap"))
            {
                return new Color(0f, 0f, 0f, 0f);
            }
            else if (texPropertyName.name.Equals("_OcclusionMap"))
            {
                return new Color(1f, 1f, 1f, 1f);
            }
            else if (texPropertyName.name.Equals("_EmissionMap"))
            {
                if (mat != null)
                {
                    if (mat.IsKeywordEnabled("_EMISSION"))
                    {
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            try
                            {
                                Color c = mat.GetColor("_EmissionColor");
                                sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_EmissionColor", c);
                                return c;
                            }
                            catch (Exception) { }
                        }
                        else
                        {
                            return Color.black;
                        }
                    } else
                    {
                        return Color.black;
                    }
                }
            }
            else if (texPropertyName.name.Equals("_DetailMask"))
            {
                return new Color(0f, 0f, 0f, 0f);
            } 
            return new Color(1f, 1f, 1f, 0f);
        }
    }
}
