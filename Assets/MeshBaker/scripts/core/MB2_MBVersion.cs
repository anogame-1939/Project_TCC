/**
 *	\brief Hax!  DLLs cannot interpret preprocessor directives, so this class acts as a "bridge"
 */
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DigitalOpus.MB.Core{

    public interface MBVersionInterface{
		string version();

        bool Is_2018_3_OrNewer();

        bool Is_2017_1_OrNewer();

        bool GetActive(GameObject go);
		void SetActive(GameObject go, bool isActive);
		void SetActiveRecursively(GameObject go, bool isActive);
		UnityEngine.Object[] FindSceneObjectsOfType(Type t);
		bool IsRunningAndMeshNotReadWriteable(Mesh m);
        Vector2[] GetMeshUVChannel(int channel, Mesh m, MB2_LogLevel LOG_LEVEL);
        void MeshClear(Mesh m, bool t);
        void MeshAssignUVChannel(int channel, Mesh m, Vector2[] uvs);
        Vector4 GetLightmapTilingOffset(Renderer r);
		Transform[] GetBones(Renderer r, bool isSkinnedMeshWithBones);
        void OptimizeMesh(Mesh m);
        int GetBlendShapeFrameCount(Mesh m, int shapeIndex);
        float GetBlendShapeFrameWeight(Mesh m, int shapeIndex, int frameIndex);
        void GetBlendShapeFrameVertices(Mesh m, int shapeIndex, int frameIndex, Vector3[] vs, Vector3[] ns, Vector3[] ts);
        void ClearBlendShapes(Mesh m);
        void AddBlendShapeFrame(Mesh m, string nm, float wt, Vector3[] vs, Vector3[] ns, Vector3[] ts);
        int MaxMeshVertexCount();
        void SetMeshIndexFormatAndClearMesh(Mesh m, int numVerts, bool vertices, bool justClearTriangles);
        bool GraphicsUVStartsAtTop();

        bool IsTextureReadable(Texture2D tex);

        bool IsSwizzledNormalMapPlatform();

        bool IsMaterialKeywordValid(Material mat, string keyword);

        /// <summary>
        /// Prior to Unity 2022.1 this is not available
        /// </summary>
        bool IsTexture_sRGBgammaCorrected(Texture2D tex, bool hint);

        bool CollectPropertyNames(List<ShaderTextureProperty> texPropertyNames, ShaderTextureProperty[] shaderTexPropertyNames, List<ShaderTextureProperty> _customShaderPropNames, Material resultMaterial, MB2_LogLevel LOG_LEVEL);

        void DoSpecialRenderPipeline_TexturePackerFastSetup(GameObject cameraGameObject);

        ColorSpace GetProjectColorSpace();

        MBVersion.PipelineType DetectPipeline();

        string UnescapeURL(string url);

        IEnumerator FindRuntimeMaterialsFromAddresses(MB2_TextureBakeResults textureBakeResult, MB2_TextureBakeResults.CoroutineResult isComplete);

        float GetScaleInLightmap(MeshRenderer r);

        bool IsAssetInProject(UnityEngine.Object target);
    }

    public class MBVersion
	{

        public const string MB_USING_HDRP = "MB_USING_HDRP";

        public enum PipelineType
        {
            Unsupported,
            Default,
            URP,
            HDRP
        }

        private static MBVersionInterface _MBVersion;

        /// <summary>
        /// Why this complicated way to create?  Because MBVersionConcrete is outside the MeshBaker.dll assembly.
        /// Classes inside MeshBaker.dll, can't see out into the Assembly-CSharp.dll. We don't want to do any
        /// Conditional compilation inside the MeshBaker.dll assembly because the evaluation version compiles this 
        /// assembly in the editor.
        /// </summary>
		private static MBVersionInterface _CreateMBVersionConcrete(){
			Type vit = null;
#if EVAL_VERSION
			vit = Type.GetType("DigitalOpus.MB.Core.MBVersionConcrete,Assembly-CSharp");
#else
			vit = typeof(MBVersionConcrete);
#endif
			return (MBVersionInterface) System.Activator.CreateInstance(vit);
		}

		public static string version(){
			if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
			return _MBVersion.version();
		}
		
        public static bool Is_2018_3_OrNewer()
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.Is_2018_3_OrNewer();
        }

        public static bool Is_2017_1_OrNewer()
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.Is_2017_1_OrNewer();
        }

		public static bool GetActive(GameObject go){
			if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
			return _MBVersion.GetActive(go);
		}
	
		public static void SetActive(GameObject go, bool isActive){
			if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
			_MBVersion.SetActive(go,isActive);
		}
		
		public static void SetActiveRecursively(GameObject go, bool isActive){
			if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
			_MBVersion.SetActiveRecursively(go,isActive);
		}

		public static UnityEngine.Object[] FindSceneObjectsOfType(Type t){
			if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
			return _MBVersion.FindSceneObjectsOfType(t);				
		}

		public static bool IsRunningAndMeshNotReadWriteable(Mesh m){
			if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
			return _MBVersion.IsRunningAndMeshNotReadWriteable(m);
		}

        public static Vector2[] GetMeshChannel(int channel, Mesh m, MB2_LogLevel LOG_LEVEL) {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.GetMeshUVChannel(channel, m,LOG_LEVEL);
        }

        public static float GetScaleInLightmap(MeshRenderer r)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.GetScaleInLightmap(r);
        }

        public static void MeshClear(Mesh m, bool t){
			if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
			_MBVersion.MeshClear(m,t);
		}

        public static void MeshAssignUVChannel(int channel, Mesh m, Vector2[] uvs)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            _MBVersion.MeshAssignUVChannel(channel, m, uvs);
        }

        public static Vector4 GetLightmapTilingOffset(Renderer r){
			if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
			return _MBVersion.GetLightmapTilingOffset(r);
		}

		public static Transform[] GetBones(Renderer r, bool isSkinnedMeshWithBones)
        {
			if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
			return _MBVersion.GetBones(r, isSkinnedMeshWithBones);
		}

        public static bool IsSwizzledNormalMapPlatform()
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.IsSwizzledNormalMapPlatform();
        }

        public static bool IsMaterialKeywordValid(Material mat, string keyword)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.IsMaterialKeywordValid(mat, keyword);
        }

        public static void OptimizeMesh(Mesh m)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            _MBVersion.OptimizeMesh(m);
        }

        public static int GetBlendShapeFrameCount(Mesh m, int shapeIndex)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.GetBlendShapeFrameCount(m, shapeIndex);
        }

        public static float GetBlendShapeFrameWeight(Mesh m, int shapeIndex, int frameIndex)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.GetBlendShapeFrameWeight(m, shapeIndex, frameIndex);
        }

        public static void GetBlendShapeFrameVertices(Mesh m, int shapeIndex, int frameIndex, Vector3[] vs, Vector3[] ns, Vector3[] ts)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            _MBVersion.GetBlendShapeFrameVertices(m, shapeIndex, frameIndex, vs, ns, ts);
        }

        public static void ClearBlendShapes(Mesh m)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            _MBVersion.ClearBlendShapes(m);
        }

        public static void AddBlendShapeFrame(Mesh m, string nm, float wt, Vector3[] vs, Vector3[] ns, Vector3[] ts)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            _MBVersion.AddBlendShapeFrame(m, nm, wt, vs, ns, ts);
        }

        public static int MaxMeshVertexCount()
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.MaxMeshVertexCount();
        }

        public static void SetMeshIndexFormatAndClearMesh(Mesh m, int numVerts, bool vertices, bool justClearTriangles)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            _MBVersion.SetMeshIndexFormatAndClearMesh(m, numVerts, vertices, justClearTriangles);
        }

        public static bool GraphicsUVStartsAtTop()
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.GraphicsUVStartsAtTop();
        }

        /// <summary>
        /// Returns hint if the API doesn't support this
        /// </summary>
        public static bool IsTexture_sRGBgammaCorrected(Texture2D tex, bool hint)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.IsTexture_sRGBgammaCorrected(tex, hint);
        }

        public static bool IsTextureReadable(Texture2D tex)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.IsTextureReadable(tex);
        }

        public static void CollectPropertyNames(List<ShaderTextureProperty> texPropertyNames, ShaderTextureProperty[] shaderTexPropertyNames, List<ShaderTextureProperty> _customShaderPropNames, Material resultMaterial, MB2_LogLevel LOG_LEVEL)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            _MBVersion.CollectPropertyNames(texPropertyNames, shaderTexPropertyNames, _customShaderPropNames, resultMaterial, LOG_LEVEL);
        }

        internal static void DoSpecialRenderPipeline_TexturePackerFastSetup(GameObject cameraGameObject)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            _MBVersion.DoSpecialRenderPipeline_TexturePackerFastSetup(cameraGameObject);
        }

        public static ColorSpace GetProjectColorSpace()
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.GetProjectColorSpace();
        }

        public static PipelineType DetectPipeline()
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.DetectPipeline();
        }

        public static string UnescapeURL(string url)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.UnescapeURL(url);
        }

        public static bool IsAssetInProject(UnityEngine.Object target)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            return _MBVersion.IsAssetInProject(target);
        }

        public static bool IsUsingAddressables()
        {
            foreach (var s in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (s.ToString().Contains("Addressables"))
                {
                    System.Type t = s.GetType("UnityEngine.AddressableAssets.AssetReference");
                    if (t != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static IEnumerator FindRuntimeMaterialsFromAddresses(MB2_TextureBakeResults textureBakeResult, MB2_TextureBakeResults.CoroutineResult isComplete)
        {
            if (_MBVersion == null) _MBVersion = _CreateMBVersionConcrete();
            yield return _MBVersion.FindRuntimeMaterialsFromAddresses(textureBakeResult, isComplete);
        }
    }
}