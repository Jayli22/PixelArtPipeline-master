using UnityEngine;

namespace SBS
{
    public class StudioUtility
    {
        static public Texture2D PrepareShadowAndExtractTexture(SpriteBakingStudio studio)
        {
            StudioSetting setting = studio.setting;

            Texture2D resultTexture = null;

            if (setting.shadow.type != ShadowType.None && studio.isShadowReady)
            {
                if (setting.shadow.type == ShadowType.Simple)
                    setting.model.RescaleSimpleShadow();
                else if (setting.IsStaticRealShadow())
                    studio.BakeStaticShadow();

                if (setting.shadow.shadowOnly)
                {
                    PickOutStaticShadow(setting);
                    {
                        Vector3 originalPosition = ThrowOutModelFarAway(setting.model);
                        {
                            resultTexture = ExtractTexture(Camera.main, setting);
                        }
                        PutModelBackInPlace(setting.model, originalPosition);
                    }
                    PushInStaticShadow(setting);
                }
                else if (!setting.shadow.shadowOnly && (setting.variation.on && setting.variation.excludeShadow))
                {
                    PickOutStaticShadow(setting);
                    {
                        GameObject shadowObject = null;
                        if (setting.shadow.type == ShadowType.Simple)
                            shadowObject = setting.model.simpleShadow.gameObject;
                        else if (setting.shadow.type == ShadowType.Real)
                            shadowObject = setting.shadow.fieldObj;

                        // Shadow Pass
                        Vector3 originalPosition = ThrowOutModelFarAway(setting.model);
                        Texture2D shadowTexture = ExtractTexture(Camera.main, setting, true);
                        PutModelBackInPlace(setting.model, originalPosition);

                        // Model Pass
                        shadowObject.SetActive(false);
                        Texture2D modelTexture = ExtractTexture(Camera.main, setting);
                        shadowObject.SetActive(true);

                        // merge texture
                        resultTexture = MergeTextures(setting, shadowTexture, modelTexture);
                    }
                    PushInStaticShadow(setting);
                }
                else
                {
                    resultTexture = ExtractTexture(Camera.main, setting);
                }
            }
            else
            {
                resultTexture = ExtractTexture(Camera.main, setting);
            }

            return resultTexture;
        }

        static public void PickOutStaticShadow(StudioSetting setting)
        {
            if (setting.shadow.type == ShadowType.Simple)
                setting.model.simpleShadow.gameObject.transform.parent = null;
        }

        static public void PushInStaticShadow(StudioSetting setting)
        {
            if (setting.shadow.type == ShadowType.Simple)
                setting.model.simpleShadow.gameObject.transform.parent = setting.model.gameObject.transform;
        }

        static public Vector3 ThrowOutModelFarAway(StudioModel model)
        {
            Vector3 originalPosition = model.transform.position;
            model.transform.position = CreateFarAwayPosition();
            return originalPosition;
        }

        static public void PutModelBackInPlace(StudioModel model, Vector3 originalPosition)
        {
            model.transform.position = originalPosition;
        }

        static public Vector3 CreateFarAwayPosition()
        {
            return new Vector3(10000f, 0f, 0f);
        }

        static public Texture2D ExtractTexture(Camera camera, StudioSetting setting, bool isShadow = false)
        {
            if (camera == null || setting.extractor == null)
                return Texture2D.whiteTexture;

            RenderTexture.active = camera.targetTexture;

            Texture2D resultTex = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.ARGB32, false);
            setting.extractor.Extract(camera, setting.model, setting.variation, isShadow, ref resultTex);

            RenderTexture.active = null;

            return resultTex;
        }

        static public Texture2D MergeTextures(StudioSetting setting, Texture2D baseTexture, Texture2D bodyTexture)
        {
            if (baseTexture.width != bodyTexture.width || baseTexture.height != bodyTexture.height)
            {
                Debug.LogError("baseTexture.width != bodyTexture.width || baseTexture.height != bodyTexture.height");
                return bodyTexture;
            }

            for (int x = 0; x < baseTexture.width; x++)
            {
                for (int y = 0; y < baseTexture.height; y++)
                {
                    Color bodyPixel = bodyTexture.GetPixel(x, y);
                    if (bodyPixel == Color.clear)
                        continue;

                    Color basePixel = baseTexture.GetPixel(x, y);
                    Color resultColor = (basePixel == Color.clear ? bodyPixel :
                        BlendColors(bodyPixel, basePixel, setting.variation.bodyBlendFactor, setting.variation.shadowBlendFactor));

                    baseTexture.SetPixel(x, y, resultColor);
                }
            }

            return baseTexture;
        }

        static public Color BlendColors(Color srcColor, Color dstColor, BlendFactor srcFactor, BlendFactor dstFactor)
        {
            return srcColor * MakeBlendFactor(srcColor, dstColor, srcFactor) +
                   dstColor * MakeBlendFactor(srcColor, dstColor, dstFactor);
        }

        static public Color MakeBlendFactor(Color srcPixel, Color dstPixel, BlendFactor factor)
        {
            switch (factor)
            {
            case BlendFactor.Zero:
                return Color.clear;

            case BlendFactor.One:
                return Color.white;

            case BlendFactor.SrcColor:
                return new Color(
                    srcPixel.r,
                    srcPixel.g,
                    srcPixel.b,
                    srcPixel.a);

            case BlendFactor.OneMinusSrcColor:
                return new Color(
                    (1f - srcPixel.r),
                    (1f - srcPixel.g),
                    (1f - srcPixel.b),
                    (1f - srcPixel.a));

            case BlendFactor.DstColor:
                return new Color(
                    dstPixel.r,
                    dstPixel.g,
                    dstPixel.b,
                    dstPixel.a);

            case BlendFactor.OneMinusDstColor:
                return new Color(
                    (1f - dstPixel.r),
                    (1f - dstPixel.g),
                    (1f - dstPixel.b),
                    (1f - dstPixel.a));

            case BlendFactor.SrcAlpha:
                return new Color(
                    srcPixel.a,
                    srcPixel.a,
                    srcPixel.a,
                    srcPixel.a);

            case BlendFactor.OneMinusSrcAlpha:
                return new Color(
                    (1f - srcPixel.a),
                    (1f - srcPixel.a),
                    (1f - srcPixel.a),
                    (1f - srcPixel.a));

            case BlendFactor.DstAlpha:
                return new Color(
                    dstPixel.a,
                    dstPixel.a,
                    dstPixel.a,
                    dstPixel.a);

            case BlendFactor.OneMinusDstAlpha:
                return new Color(
                    (1f - dstPixel.a),
                    (1f - dstPixel.a),
                    (1f - dstPixel.a),
                    (1f - dstPixel.a));
            }

            return Color.white;
        }

        static public void UpdateShadowFieldSize(Camera camera, GameObject fieldObject)
        {
            MeshRenderer fieldRenderer = fieldObject.GetComponent<MeshRenderer>();
            if (fieldRenderer == null)
                return;

            Vector3 maxWorldPos = camera.ScreenToWorldPoint(new Vector3(camera.pixelWidth, camera.pixelHeight));
            Vector3 minWorldPos = camera.ScreenToWorldPoint(Vector3.zero);
            Vector3 texWorldSize = maxWorldPos - minWorldPos;

            fieldObject.transform.localScale = Vector3.one;
            fieldObject.transform.position = Vector3.zero;

            fieldObject.transform.localScale = new Vector3
            (
                texWorldSize.x / fieldRenderer.bounds.size.x,
                1f,
                texWorldSize.z / fieldRenderer.bounds.size.z
            );
        }
    }
}
