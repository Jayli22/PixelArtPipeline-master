using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SBS
{
    public class FrameSampler
    {
        static private FrameSampler instance;
        static public FrameSampler GetInstance()
        {
            if (instance == null)
                instance = new FrameSampler();
            return instance;
        }

        private SpriteBakingStudio studio = null;
        private StudioSetting setting = null;

        public delegate void EndDelegate();
        public EndDelegate OnEnd;

        public enum SamplingState
        {
            Initialize = 0,
            BeginFrame,
            CaptureFrame,
            EndFrame,
            Finalize
        }
        private SimpleStateMachine<SamplingState> stateMachine;

        private int currFrameNumber = 0;
        private float currFrameTime = 0.0f;

        private int resolutionX, resolutionY;

        private CameraClearFlags tmpCamClearFlags;
        private Color tmpCamBgColor;

        private int frameSize = 1;

        private ScreenPoint screenPivot = new ScreenPoint(0, 0);
        private TextureBound exTexBound;

        private Vector3 camViewInitPos = Vector3.zero;

		private double prevTime = 0.0;

        public bool IsSamplingNow()
        {
            return (stateMachine != null);
        }

        public void SampleFrames(SpriteBakingStudio studio)
        {
            this.studio = studio;
            this.setting = studio.setting;

#if UNITY_EDITOR
            EditorApplication.update -= UpdateState;
            EditorApplication.update += UpdateState;
#endif

            stateMachine = new SimpleStateMachine<SamplingState>();
            stateMachine.AddState(SamplingState.Initialize, OnInitialize);
            stateMachine.AddState(SamplingState.BeginFrame, OnBeginFrame);
            stateMachine.AddState(SamplingState.CaptureFrame, OnCaptureFrame);
            stateMachine.AddState(SamplingState.EndFrame, OnEndFrame);
            stateMachine.AddState(SamplingState.Finalize, OnFinalize);

            stateMachine.ChangeState(SamplingState.Initialize);
        }

        public void UpdateState()
		{
            if (stateMachine != null)
                stateMachine.Update();
        }

        public void OnInitialize()
        {
            try
            {
                setting.model.UpdateModel(Frame.begin);

                currFrameNumber = 0;
                currFrameTime = 0.0f;
                resolutionX = (int)setting.textureResolution.x;
                resolutionY = (int)setting.textureResolution.y;
                Camera.main.targetTexture = new RenderTexture(resolutionX, resolutionY, 24, RenderTextureFormat.ARGB32);
                tmpCamClearFlags = Camera.main.clearFlags;
                tmpCamBgColor = Camera.main.backgroundColor;

                frameSize = !setting.IsStaticModel() ? setting.frameSize : 1;

                TileUtils.HideAllTiles();

                Vector3 screenPivot3D = Camera.main.WorldToScreenPoint(setting.model.GetPivotPosition());
                screenPivot.x = (int)screenPivot3D.x;
                screenPivot.y = (int)screenPivot3D.y;

                exTexBound = new TextureBound();

                studio.samplings.Clear();
                studio.selectedFrames.Clear();

                camViewInitPos = Camera.main.transform.position;

                stateMachine.ChangeState(SamplingState.BeginFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                stateMachine.ChangeState(SamplingState.Finalize);
            }
        }

        public void OnBeginFrame()
		{
            try
            {
#if UNITY_EDITOR
                int shownCurrFrameNumber = currFrameNumber + 1;
                float progress = (float)shownCurrFrameNumber / frameSize;
                EditorUtility.DisplayProgressBar("Sampling...", "Frame: " + shownCurrFrameNumber + " (" + ((int)(progress * 100f)) + "%)", progress);
#endif

                float frameRatio = 0.0f;
                if (currFrameNumber > 0 && currFrameNumber < frameSize)
                    frameRatio = (float)currFrameNumber / (float)(frameSize - 1);

                currFrameTime = setting.model.GetTimeForRatio(frameRatio);

                setting.model.UpdateModel(new Frame(currFrameNumber, currFrameTime));

                stateMachine.ChangeState(SamplingState.CaptureFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                stateMachine.ChangeState(SamplingState.Finalize);
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

                Texture2D tex = StudioUtility.PrepareShadowAndExtractTexture(studio);

                studio.samplings.Add(new SamplingData(tex, currFrameTime));

                TextureBound texBound = new TextureBound();
                if (!TextureUtils.CalcTextureBound(tex, screenPivot, texBound))
                {
                    texBound.minX = screenPivot.x - 1;
                    texBound.maxX = screenPivot.x + 1;
                    texBound.minY = screenPivot.y - 1;
                    texBound.maxY = screenPivot.y + 1;
                }

                TextureUtils.CalcTextureSymmetricBound(
                    false, setting.IsTopView(),
                    screenPivot, resolutionX, resolutionY,
                    texBound, exTexBound);

                stateMachine.ChangeState(SamplingState.EndFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                stateMachine.ChangeState(SamplingState.Finalize);
            }
        }

        public void OnEndFrame()
        {
            currFrameNumber++;

            if (currFrameNumber < frameSize)
                stateMachine.ChangeState(SamplingState.BeginFrame);
            else
                stateMachine.ChangeState(SamplingState.Finalize);
        }

        public void OnFinalize()
        {
#if UNITY_EDITOR
            EditorApplication.update -= UpdateState;
#endif

            try
            {
                stateMachine = null;

                TileUtils.UpdateTile(studio);

                setting.model.UpdateModel(Frame.begin);

                Camera.main.targetTexture = null;
                Camera.main.clearFlags = tmpCamClearFlags;
                Camera.main.backgroundColor = tmpCamBgColor;

                if (setting.IsStaticRealShadow())
                    studio.BakeStaticShadow();

                TrimAll();

                for (int i = 0; i < studio.samplings.Count; i++)
                    studio.selectedFrames.Add(new Frame(i, studio.samplings[i].time));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
#if UNITY_EDITOR
                EditorUtility.ClearProgressBar();
#endif

                if (OnEnd != null)
                    OnEnd();
            }
        }

        private void TrimAll()
        {
            try
            {
                int margin = 5;
                if (exTexBound.minX - margin >= 0)
                    exTexBound.minX -= margin;
                if (exTexBound.maxX + margin < resolutionX)
                    exTexBound.maxX += margin;
                if (exTexBound.minY - margin >= 0)
                    exTexBound.minY -= margin;
                if (exTexBound.maxY + margin < resolutionY)
                    exTexBound.maxY += margin;

                int trimWidth = exTexBound.maxX - exTexBound.minX + 1;
                int trimHeight = exTexBound.maxY - exTexBound.minY + 1;

                foreach (SamplingData sample in studio.samplings)
                {
                    Texture2D trimTex = new Texture2D(trimWidth, trimHeight, TextureFormat.ARGB32, false);
                    for (int y = 0; y < trimHeight; y++)
                    {
                        for (int x = 0; x < trimWidth; x++)
                        {
                            Color color = sample.tex.GetPixel(exTexBound.minX + x, exTexBound.minY + y);
                            trimTex.SetPixel(x, y, color);
                        }
                    }

                    sample.tex = TextureUtils.ScaleTexture(trimTex, trimWidth, trimHeight);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
