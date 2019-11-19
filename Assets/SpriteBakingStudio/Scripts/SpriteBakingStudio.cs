using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SBS
{
    public delegate void ModelRotationCallback();

    [DisallowMultipleComponent]
    public class SpriteBakingStudio : MonoBehaviour
    {
        [SerializeField]
        public StudioSetting setting;

        [NonSerialized]
        public string[] atlasSizes = new string[] { "128", "256", "512", "1024", "2048", "4096", "8192" };

        [NonSerialized]
        public List<SamplingData> samplings = new List<SamplingData>();
        [NonSerialized]
        public List<Frame> selectedFrames = new List<Frame>();

        [NonSerialized]
        public bool isSamplingReady = false;
        [NonSerialized]
        public bool isBakingReady = false;
        [NonSerialized]
        public bool isShadowReady = false;

        //----------------------------------------------------------------------

        public enum BakingState
        {
            Initialize,
            BeginModel,
            BeginView,
            BeginFrame,
            CaptureFrame,
            EndView,
            EndFrame,
            EndModel,
            Finalize
        }
        private SimpleStateMachine<BakingState> stateMachine = null;

        [NonSerialized]
        public int checkedViewSize = 0;
        [NonSerialized]
        public List<string> checkedViewNames = new List<string>();
        [NonSerialized]
        public List<ModelRotationCallback> checkedViewFuncs = new List<ModelRotationCallback>();

        private int currModelIndex = 0;
        private StudioModel currModel = null;
        private List<StudioModel> checkedModels = new List<StudioModel>();

        private int currViewIndex = 0;
        private string currViewName;
        private int frameCount = 0;
        private int currFrameIndex = 0;
        private int resolutionX, resolutionY;

        private CameraClearFlags tempCamClearFlags;

        private Color tempCamBgColor;
        private float tempCameraSize;
        private Vector3 tempCameraPos;
        private Quaternion tempCameraRot;

        private TrimProperty trim = null;
        private OutputProperty output = null;

        private string outputPath;

        private ScreenPoint currPivot = new ScreenPoint(0, 0);
        private TextureBound exTexBound;

        private Vector3 camViewInitPos = Vector3.zero;

        private List<Texture2D> viewTextures = null;
        private List<ScreenPoint> viewPivots = null;
        private List<List<Texture2D>> viewTexturesList = new List<List<Texture2D>>();
        private List<List<ScreenPoint>> viewPivotsList = new List<List<ScreenPoint>>();

        private List<Texture2D> modelTextures = null;
        private List<ScreenPoint> modelPivots = null;
        private List<List<Texture2D>> modelTexturesList = new List<List<Texture2D>>();
        private List<List<ScreenPoint>> modelPivotsList = new List<List<ScreenPoint>>();
        private List<TextureBound> modelTexBounds = new List<TextureBound>();

        private double prevTime = 0.0;

        public bool IsBakingNow()
        {
            return (stateMachine != null);
        }

        public void BakeSprites()
        {
#if UNITY_EDITOR
            EditorApplication.update -= UpdateState;
            EditorApplication.update += UpdateState;
#endif

            stateMachine = new SimpleStateMachine<BakingState>();
            stateMachine.AddState(BakingState.Initialize, OnInitialize);
            stateMachine.AddState(BakingState.BeginModel, OnBeginModel);
            stateMachine.AddState(BakingState.BeginView, OnBeginView);
            stateMachine.AddState(BakingState.BeginFrame, OnBeginFrame);
            stateMachine.AddState(BakingState.CaptureFrame, OnCaptureFrame);
            stateMachine.AddState(BakingState.EndView, OnEndView);
            stateMachine.AddState(BakingState.EndFrame, OnEndFrame);
            stateMachine.AddState(BakingState.EndModel, OnEndModel);
            stateMachine.AddState(BakingState.Finalize, OnFinalize);

            stateMachine.ChangeState(BakingState.Initialize);
        }

        public void UpdateState()
        {
            if (stateMachine != null)
                stateMachine.Update();
        }

        #region State Machine

        public void OnInitialize()
        {
            try
            {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Progress...", "Ready...", 0.0f);
#endif

                resolutionX = (int)setting.textureResolution.x;
                resolutionY = (int)setting.textureResolution.y;
                Camera.main.targetTexture = new RenderTexture(resolutionX, resolutionY, 24, RenderTextureFormat.ARGB32);
                tempCamClearFlags = Camera.main.clearFlags;
                tempCamBgColor = Camera.main.backgroundColor;
                tempCameraSize = Camera.main.orthographicSize;
                tempCameraPos = Camera.main.transform.position;
                tempCameraRot = Camera.main.transform.rotation;

                if (!setting.IsModelGroup())
                {
                    currModel = setting.model;
                }
                else // setting.IsModelGroup()
                {
                    setting.GetModelGroup().InactivateAllModels();

                    currModelIndex = 0;
                    checkedModels = setting.GetModelGroup().GetCheckedModels();

                    modelTexturesList.Clear();
                    modelPivotsList.Clear();
                    modelTexBounds.Clear();
                }

                exTexBound = new TextureBound();

                trim = setting.trim.CloneForBaking(setting.IsSingleStaticModel());
                output = setting.output.CloneForBaking(setting.IsStaticModel());

                if (samplings.Count == 0)
                {
                    if (setting.IsStaticModel())
                    {
                        selectedFrames.Add(Frame.begin);
                    }
                    else
                    {
                        for (int i = 0; i < setting.frameSize; ++i)
                        {
                            float frameRatio = 0.0f;
                            if (i > 0 && i < setting.frameSize)
                                frameRatio = (float)i / (float)(setting.frameSize - 1);

                            float time = currModel.GetTimeForRatio(frameRatio);
                            selectedFrames.Add(new Frame(i, time));
                        }
                    }
                }

                frameCount = selectedFrames.Count;

                TileUtils.HideAllTiles();

                DateTime now = DateTime.Now;
                string year = now.Year.ToString();
                string month = now.Month >= 10 ? now.Month.ToString() : "0" + now.Month;
                string day = now.Day >= 10 ? now.Day.ToString() : "0" + now.Day;
                string hour = now.Hour >= 10 ? now.Hour.ToString() : "0" + now.Hour;
                string minute = now.Minute >= 10 ? now.Minute.ToString() : "0" + now.Minute;
                string second = now.Second >= 10 ? now.Second.ToString() : "0" + now.Second;
                string timeStr = year.Substring(2, 2) + month + day + "_" + hour + minute + second;
                string folderName = setting.fileName + "_" + timeStr;

                outputPath = Path.Combine(setting.outputPath, folderName);
                Directory.CreateDirectory(outputPath);

                stateMachine.ChangeState(BakingState.BeginModel);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                stateMachine.ChangeState(BakingState.Finalize);
            }
        }

        public void OnBeginModel()
        {
            try
            {
                if (setting.IsModelGroup())
                {
                    currModel = checkedModels[currModelIndex];
                    currModel.gameObject.SetActive(true);
                }

                currModel.UpdateModel(Frame.begin);

                currViewIndex = 0;

                if (setting.IsModelGroup() && !trim.allUnified)
                    exTexBound = new TextureBound();

                modelTextures = new List<Texture2D>();
                modelPivots = new List<ScreenPoint>();

                viewTexturesList.Clear();
                viewPivotsList.Clear();

                stateMachine.ChangeState(BakingState.BeginView);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                stateMachine.ChangeState(BakingState.Finalize);
            }
        }

        public void OnBeginView()
        {
            try
            {
                checkedViewFuncs[currViewIndex]();
                currViewName = checkedViewNames[currViewIndex];

                if (!setting.IsModelGroup() && !trim.allUnified)
                    exTexBound = new TextureBound();

                Vector3 pivot3D = Camera.main.WorldToScreenPoint(currModel.GetPivotPosition());
                currPivot.x = (int)pivot3D.x;
                currPivot.y = (int)pivot3D.y;

                currFrameIndex = 0;

                viewTextures = new List<Texture2D>();
                viewPivots = new List<ScreenPoint>();

                camViewInitPos = Camera.main.transform.position;

                stateMachine.ChangeState(BakingState.BeginFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                stateMachine.ChangeState(BakingState.Finalize);
            }
        }

        public void OnBeginFrame()
        {
            try
            {
#if UNITY_EDITOR
                if (!setting.IsModelGroup())
                {
                    int shownCurrFrameIndex = currFrameIndex + 1;
                    float progress = (float)(currViewIndex * frameCount + shownCurrFrameIndex) / (checkedViewSize * frameCount);
                    if (checkedViewFuncs.Count == 0)
                        EditorUtility.DisplayProgressBar("Progress...", "Frame: " + shownCurrFrameIndex + " (" + ((int)(progress * 100f)) + "%)", progress);
                    else
                        EditorUtility.DisplayProgressBar("Progress...", "View: " + currViewName + " | Frame: " + shownCurrFrameIndex + " (" + ((int)(progress * 100f)) + "%)", progress);
                }
                else // setting.IsModelGroup()
                {
                    int shownCurrViewIndex = currViewIndex + 1;
                    float progress = (float)(currModelIndex * checkedViewSize + shownCurrViewIndex) / (checkedModels.Count * checkedViewSize);
                    if (checkedModels.Count == 0)
                        EditorUtility.DisplayProgressBar("Progress...", "View: " + shownCurrViewIndex + " (" + ((int)(progress * 100f)) + "%)", progress);
                    else
                        EditorUtility.DisplayProgressBar("Progress...", "Model: " + currModel.name + " | View: " + shownCurrViewIndex + " (" + ((int)(progress * 100f)) + "%)", progress);
                }
#endif

                Frame frame = selectedFrames[currFrameIndex];
                currModel.UpdateModel(frame);

                stateMachine.ChangeState(BakingState.CaptureFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                stateMachine.ChangeState(BakingState.Finalize);
            }
        }

        public void OnCaptureFrame()
        {
            try
            {
#if UNITY_EDITOR
                double deltaTime = EditorApplication.timeSinceStartup - prevTime;
                if (deltaTime < setting.delay)
                    return;
                prevTime = EditorApplication.timeSinceStartup;
#endif

                Camera.main.transform.position = camViewInitPos;

                Texture2D tex = StudioUtility.PrepareShadowAndExtractTexture(this);

                TextureBound texBound = new TextureBound();
                if (!TextureUtils.CalcTextureBound(tex, currPivot, texBound))
                {
                    texBound.minX = currPivot.x - 1;
                    texBound.maxX = currPivot.x + 1;
                    texBound.minY = currPivot.y - 1;
                    texBound.maxY = currPivot.y + 1;
                }

                ScreenPoint pivot = new ScreenPoint(currPivot.x, currPivot.y);

                if (trim.on)
                {
                    if (texBound.minX - trim.spriteMargin >= 0)
                        texBound.minX -= trim.spriteMargin;
                    if (texBound.maxX + trim.spriteMargin < resolutionX)
                        texBound.maxX += trim.spriteMargin;
                    if (texBound.minY - trim.spriteMargin >= 0)
                        texBound.minY -= trim.spriteMargin;
                    if (texBound.maxY + trim.spriteMargin < resolutionY)
                        texBound.maxY += trim.spriteMargin;

                    if (!trim.useUnifiedSize)
                    {
                        tex = TextureUtils.TrimTexture(tex, texBound);
                        TextureUtils.UpdatePivot(pivot, tex, texBound);
                    }
                }

                if (output.type == OutputType.Separately && !trim.useUnifiedSize)
                {
                    if (!setting.IsModelGroup())
                        BakeSeparately(tex, pivot, currViewName, currFrameIndex);
                    else
                        BakeSeparately(tex, pivot, currModel.name, currViewName);
                }
                else if (output.type == OutputType.SpriteSheet || trim.useUnifiedSize)
                {
                    if (!setting.IsModelGroup())
                    {
                        viewTextures.Add(tex);
                        viewPivots.Add(pivot);
                    }
                    else
                    {
                        modelTextures.Add(tex);
                        modelPivots.Add(pivot);
                    }

                    if (trim.useUnifiedSize)
                    {
                        TextureUtils.CalcTextureSymmetricBound(
                            trim.pivotSymmetrically,
                            setting.IsTopView(),
                            pivot, resolutionX, resolutionY,
                            texBound, exTexBound);
                    }
                }

                stateMachine.ChangeState(BakingState.EndFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                stateMachine.ChangeState(BakingState.Finalize);
            }
        }

        public void OnEndFrame()
        {
            try
            {
                currFrameIndex++;

                if (currFrameIndex < frameCount)
                    stateMachine.ChangeState(BakingState.BeginFrame);
                else
                    stateMachine.ChangeState(BakingState.EndView);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                stateMachine.ChangeState(BakingState.Finalize);
            }
        }

        public void OnEndView()
        {
            try
            {
                if (!setting.IsModelGroup())
                {
                    if (output.type == OutputType.Separately && trim.useUnifiedSize)
                    {
                        if (!trim.allUnified)
                        {
                            TrimToUnifiedSize(viewTextures, viewPivots);
                            BakeSeparately(viewTextures, viewPivots, currViewName);
                        }
                        else
                        {
                            viewTexturesList.Add(viewTextures);
                            viewPivotsList.Add(viewPivots);
                        }
                    }
                    else if (output.type == OutputType.SpriteSheet)
                    {
                        if (!output.allInOneAtlas)
                        {
                            Debug.Assert(!setting.IsSingleStaticModel());

                            if (!trim.useUnifiedSize)
                            {
                                // trimmed or not
                                BakeSpriteSheet(viewTextures, viewPivots, currViewName);
                            }
                            else
                            {
                                if (!trim.allUnified)
                                {
                                    TrimToUnifiedSize(viewTextures, viewPivots);
                                    BakeSpriteSheet(viewTextures, viewPivots, currViewName);
                                }
                                else
                                {
                                    viewTexturesList.Add(viewTextures);
                                    viewPivotsList.Add(viewPivots);
                                }
                            }
                        }
                        else // output.allInOneAtlas
                        {
                            Debug.Assert(setting.IsStaticModel());
                            viewTexturesList.Add(viewTextures);
                            viewPivotsList.Add(viewPivots);
                        }
                    }
                }

                currViewIndex++;

                if (currViewIndex < checkedViewSize)
                {
                    stateMachine.ChangeState(BakingState.BeginView);
                }
                else
                {
                    if (!setting.IsModelGroup())
                        stateMachine.ChangeState(BakingState.Finalize);
                    else
                        stateMachine.ChangeState(BakingState.EndModel);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                stateMachine.ChangeState(BakingState.Finalize);
            }
        }

        public void OnEndModel()
        {
            try
            {
                if (output.type == OutputType.Separately && trim.useUnifiedSize)
                {
                    if (!trim.allUnified)
                    {
                        TrimToUnifiedSize(modelTextures, modelPivots);
                        BakeSeparately(modelTextures, modelPivots, currModel.name, checkedViewNames);
                    }
                    else
                    {
                        modelTexturesList.Add(modelTextures);
                        modelPivotsList.Add(modelPivots);
                    }
                }
                else if (output.type == OutputType.SpriteSheet)
                {
                    if (!trim.useUnifiedSize && !output.allInOneAtlas)
                    {
                        // trimmed or not
                        BakeSpriteSheet(modelTextures, modelPivots, currModel.name, checkedViewNames);
                    }
                    else // trim.useUnifiedSize || output.allInOneAtlas
                    {
                        if (!trim.allUnified && !output.allInOneAtlas)
                        {
                            Debug.Assert(trim.useUnifiedSize);
                            TrimToUnifiedSize(modelTextures, modelPivots);
                            BakeSpriteSheet(modelTextures, modelPivots, currModel.name, checkedViewNames);
                        }
                        else // trim.allUnified || output.allInOneAtlas
                        {
                            modelTexturesList.Add(modelTextures);
                            modelPivotsList.Add(modelPivots);

                            if (trim.useUnifiedSize && !trim.allUnified && output.allInOneAtlas)
                                modelTexBounds.Add(exTexBound);
                        }
                    }
                }

                currModel.gameObject.SetActive(false);

                currModelIndex++;

                if (currModelIndex < checkedModels.Count)
                    stateMachine.ChangeState(BakingState.BeginModel);
                else
                    stateMachine.ChangeState(BakingState.Finalize);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                stateMachine.ChangeState(BakingState.Finalize);
            }
        }

        public void OnFinalize()
        {
            try
            {
#if UNITY_EDITOR
                EditorApplication.update -= UpdateState;
#endif
                stateMachine = null;

                if (samplings.Count == 0)
                    selectedFrames.Clear();

                if (setting.IsModelGroup())
                    setting.GetModelGroup().ActivateBiggestModel();

                currModel.UpdateModel(Frame.begin);

                if (checkedViewFuncs.Count >= 2)
                    checkedViewFuncs[0]();

                if (setting.IsStaticRealShadow() && setting.shadow.staticShadowVisible)
                    BakeStaticShadow();

                if (trim.allUnified || output.allInOneAtlas)
                {
                    if (!setting.IsModelGroup())
                    {
                        Debug.Assert(viewTexturesList.Count == viewPivotsList.Count);

                        if (trim.allUnified)
                        {
                            Debug.Assert(!setting.IsSingleStaticModel());

                            for (int i = 0; i < viewTexturesList.Count; i++)
                            {
                                TrimToUnifiedSize(viewTexturesList[i], viewPivotsList[i]);

                                if (output.type == OutputType.Separately)
                                    BakeSeparately(viewTexturesList[i], viewPivotsList[i], checkedViewNames[i]);
                                else if (output.type == OutputType.SpriteSheet)
                                    BakeSpriteSheet(viewTexturesList[i], viewPivotsList[i], checkedViewNames[i]);
                            }
                        }
                        else if (output.allInOneAtlas)
                        {
                            Debug.Assert(setting.IsSingleStaticModel());

                            List<Texture2D> allViewTextures = new List<Texture2D>();
                            List<ScreenPoint> allViewPivots = new List<ScreenPoint>();
                            for (int i = 0; i < viewTexturesList.Count; i++)
                            {
                                allViewTextures.AddRange(viewTexturesList[i]);
                                allViewPivots.AddRange(viewPivotsList[i]);
                            }

                            if (trim.useUnifiedSize)
                                TrimToUnifiedSize(allViewTextures, allViewPivots);

                            Debug.Assert(allViewTextures.Count == allViewPivots.Count);
                            Debug.Assert(allViewTextures.Count == checkedViewNames.Count);

                            BakeSpriteSheet(allViewTextures, allViewPivots, "", checkedViewNames);
                        }
                    }
                    else // setting.IsModelGroup()
                    {
                        Debug.Assert(modelTexturesList.Count == modelPivotsList.Count);

                        if (!output.allInOneAtlas)
                        {
                            Debug.Assert(trim.allUnified);

                            for (int i = 0; i < modelTexturesList.Count; i++)
                            {
                                TrimToUnifiedSize(modelTexturesList[i], modelPivotsList[i]);

                                if (output.type == OutputType.Separately)
                                    BakeSeparately(modelTexturesList[i], modelPivotsList[i], checkedModels[i].name, checkedViewNames);
                                else if (output.type == OutputType.SpriteSheet)
                                    BakeSpriteSheet(modelTexturesList[i], modelPivotsList[i], checkedModels[i].name, checkedViewNames);
                            }
                        }
                        else // output.allInOneAtlas
                        {
                            Debug.Assert(output.type == OutputType.SpriteSheet);

                            if (!trim.useUnifiedSize)
                            {
                                // trimmed or not
                                List<Texture2D> allModelTextures = new List<Texture2D>();
                                List<ScreenPoint> allModelPivots = new List<ScreenPoint>();
                                List<string> allModelViewNames = new List<string>();
                                for (int i = 0; i < modelTexturesList.Count; i++)
                                {
                                    allModelTextures.AddRange(modelTexturesList[i]);
                                    allModelPivots.AddRange(modelPivotsList[i]);

                                    foreach (string viewName in checkedViewNames)
                                        allModelViewNames.Add(checkedModels[i].name + "_" + viewName);
                                }

                                Debug.Assert(allModelTextures.Count == allModelPivots.Count);
                                Debug.Assert(allModelTextures.Count == allModelViewNames.Count);

                                BakeSpriteSheet(allModelTextures, allModelPivots, "", allModelViewNames);
                            }
                            else // trim.useUnifiedSize
                            {
                                List<Texture2D> allModelTextures = new List<Texture2D>();
                                List<ScreenPoint> allModelPivots = new List<ScreenPoint>();
                                List<string> allModelViewNames = new List<string>();
                                for (int modeli = 0; modeli < modelTexturesList.Count; modeli++)
                                {
                                    if (!trim.allUnified)
                                    {
                                        Debug.Assert(modelTexturesList.Count == modelTexBounds.Count);
                                        for (int texi = 0; texi < modelTexturesList[modeli].Count; ++texi)
                                        {
                                            modelTexturesList[modeli][texi] =
                                                TextureUtils.TrimTexture(modelTexturesList[modeli][texi], modelTexBounds[modeli]);

                                            TextureUtils.UpdatePivot(modelPivotsList[modeli][texi], modelTexturesList[modeli][texi], modelTexBounds[modeli]);
                                        }
                                    }

                                    allModelTextures.AddRange(modelTexturesList[modeli]);
                                    allModelPivots.AddRange(modelPivotsList[modeli]);

                                    foreach (string viewName in checkedViewNames)
                                        allModelViewNames.Add(checkedModels[modeli].name + "_" + viewName);
                                }

                                Debug.Assert(allModelTextures.Count == allModelPivots.Count);
                                Debug.Assert(allModelTextures.Count == allModelViewNames.Count);

                                if (trim.allUnified)
                                    TrimToUnifiedSize(allModelTextures, allModelPivots);

                                BakeSpriteSheet(allModelTextures, allModelPivots, "", allModelViewNames);
                            }
                        }
                    }
                }

                TileUtils.UpdateTile(this);

                Camera.main.targetTexture = null;
                Camera.main.clearFlags = tempCamClearFlags;
                Camera.main.backgroundColor = tempCamBgColor;
                Camera.main.orthographicSize = tempCameraSize;
                Camera.main.transform.position = tempCameraPos;
                Camera.main.transform.rotation = tempCameraRot;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
#if UNITY_EDITOR
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
#endif
            }
        }

        #endregion // State Machine

        #region Baking

        private void TrimToUnifiedSize(List<Texture2D> textures, List<ScreenPoint> pivots)
        {
            try
            {
                for (int i = 0; i < textures.Count; ++i)
                {
                    textures[i] = TextureUtils.TrimTexture(textures[i], exTexBound);
                    TextureUtils.UpdatePivot(pivots[i], textures[i], exTexBound);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void BakeStaticShadow()
        {
            if (!isSamplingReady || !isShadowReady)
                return;

            TileUtils.HideAllTiles();

            setting.shadow.camera.CopyFrom(Camera.main);
            setting.shadow.camera.transform.position = setting.shadow.cameraPosition;
            TransformUtils.LookAtModel(setting.shadow.camera.transform, setting.model);
            setting.shadow.camera.targetDisplay = 1;

            setting.shadow.fieldObj.SetActive(false);

            setting.shadow.camera.targetTexture = new RenderTexture(setting.shadow.camera.pixelWidth, setting.shadow.camera.pixelHeight, 24, RenderTextureFormat.ARGB32);
            Color bgColor = setting.shadow.camera.backgroundColor;

            Texture2D rawTex = StudioUtility.ExtractTexture(setting.shadow.camera, setting, true);

            string shadowDirPath = Path.Combine(Application.dataPath, "SpriteBakingStudio/Shadow");
            string fileName = Global.STATIC_SHADOW_NAME;
            TextureUtils.SaveTexture(shadowDirPath, fileName, rawTex);

#if UNITY_EDITOR
            AssetDatabase.Refresh();

            string filePath = Path.Combine(shadowDirPath, fileName + ".png");
            int assetIndex = filePath.IndexOf("Assets");
            if (assetIndex < 0)
                return;
            string assetFilePath = filePath.Substring(assetIndex, filePath.Length - assetIndex);

            Texture2D assetTex = AssetDatabase.LoadAssetAtPath(assetFilePath, typeof(Texture2D)) as Texture2D;
            if (assetTex != null)
            {
                MeshRenderer rndr = setting.shadow.fieldObj.GetComponent<MeshRenderer>();
                if (rndr == null)
                    return;

                rndr.sharedMaterial.mainTexture = assetTex;

                setting.shadow.camera.transform.position = new Vector3(0f, 500f, 0f);
                setting.shadow.camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                setting.shadow.fieldObj.transform.position = Vector3.zero;
                setting.shadow.fieldObj.transform.rotation = Quaternion.Euler(0f, 180, 0f);

                StudioUtility.UpdateShadowFieldSize(setting.shadow.camera, setting.shadow.fieldObj);

                setting.shadow.camera.transform.position = setting.shadow.cameraPosition;
                TransformUtils.LookAtModel(setting.shadow.camera.transform, setting.model);
                setting.shadow.fieldObj.transform.position = setting.shadow.fieldPosition;
                setting.shadow.fieldObj.transform.rotation = Quaternion.Euler(0f, setting.shadow.fieldRotation, 0f);

                setting.shadow.fieldObj.SetActive(true);
            }
#endif

            setting.shadow.camera.backgroundColor = bgColor;
            setting.shadow.camera.targetTexture = null;
        }

        private void BakeSeparately(List<Texture2D> textures, List<ScreenPoint> pivots, string subName, List<string> detailNames)
        {
            Debug.Assert(textures.Count == pivots.Count);
            Debug.Assert(textures.Count == detailNames.Count);

            try
            {
                for (int i = 0; i < textures.Count; i++)
                    BakeSeparately(textures[i], pivots[i], subName, detailNames[i]);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void BakeSeparately(List<Texture2D> textures, List<ScreenPoint> pivots, string subName)
        {
            try
            {
                for (int i = 0; i < textures.Count; i++)
                    BakeSeparately(textures[i], pivots[i], subName, i);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void BakeSeparately(Texture2D tex, ScreenPoint pivot, string subName, int frame)
        {
            try
            {
                string detailName = frame.ToString().PadLeft(frame.ToString().Length, '0');
                BakeSeparately(tex, pivot, subName, detailName);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void BakeSeparately(Texture2D tex, ScreenPoint pivot, string subName, string detailName = "")
        {
            try
            {
                string fileFullName = setting.fileName;
                if (subName.Length > 0)
                    fileFullName += "_" + subName;
                if (detailName.Length > 0)
                    fileFullName += "_" + detailName;

                TextureUtils.SaveTexture(outputPath, fileFullName, tex);

                if (tex != null)
                {
#if UNITY_EDITOR
                    AssetDatabase.Refresh();
                    string filePath = Path.Combine(outputPath, fileFullName + ".png");
                    int assetRootIndex = filePath.IndexOf("Assets");
                    if (assetRootIndex < 0)
                        return;
                    filePath = filePath.Substring(assetRootIndex, filePath.Length - assetRootIndex);

                    TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(filePath);
                    if (texImporter != null)
                    {
                        texImporter.textureType = TextureImporterType.Sprite;
                        texImporter.spriteImportMode = SpriteImportMode.Multiple;

                        SpriteMetaData[] metaData = new SpriteMetaData[1];
                        metaData[0].name = fileFullName;
                        metaData[0].rect = new Rect(0.0f, 0.0f, (float)tex.width, (float)tex.height);
                        metaData[0].alignment = (int)SpriteAlignment.Custom;
                        metaData[0].pivot = new Vector2((float)pivot.x / (float)tex.width,
                                                        (float)pivot.y / (float)tex.height);

                        texImporter.spritesheet = metaData;
                        AssetDatabase.ImportAsset(filePath);
                    }
#endif
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void BakeSpriteSheet(List<Texture2D> textures, List<ScreenPoint> pivots, string subName)
        {
            try
            {
                List<string> spriteNames = new List<string>();
                for (int i = 0; i < textures.Count; ++i)
                    spriteNames.Add(i.ToString());
                BakeSpriteSheet(textures, pivots, subName, spriteNames);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void BakeSpriteSheet(List<Texture2D> textures, List<ScreenPoint> pivots, string subName, List<string> spriteNames)
        {
            Debug.Assert(textures.Count == pivots.Count);
            Debug.Assert(textures.Count == spriteNames.Count);

            try
            {
                int atlasLength = 64;
                if (!int.TryParse(atlasSizes[output.atlasSizeIndex], out atlasLength))
                    atlasLength = 2048;

                Rect[] atlasRects = null;
                Texture2D atlasTex = null;

                if (output.algorithm == PackingAlgorithm.Optimized)
                {
                    atlasTex = new Texture2D(atlasLength, atlasLength, TextureFormat.ARGB32, false);

                    atlasRects = atlasTex.PackTextures(textures.ToArray(), output.spritePadding, atlasLength);
                    for (int i = 0; i < atlasRects.Length; i++)
                    {
                        Texture2D tex = textures[i];
                        float newX = atlasRects[i].x * atlasTex.width;
                        float newY = atlasRects[i].y * atlasTex.height;
                        atlasRects[i] = new Rect(newX, newY, (float)tex.width, (float)tex.height);
                    }
                }
                else if (output.algorithm == PackingAlgorithm.InOrder)
                {
                    int maxSpriteWidth = int.MinValue;
                    int maxSpriteHeight = int.MinValue;
                    foreach (Texture2D tex in textures)
                    {
                        maxSpriteWidth = Mathf.Max(tex.width, maxSpriteWidth);
                        maxSpriteHeight = Mathf.Max(tex.height, maxSpriteHeight);
                    }

                    while (atlasLength < maxSpriteWidth || atlasLength < maxSpriteHeight)
                        atlasLength *= 2;

                    int atlasWidth = atlasLength;
                    int atlasHeight = atlasLength;

                    while (true)
                    {
                        atlasTex = new Texture2D(atlasWidth, atlasHeight, TextureFormat.ARGB32, false);
                        TextureUtils.SetPixels(atlasTex, Color.clear);

                        atlasRects = new Rect[textures.Count];
                        int originY = atlasHeight - maxSpriteHeight;

                        bool needMultiply = false;

                        int atlasRectIndex = 0;
                        int currX = 0, currY = originY;
                        foreach (Texture2D tex in textures)
                        {
                            if (currX + tex.width > atlasWidth)
                            {
                                if (currY - maxSpriteHeight < 0)
                                {
                                    needMultiply = true;
                                    break;
                                }
                                currX = 0;
                                currY -= (maxSpriteHeight + output.spritePadding);
                            }
                            TextureUtils.WriteTexture(atlasTex, tex, currX, currY);
                            atlasRects[atlasRectIndex++] = new Rect(currX, currY, tex.width, tex.height);
                            currX += (tex.width + output.spritePadding);
                        }

                        if (needMultiply)
                        {
                            if (atlasWidth == atlasHeight)
                                atlasWidth *= 2;
                            else // atlasWidth > atlasHeight
                                atlasHeight *= 2;

                            if (atlasWidth > 8192)
                            {
                                Debug.Log("Output sprite sheet size is bigger than 8192 X 8192");
                                return;
                            }
                        }
                        else
                        {
                            atlasLength = atlasWidth;
                            break;
                        }
                    }
                }

                string fileFullName = setting.fileName;
                if (subName.Length > 0)
                    fileFullName += "_" + subName;
                TextureUtils.SaveTexture(outputPath, fileFullName, atlasTex);

#if UNITY_EDITOR
                AssetDatabase.Refresh();
                string filePath = Path.Combine(outputPath, fileFullName + ".png");
                int assetIndex = filePath.IndexOf("Assets");
                if (assetIndex < 0)
                    return;
                filePath = filePath.Substring(assetIndex, filePath.Length - assetIndex);

                TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(filePath);
                if (texImporter != null)
                {
                    texImporter.textureType = TextureImporterType.Sprite;
                    texImporter.spriteImportMode = SpriteImportMode.Multiple;
                    texImporter.maxTextureSize = atlasLength;

                    int texCount = textures.Count;
                    SpriteMetaData[] metaData = new SpriteMetaData[texCount];
                    for (int i = 0; i < texCount; i++)
                    {
                        metaData[i].name = fileFullName + spriteNames[i];
                        metaData[i].rect = atlasRects[i];
                        metaData[i].alignment = (int)SpriteAlignment.Custom;
                        metaData[i].pivot = new Vector2((float)pivots[i].x / (float)textures[i].width,
                                                        (float)pivots[i].y / (float)textures[i].height);
                    }
                    texImporter.spritesheet = metaData;

                    AssetDatabase.ImportAsset(filePath);
                }

                if (!(currModel is StudioStaticModel) && output.makeAnimationClip)
                {
                    Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath).OfType<Sprite>().ToArray();

                    AnimationClip animClip = new AnimationClip();
                    animClip.frameRate = setting.frameSamples;
                    animClip.wrapMode = WrapMode.Loop;

                    var spriteBinding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

                    ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[sprites.Length];
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        spriteKeyFrames[i] = new ObjectReferenceKeyframe();
                        float unitTime = 1f / setting.frameSamples;
                        spriteKeyFrames[i].time = setting.spriteInterval * i * unitTime;
                        spriteKeyFrames[i].value = sprites[i];
                    }

                    AnimationUtility.SetObjectReferenceCurve(animClip, spriteBinding, spriteKeyFrames);

                    filePath = Path.Combine(outputPath, fileFullName + ".anim");
                    assetIndex = filePath.IndexOf("Assets");
                    filePath = filePath.Substring(assetIndex);
                    AssetDatabase.CreateAsset(animClip, filePath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
#endif
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion // Baking
    }
}
