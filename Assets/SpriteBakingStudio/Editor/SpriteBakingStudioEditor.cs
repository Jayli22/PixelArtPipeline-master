using System.IO;
using UnityEngine;
using UnityEditor;
using UnityStandardAssets.ImageEffects;

namespace SBS
{
    [CustomEditor(typeof(SpriteBakingStudio))]
    public class StudioBakingStudioEditor : Editor
    {
        private SpriteBakingStudio studio = null;
        private StudioSetting setting = null;

        private bool variationExcludingShadowBackup = false;
        private bool shadowWithoutModel_ = false;

        private int currModelUnitDegree = 0;
        private int currModelDegree = 0;

        private Texture2D previewTexture = null;

        private bool IsCapturable()
        {
            return studio.isSamplingReady && !FrameSampler.GetInstance().IsSamplingNow() && !studio.IsBakingNow();
        }

        void OnEnable()
        {
            studio = (SpriteBakingStudio)target;

            if (studio.setting == null)
                studio.setting = new StudioSetting();
            setting = studio.setting;
        }

        void OnDisable()
        {
            EditorApplication.update -= studio.UpdateState;

            FrameSampler sampler = FrameSampler.GetInstance();
            if (sampler != null)
                EditorApplication.update -= sampler.UpdateState;
        }

        public override void OnInspectorGUI()
        {
            GUI.changed = false;

            if (studio == null)
                return;

            Undo.RecordObject(studio, "Sprite Baking Studio");

            studio.checkedViewSize = 0;
            studio.checkedViewNames.Clear();
            studio.checkedViewFuncs.Clear();

            studio.isSamplingReady = true;
            studio.isBakingReady = true;

#if UNITY_WEBPLAYER
            EditorGUILayout.HelpBox("Don't set 'Build Setting > Platform' to WebPlayer!", MessageType.Error);
            studio.isSamplingReady = false;
#endif

            EditorGUI.BeginChangeCheck(); // check any changes
            {
                EditorGUI.BeginChangeCheck();
                DrawModelFields();
                bool modelChanged = EditorGUI.EndChangeCheck();
                if (modelChanged)
                {
                    studio.samplings.Clear();
                    studio.selectedFrames.Clear();
                }
                
                EditorGUILayout.Space();

                DrawCameraFields();

                EditorGUILayout.Space();

                DrawLightFields();

                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                DrawViewFields();
                bool viewChanged = EditorGUI.EndChangeCheck();

                EditorGUILayout.Space();

                bool modelViewChanged = modelChanged || viewChanged;
                DrawShadowFieldsWithSpace(modelViewChanged);

                DrawExtractorFields();

                EditorGUILayout.Space();

                DrawVariationFields();

                EditorGUILayout.Space();

                DrawPreviewFields();

                EditorGUILayout.Space();

                DrawTextureResolutionFields();

                EditorGUILayout.Space();

                DrawSamplingFieldsWithSpace();
            }
            bool anyChanged = EditorGUI.EndChangeCheck();

            if (setting.preview.on)
            {
                if (IsCapturable() && (anyChanged || previewTexture == null))
                    UpdatePreviewTexture();
            }

            //---------------------------------- Output -----------------------------------

            DrawTrimFields();

            EditorGUILayout.Space();

            DrawOutputTypeFields();

            EditorGUILayout.Space();

            DrawOutputLocationFields();

            EditorGUILayout.Space();

            DrawBakingFields();
        }

        private void DrawModelFields()
        {
            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

            StudioModel prevModel = setting.model;
            setting.model = (StudioModel)EditorGUILayout.ObjectField("Model", setting.model, typeof(StudioModel), true);
            if (setting.model == null)
                setting.model = FindObjectOfType<StudioModel>();
            StudioModel currModel = setting.model;

            if (currModel == null)
            {
                EditorGUILayout.HelpBox("No model!", MessageType.Warning);
                studio.isSamplingReady = false;
            }
            else
            {
                if (currModel != prevModel)
                {
                    if (prevModel != null)
                        prevModel.gameObject.SetActive(false);
                    currModel.gameObject.SetActive(true);
                }

                if (currModel is StudioAnimatedModel)
                {
                    StudioAnimatedModel animatedModel = (StudioAnimatedModel)currModel;
                    animatedModel.animClip = (AnimationClip)EditorGUILayout.ObjectField("Animation", animatedModel.animClip, typeof(AnimationClip), true);
                }

                if (currModel is StudioSkinnedModel)
                    currModel.transform.position = Vector3.zero;

                if (setting.IsModelGroup())
                {
                    StaticModelGroup group = setting.GetModelGroup();
                    if (DrawingUtils.DrawMiddleButton("Refresh Sub Models"))
                    {
                        group.RefreshModels();
                        group.InitModelsPosition();
                    }
                    if (!FrameSampler.GetInstance().IsSamplingNow() && !studio.IsBakingNow())
                        group.InitModelsPosition();
                }

                if (!currModel.IsReady())
                {
                    EditorGUILayout.HelpBox("Target model not ready!", MessageType.Warning);
                    studio.isSamplingReady = false;
                }
            }

            GUILayout.EndVertical(); // HelpBox
        }

        private void DrawCameraFields()
        {
            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);
            
            if (Camera.main != null)
            {
                if (setting.model != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (Camera.main.orthographic)
                    {
                        Camera.main.orthographicSize = EditorGUILayout.FloatField("Camera Size", Camera.main.orthographicSize);

                        if (!(setting.model is StudioParticleModel))
                        {
                            if (DrawingUtils.DrawNarrowButton("Adjust Camera"))
                                TransformUtils.AdjustCamera(setting);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EdgeDetection edgeDetection = Camera.main.gameObject.GetComponent<EdgeDetection>();
                if (edgeDetection != null)
                    edgeDetection.enabled = EditorGUILayout.Toggle("Edge Detection", edgeDetection.enabled);

                Antialiasing antialiasing = Camera.main.gameObject.GetComponent<Antialiasing>();
                if (antialiasing != null)
                    antialiasing.enabled = EditorGUILayout.Toggle("Antialiasing", antialiasing.enabled);
            }
            else
            {
                EditorGUILayout.HelpBox("No main camera!", MessageType.Warning);
                studio.isSamplingReady = false;
            }

            GUILayout.EndVertical(); // HelpBox
        }

        private void DrawLightFields()
        {
            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

            setting.light.obj = (Light)EditorGUILayout.ObjectField("Main Camera", setting.light.obj, typeof(Light), true);
            if (setting.light.obj == null)
            {
                GameObject lightObj = GameObject.Find("Directional light");
                if (lightObj != null)
                    setting.light.obj = lightObj.GetComponent<Light>();
            }

            if (setting.light.obj != null)
            {
                EditorGUI.BeginChangeCheck();
                setting.light.followCamera = EditorGUILayout.Toggle("Follow Camera", setting.light.followCamera);
                if (EditorGUI.EndChangeCheck())
                {
                    if (setting.light.followCamera && Camera.main != null)
                    {
                        setting.light.obj.transform.position = Camera.main.transform.position;
                        setting.light.obj.transform.rotation = Camera.main.transform.rotation;
                    }
                }

                if (!setting.light.followCamera)
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginChangeCheck();
                    setting.light.obj.transform.position = EditorGUILayout.Vector3Field("Position", setting.light.obj.transform.position);
                    if (EditorGUI.EndChangeCheck() || DrawingUtils.DrawMiddleButton("Look At Model"))
                        TransformUtils.LookAtModel(setting.light.obj.transform, setting.model);
                    EditorGUI.indentLevel--;
                }
            }

            GUILayout.EndVertical(); // HelpBox
        }

        private void DrawViewFields()
        {
            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

            EditorGUI.BeginChangeCheck();
            setting.view.slopeAngle = EditorGUILayout.FloatField("Slope Angle (0~90)", setting.view.slopeAngle);
            setting.view.slopeAngle = Mathf.Clamp(setting.view.slopeAngle, 0f, 90f);
            bool slopeAngleChanged = EditorGUI.EndChangeCheck();

            if (setting.IsSideView() && slopeAngleChanged)
                setting.view.showTile = false;
            else
                DrawTileFields(ref slopeAngleChanged);

            if (slopeAngleChanged)
                TransformUtils.UpdateCamera(studio);

            EditorGUI.BeginChangeCheck();
            setting.view.size = EditorGUILayout.IntField("View Size", setting.view.size);
            bool viewSizeChanged = EditorGUI.EndChangeCheck();

            if (setting.view.size < 1)
                setting.view.size = 1;

            float unitDegree = 360f / setting.view.size;

            bool[] prevViewToggles = null;
            if (viewSizeChanged || setting.view.size != setting.view.toggles.Length)
            {
                prevViewToggles = (bool[])setting.view.toggles.Clone();
                setting.view.toggles = new bool[setting.view.size];
                MigrateViews(prevViewToggles);

                int integerUnitDegree = (int)unitDegree;
                if (currModelUnitDegree % integerUnitDegree != 0)
                    currModelDegree = 0;
                currModelUnitDegree = integerUnitDegree;
            }

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            setting.view.initialDegree = EditorGUILayout.FloatField(string.Format("Initial Degree (0~{0})", unitDegree), setting.view.initialDegree);
            bool baseAngleChanged = EditorGUI.EndChangeCheck();
            setting.view.initialDegree = Mathf.Clamp(setting.view.initialDegree, 0, unitDegree);

            for (int i = 0; i < setting.view.size; i++)
            {
                float degree = unitDegree * i;
                ModelRotationCallback callback = () =>
                {
#if UNITY_2018 || UNITY_2018_1_OR_NEWER
                    StudioAnimatedModel animatedModel = setting.model as StudioAnimatedModel;
                    if (animatedModel != null)
                        animatedModel.currDegree = setting.view.initialDegree + degree;
#endif
                    TransformUtils.RotateModel(setting.model, setting.view.initialDegree + degree);
                };

                if (MakeViewToggleAndButton(setting.view.initialDegree + degree, callback, ref setting.view.toggles[i]))
                {
                    currModelDegree = (int)degree;
                    callback();
                }
            }

            EditorGUI.indentLevel--;

            DrawViewSelectionButtons(setting.view.toggles);

            GUILayout.EndVertical(); // HelpBox

            if (viewSizeChanged || baseAngleChanged)
                TransformUtils.RotateModel(setting.model, setting.view.initialDegree + currModelDegree);

            CountCheckedViews(setting.view.toggles);

            if (studio.checkedViewSize == 0)
            {
                EditorGUILayout.HelpBox("No selected view!", MessageType.Warning);
                studio.isSamplingReady = false;
                studio.isBakingReady = false;
            }
        }

        private void DrawTileFields(ref bool slopeAngleChanged)
        {
            EditorGUI.BeginChangeCheck();
            setting.view.showTile = EditorGUILayout.Toggle("Show Tile", setting.view.showTile);
            bool tileShowingChanged = EditorGUI.EndChangeCheck();

            bool aspectRatioChanged = false;

            if (setting.view.showTile)
            {
                if (setting.IsSideView())
                {
                    if (tileShowingChanged)
                    {
                        setting.view.slopeAngle = 30;
                        slopeAngleChanged = true;
                    }
                }
                else
                {
                    EditorGUI.indentLevel++;

                    setting.view.tileType = (TileType)EditorGUILayout.EnumPopup("Tile Type", setting.view.tileType);

                    EditorGUI.BeginChangeCheck();
                    setting.view.tileAspectRatio = EditorGUILayout.Vector2Field("Aspect Ratio", setting.view.tileAspectRatio);
                    aspectRatioChanged = EditorGUI.EndChangeCheck();

                    if (setting.view.tileAspectRatio.x < 1f)
                        setting.view.tileAspectRatio.x = 1f;
                    if (setting.view.tileAspectRatio.y < 1f)
                        setting.view.tileAspectRatio.y = 1f;
                    if (setting.view.tileAspectRatio.x < setting.view.tileAspectRatio.y)
                        setting.view.tileAspectRatio.x = setting.view.tileAspectRatio.y;

                    EditorGUI.indentLevel--;
                }

                if (tileShowingChanged || slopeAngleChanged)
                {
                    setting.view.tileAspectRatio.x = setting.view.tileAspectRatio.y / Mathf.Sin(setting.view.slopeAngle * Mathf.Deg2Rad);
                }
                else if (aspectRatioChanged)
                {
                    setting.view.slopeAngle = Mathf.Asin(setting.view.tileAspectRatio.y / setting.view.tileAspectRatio.x) * Mathf.Rad2Deg;
                    slopeAngleChanged = true;
                }
            }
            
            if (IsCapturable())
            {
                if (setting.view.showTile)
                    CreateObject(Global.TILES_OBJECT_NAME, new Vector3(0f, -0.1f, 0f));
                else
                    DeleteObject(Global.TILES_OBJECT_NAME);

                TileUtils.UpdateTile(studio);
            }
        }

        private void MigrateViews(bool[] oldViewToggles)
        {
            int oldCheckCount = 0;
            for (int oldIndex = 0; oldIndex < oldViewToggles.Length; ++oldIndex)
            {
                if (oldViewToggles[oldIndex])
                {
                    float ratio = (float)oldIndex / oldViewToggles.Length;
                    int newIndex = Mathf.FloorToInt(setting.view.toggles.Length * ratio);
                    setting.view.toggles[newIndex] = true;
                    ++oldCheckCount;
                }
            }

            int newCheckCount = 0;
            foreach (bool toggle in setting.view.toggles)
            {
                if (toggle)
                    ++newCheckCount;
            }

            if (newCheckCount < oldCheckCount)
            {
                int difference = oldCheckCount - newCheckCount;
                for (int diffi = 0; diffi < difference; ++diffi)
                {
                    for (int viewi = 0; viewi < setting.view.toggles.Length; ++viewi)
                    {
                        if (!setting.view.toggles[viewi])
                        {
                            setting.view.toggles[viewi] = true;
                            break;
                        }
                    }
                }
            }
        }

        private void DrawViewSelectionButtons(bool[] toggles)
        {
            EditorGUILayout.BeginHorizontal();
            if (DrawingUtils.DrawNarrowButton("Select All"))
            {
                for (int i = 0; i < toggles.Length; i++)
                    toggles[i] = true;
            }
            if (DrawingUtils.DrawNarrowButton("Unselect All"))
            {
                for (int i = 0; i < toggles.Length; i++)
                    toggles[i] = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void CountCheckedViews(bool[] toggles)
        {
            int viewCount = 0;
            foreach (bool toggle in toggles)
            {
                if (toggle)
                    viewCount++;
            }
            studio.checkedViewSize = viewCount;
        }

        private bool MakeViewToggleAndButton(float degree, ModelRotationCallback callback, ref bool toggle)
        {
            string label = degree + "º";
            string viewName = degree + "Degree";
            viewName = viewName.Replace('.', 'p');

            bool applied = false;
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginChangeCheck();
                toggle = EditorGUILayout.Toggle(label, toggle);
                bool toggleChanged = EditorGUI.EndChangeCheck();

                if (toggle)
                {
                    studio.checkedViewNames.Add(viewName);
                    studio.checkedViewFuncs.Add(callback);
                }

                bool applyButtonClicked = DrawingUtils.DrawNarrowButton("Apply", 60);
                if (applyButtonClicked || (toggleChanged && toggle))
                    applied = true;
            }
            EditorGUILayout.EndHorizontal();

            return applied;
        }

        private void DrawShadowFieldsWithSpace(bool modelViewChanged)
        {
            if (!studio.isSamplingReady)
                return;

            if (Camera.main.transform.position.y <= 0f)
            {
                studio.isShadowReady = false;
                return;
            }
            if (Camera.main.transform.forward.y >= 0f)
            {
                studio.isShadowReady = false;
                return;
            }

            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

            EditorGUI.BeginChangeCheck();
            setting.shadow.type = (ShadowType)EditorGUILayout.EnumPopup("Shadow", setting.shadow.type);
            bool shadowTypeChanged = EditorGUI.EndChangeCheck();

            if (shadowTypeChanged)
            {
                if (setting.shadow.type != ShadowType.None)
                {
                    setting.shadow.shadowOnly = shadowWithoutModel_;
                    if (setting.variation.excludeShadow)
                    {
                        setting.shadow.shadowOnly = false;
                        shadowWithoutModel_ = false;
                    }
                }
                else
                {
                    shadowWithoutModel_ = setting.shadow.shadowOnly;
                    setting.shadow.shadowOnly = false;
                }
            }

            if (setting.shadow.type == ShadowType.Simple)
            {
                EditorGUI.indentLevel++;

                DeleteObjectUnder("Shadow", setting.model.transform); // old shadow object
                DeleteObject(Global.STATIC_SHADOW_NAME);
                DeleteObject(Global.DYNAMIC_SHADOW_NAME);

                EditorGUI.BeginChangeCheck();

                if (setting.model.simpleShadow.gameObject != null)
                {
                    if (setting.model.transform != setting.model.simpleShadow.gameObject.transform.parent)
                        setting.model.simpleShadow.gameObject = null;
                }
                if (setting.model.simpleShadow.gameObject == null)
                    setting.model.simpleShadow.gameObject = CreateObject(Global.SIMPLE_SHADOW_NAME, Vector3.zero, setting.model.transform);
                AttachDontApplyUniformShader(setting.model.simpleShadow.gameObject);

                EditorGUILayout.BeginHorizontal();
                {
                    Vector3 prevScale = setting.model.simpleShadow.scale;
                    setting.model.simpleShadow.scale = EditorGUILayout.Vector2Field("Scale", setting.model.simpleShadow.scale);

                    if (setting.model.simpleShadow.autoScale) GUI.enabled = false;
                    if (DrawingUtils.DrawNarrowButton("Unify", 50))
                    {
                        if (setting.model.simpleShadow.scale.y != prevScale.y)
                            setting.model.simpleShadow.scale.x = setting.model.simpleShadow.scale.y;
                        else
                            setting.model.simpleShadow.scale.y = setting.model.simpleShadow.scale.x;
                    }
                    if (setting.model.simpleShadow.autoScale) GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();

                if (setting.model is StudioSkinnedModel)
                {
                    EditorGUI.BeginChangeCheck();
                    setting.model.simpleShadow.autoScale = EditorGUILayout.Toggle("Auto Scale", setting.model.simpleShadow.autoScale);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Vector3 ratio = setting.model.GetRatioBetweenSizes();
                        Vector2 scale = setting.model.simpleShadow.scale;
                        if (setting.model.simpleShadow.autoScale)
                            setting.model.simpleShadow.scale = new Vector2(scale.x * ratio.x, scale.y * ratio.z);
                        else
                            setting.model.simpleShadow.scale = new Vector2(scale.x / ratio.x, scale.y / ratio.z);
                    }
                }

                DrawShadowOpacityField(setting.model.simpleShadow.gameObject);

                DrawShadowOnlyField();

                bool anyChanged = EditorGUI.EndChangeCheck();

                if (modelViewChanged || shadowTypeChanged || anyChanged)
                    setting.model.RescaleSimpleShadow();

                EditorGUI.indentLevel--;
            }
            else if (setting.shadow.type == ShadowType.Real)
            {
                EditorGUI.indentLevel++;
                setting.shadow.method = (RealShadowMethod)EditorGUILayout.EnumPopup("Method", setting.shadow.method);
                EditorGUI.indentLevel--;

                if (setting.shadow.method == RealShadowMethod.Dynamic)
                {
                    if (setting.model is StudioParticleModel)
                    {
                        EditorGUILayout.HelpBox("Dynamic method is not supported for ParticleSystem.", MessageType.Info);
                        GUILayout.EndVertical(); // HelpBox
                        studio.isSamplingReady = false;
                        return;
                    }

                    EditorGUI.indentLevel++;

                    DeleteObjectUnder(Global.SIMPLE_SHADOW_NAME, setting.model.transform);
                    DeleteObject(Global.STATIC_SHADOW_NAME);

                    SetupShadowCameraAndFields(Global.DYNAMIC_SHADOW_NAME);

                    DrawRealShadowThingsField();

                    DrawShadowOpacityField(setting.shadow.fieldObj);

                    DrawShadowOnlyField();

                    EditorGUI.indentLevel--;
                }
                else if (setting.shadow.method == RealShadowMethod.Static)
                {
                    EditorGUI.indentLevel++;

                    DeleteObjectUnder(Global.SIMPLE_SHADOW_NAME, setting.model.transform);
                    DeleteObject(Global.DYNAMIC_SHADOW_NAME);

                    SetupShadowCameraAndFields(Global.STATIC_SHADOW_NAME);

                    if (setting.shadow.fieldObj != null)
                        setting.shadow.fieldObj.SetActive(setting.shadow.staticShadowVisible);

                    DrawRealShadowThingsField();

                    DrawShadowOpacityField(setting.shadow.fieldObj);

                    DrawShadowOnlyField();

                    if (!setting.preview.on)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (DrawingUtils.DrawNarrowButton("Update"))
                        {
                            setting.shadow.staticShadowVisible = true;
                            studio.BakeStaticShadow();
                        }
                        if (DrawingUtils.DrawNarrowButton("Hide"))
                        {
                            setting.shadow.staticShadowVisible = false;
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                DeleteObjectUnder(Global.SIMPLE_SHADOW_NAME, setting.model.transform);
                DeleteObject(Global.STATIC_SHADOW_NAME);
                DeleteObject(Global.DYNAMIC_SHADOW_NAME);
                setting.shadow.shadowOnly = false;
            }

            GUILayout.EndVertical(); // HelpBox

            studio.isShadowReady = true;

            EditorGUILayout.Space();
        }

        private void SetupShadowCameraAndFields(string shadowName)
        {
            if (setting.shadow.camera == null || setting.shadow.fieldObj == null)
            {
                GameObject shadowObj = CreateObject(shadowName, Vector3.zero);
                if (shadowObj != null)
                {
                    Transform cameraTransform = shadowObj.transform.Find("Camera");
                    if (cameraTransform != null)
                    {
                        setting.shadow.camera = cameraTransform.gameObject.GetComponent<Camera>();
                        setting.shadow.camera.orthographicSize = Camera.main.orthographicSize;
                    }
                    
                    Transform fieldTransform = shadowObj.transform.Find("Field");
                    if (fieldTransform != null)
                        setting.shadow.fieldObj = fieldTransform.gameObject;

                    StudioUtility.UpdateShadowFieldSize(setting.shadow.camera, setting.shadow.fieldObj);

                    if (cameraTransform != null)
                    {
                        cameraTransform.position = setting.shadow.cameraPosition;
                        TransformUtils.LookAtModel(setting.shadow.camera.transform, setting.model);
                    }

                    if (fieldTransform != null)
                    {
                        fieldTransform.position = setting.shadow.fieldPosition;
                        fieldTransform.rotation = Quaternion.Euler(0f, setting.shadow.fieldRotation, 0f);
                    }   
                }
            }
        }

        private void DrawRealShadowThingsField()
        {
            if (setting.shadow.camera == null || setting.shadow.fieldObj == null)
                return;

            EditorGUI.indentLevel++;

            GUIStyle style = new GUIStyle("label");
            style.fontSize = 10;
            style.fontStyle = FontStyle.Italic;
            EditorGUILayout.LabelField("experimental", style);

            EditorGUI.BeginChangeCheck();
            setting.shadow.cameraPosition = EditorGUILayout.Vector3Field("Camera Position", setting.shadow.cameraPosition);
            bool cameraPositionChanged = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            setting.shadow.autoAdjustField = EditorGUILayout.Toggle("Auto Adjust Field", setting.shadow.autoAdjustField);
            bool autoAdjustingChanged = EditorGUI.EndChangeCheck();

            if ((cameraPositionChanged && setting.shadow.autoAdjustField) || (autoAdjustingChanged && setting.shadow.autoAdjustField))
            {
                setting.shadow.camera.transform.position = setting.shadow.cameraPosition;
                TransformUtils.LookAtModel(setting.shadow.camera.transform, setting.model);

                Vector3 dirToModel = setting.model.ComputedCenter - setting.shadow.cameraPosition;

                Plane plane = new Plane(Vector3.up, Vector3.zero);
                Ray ray = new Ray(setting.shadow.cameraPosition, dirToModel);
                float distance = 0;
                if (plane.Raycast(ray, out distance))
                    setting.shadow.fieldObj.transform.position = setting.shadow.fieldPosition = ray.GetPoint(distance) / 2;

                dirToModel.y = 0;
                setting.shadow.fieldRotation = Vector3.Angle(dirToModel, Vector3.forward);
                if (setting.shadow.cameraPosition.x > 0)
                    setting.shadow.fieldRotation *= -1;
                setting.shadow.fieldObj.transform.rotation = Quaternion.Euler(0f, setting.shadow.fieldRotation, 0f);
            }

            if (Mathf.Abs(setting.shadow.cameraPosition.x) > Mathf.Epsilon || Mathf.Abs(setting.shadow.cameraPosition.z) > Mathf.Epsilon)
            {
                EditorGUI.BeginChangeCheck();
                setting.shadow.fieldPosition = EditorGUILayout.Vector3Field("Field Position", setting.shadow.fieldPosition);
                if (EditorGUI.EndChangeCheck())
                    setting.shadow.fieldObj.transform.position = setting.shadow.fieldPosition;

                EditorGUI.BeginChangeCheck();
                setting.shadow.fieldRotation = EditorGUILayout.FloatField("Field Rotation", setting.shadow.fieldRotation);
                if (EditorGUI.EndChangeCheck())
                    setting.shadow.fieldObj.transform.rotation = Quaternion.Euler(0f, setting.shadow.fieldRotation, 0f);

                EditorGUI.BeginChangeCheck();
                setting.shadow.fieldScale = EditorGUILayout.Vector3Field("Field Scale", setting.shadow.fieldScale);
                if (EditorGUI.EndChangeCheck())
                    setting.shadow.fieldObj.transform.localScale = setting.shadow.fieldScale;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawShadowOpacityField(GameObject shadowObj)
        {
            if (shadowObj == null)
                return;

            Renderer renderer = shadowObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.sharedMaterial.color;
                float opacity = EditorGUILayout.Slider("Opacity", color.a, 0, 1);
                color.a = Mathf.Clamp01(opacity);
                renderer.sharedMaterial.color = color;
            }
        }

        private void DrawShadowOnlyField()
        {
            if (setting.variation.on && setting.variation.excludeShadow) GUI.enabled = false;
            setting.shadow.shadowOnly = EditorGUILayout.Toggle("Shadow Only", setting.shadow.shadowOnly);
            if (setting.variation.on && setting.variation.excludeShadow) GUI.enabled = true;
        }

        private void DrawExtractorFields()
        {
            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

            setting.extractor = (ExtractorBase)EditorGUILayout.ObjectField("Extractor", setting.extractor, typeof(ExtractorBase), true);
            if (setting.extractor == null)
            {
                string[] assetGuids = AssetDatabase.FindAssets(Global.DEFAULT_EXTRACTOR_NAME);
                foreach (string guid in assetGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prf = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                    if (prf != null)
                    {
                        setting.extractor = prf.GetComponent<DefaultExtractor>();
                        break;
                    }
                }
            }

            GUILayout.EndVertical(); // HelpBox
        }

        private void DrawVariationFields()
        {
            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

            EditorGUI.BeginChangeCheck();
            setting.variation.on = EditorGUILayout.Toggle("Variation", setting.variation.on);
            bool variationUsingChanged = EditorGUI.EndChangeCheck();

            if (variationUsingChanged)
            {
                if (setting.variation.on)
                {
                    setting.variation.excludeShadow = variationExcludingShadowBackup;
                    if (setting.shadow.shadowOnly)
                    {
                        setting.variation.excludeShadow = false;
                        variationExcludingShadowBackup = false;
                    }
                }
                else
                {
                    variationExcludingShadowBackup = setting.variation.excludeShadow;
                    setting.variation.excludeShadow = false;
                }
            }

            if (setting.variation.on)
            {
                EditorGUI.indentLevel++;

                setting.variation.tintColor = EditorGUILayout.ColorField("Tint Color", setting.variation.tintColor);
                setting.variation.tintBlendFactor = (BlendFactor)EditorGUILayout.EnumPopup("Tint Blend Factor", setting.variation.tintBlendFactor);
                setting.variation.imageBlendFactor = (BlendFactor)EditorGUILayout.EnumPopup("Image Blend Factor", setting.variation.imageBlendFactor);

                if (setting.shadow.type != ShadowType.None)
                {
                    if (setting.shadow.shadowOnly) GUI.enabled = false;
                    {
                        setting.variation.excludeShadow = EditorGUILayout.Toggle("Exclude Shadow", setting.variation.excludeShadow);

                        if (setting.variation.excludeShadow)
                        {
                            EditorGUI.indentLevel++;
                            setting.variation.bodyBlendFactor = (BlendFactor)EditorGUILayout.EnumPopup("Body Blend Factor", setting.variation.bodyBlendFactor);
                            setting.variation.shadowBlendFactor = (BlendFactor)EditorGUILayout.EnumPopup("Shadow Blend Factor", setting.variation.shadowBlendFactor);
                            EditorGUI.indentLevel--;
                        }
                    }
                    if (setting.shadow.shadowOnly) GUI.enabled = true;
                }

                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical(); // HelpBox
        }

        private void DrawPreviewFields()
        {
            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

            EditorGUI.BeginChangeCheck();
            setting.preview.on = EditorGUILayout.Toggle("Preview", setting.preview.on);
            bool previewChanged = EditorGUI.EndChangeCheck();

            if (setting.preview.on)
            {
                EditorGUI.indentLevel++;

                setting.preview.backgroundType = (PreviewBackgroundType)EditorGUILayout.EnumPopup("Background", setting.preview.backgroundType);
                if (setting.preview.backgroundType == PreviewBackgroundType.SingleColor)
                {
                    EditorGUI.indentLevel++;
                    setting.preview.backgroundColor = EditorGUILayout.ColorField("Color", setting.preview.backgroundColor);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;

                if (setting.preview.on)
                {
                    if (previewChanged || DrawingUtils.DrawMiddleButton("Update Preview"))
                        UpdatePreviewTexture();
                }

                if (setting.IsStaticRealShadow())
                    EditorGUILayout.HelpBox("When the real shadow method is Static, it slows down overall.", MessageType.Warning);
            }

            GUILayout.EndVertical(); // HelpBox
        }

        private void DrawTextureResolutionFields()
        {
            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

            setting.textureResolution = EditorGUILayout.Vector2Field("Texture Resolution", setting.textureResolution);
            if (setting.textureResolution.x < 1f || setting.textureResolution.y < 1f)
            {
                EditorGUILayout.HelpBox("Too small Texture Resolution!", MessageType.Warning);
                studio.isSamplingReady = false;
            }
            setting.textureResolution = new Vector2
            (
                Mathf.Round(setting.textureResolution.x),
                Mathf.Round(setting.textureResolution.y)
            );

            GUILayout.EndVertical(); // HelpBox
        }

        private void DrawSamplingFieldsWithSpace()
        {
            if (!studio.isSamplingReady)
                return;

            if (setting.model is StudioAnimatedModel || setting.model is StudioParticleModel || setting.IsModelGroup())
            {
                GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

                if (setting.model is StudioAnimatedModel || setting.model is StudioParticleModel)
                {
                    setting.frameSize = EditorGUILayout.IntField("Frame Size", setting.frameSize);
                    if (setting.frameSize < 1)
                        setting.frameSize = 1;
                }

                if (setting.model is StudioAnimatedModel && IsCapturable())
                {
                    bool simulatedFrameChanged = false;
                    if (setting.frameSize > 1)
                    {
                        EditorGUI.BeginChangeCheck();
                        string label = string.Format("Simulate (0~{0})", setting.frameSize - 1);
                        setting.simulatedFrame = EditorGUILayout.IntSlider(label, setting.simulatedFrame, 0, setting.frameSize - 1);
                        simulatedFrameChanged = EditorGUI.EndChangeCheck();
                    }
                    else
                    {
                        setting.simulatedFrame = 0;
                    }

                    if (simulatedFrameChanged)
                    {
                        float frameRatio = 0.0f;
                        if (setting.simulatedFrame > 0 && setting.simulatedFrame < setting.frameSize)
                            frameRatio = (float)setting.simulatedFrame / (float)(setting.frameSize - 1);

                        float frameTime = setting.model.GetTimeForRatio(frameRatio);
                        setting.model.UpdateModel(new Frame(setting.simulatedFrame, frameTime));
                    }
                }

                setting.delay = EditorGUILayout.DoubleField("Delay", setting.delay);
                if (setting.delay < 0.0)
                    setting.delay = 0.0;

                GUILayout.EndVertical(); // HelpBox
            }

            if (IsCapturable())
            {
                if (DrawingUtils.DrawWideButton("Sample"))
                {
                    if (studio.checkedViewFuncs.Count > 1)
                        studio.checkedViewFuncs[0]();
                    HideSelectorAndViewer();
                    FrameSampler sampler = FrameSampler.GetInstance();
                    sampler.OnEnd = ShowSelectorAndPreviewer;
                    sampler.SampleFrames(studio);
                }

                if (studio.samplings.Count > 0)
                {
                    string buttonText;
                    if (studio.selectedFrames.Count == 0)
                        buttonText = "Select Frames!";
                    else
                        buttonText = studio.selectedFrames.Count + " frame(s) selected.";

                    if (DrawingUtils.DrawWideButton(buttonText))
                        ShowSelectorAndPreviewer();
                }
            }

            EditorGUILayout.Space();
        }

        public void ShowSelectorAndPreviewer()
        {
            if (FrameSelector.instance != null || FramePreviewer.instance != null)
                return;

            FrameSelector selector = ScriptableWizard.DisplayWizard<FrameSelector>("Frame Selector");
            if (selector != null)
                selector.SetStudio(studio);

            FramePreviewer previewer = ScriptableWizard.DisplayWizard<FramePreviewer>("Frame Previewer");
            if (previewer != null)
                previewer.SetStudio(studio);
        }

        public void HideSelectorAndViewer()
        {
            if (FrameSelector.instance != null)
                FrameSelector.instance.Close();

            if (FramePreviewer.instance != null)
                FramePreviewer.instance.Close();
        }

        private void DrawTrimFields()
        {
            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

            setting.trim.on = EditorGUILayout.Toggle("Trim", setting.trim.on);
            if (setting.trim.on)
            {
                EditorGUI.indentLevel++;

                setting.trim.spriteMargin = EditorGUILayout.IntField("Sprite Margin", setting.trim.spriteMargin);

                setting.trim.useUnifiedSize = EditorGUILayout.Toggle("Unified Size", setting.trim.useUnifiedSize);
                if (setting.trim.useUnifiedSize)
                {
                    EditorGUI.indentLevel++;

                    setting.trim.pivotSymmetrically =
                        EditorGUILayout.Toggle("Pivot-Symmetric",
                        (!setting.IsSingleStaticModel() && setting.trim.allUnified) ? false : setting.trim.pivotSymmetrically);

                    if (!setting.IsSingleStaticModel())
                    {
                        setting.trim.allUnified =
                            EditorGUILayout.Toggle(setting.IsModelGroup() ? "for All Models" : "for All Views",
                            setting.trim.pivotSymmetrically ? false : setting.trim.allUnified);
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical(); // HelpBox
        }

        private void DrawOutputTypeFields()
        {
            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

            setting.output.type = (OutputType)EditorGUILayout.EnumPopup("Output Type", setting.output.type);

            if (setting.output.type == OutputType.SpriteSheet)
            {
                EditorGUI.indentLevel++;

                setting.output.algorithm = (PackingAlgorithm)EditorGUILayout.EnumPopup("Packing Algorithm", setting.output.algorithm);

                EditorGUI.indentLevel++;
                if (setting.output.algorithm == PackingAlgorithm.Optimized)
                    setting.output.atlasSizeIndex = EditorGUILayout.Popup("Max Size", setting.output.atlasSizeIndex, studio.atlasSizes);
                else if (setting.output.algorithm == PackingAlgorithm.InOrder)
                    setting.output.atlasSizeIndex = EditorGUILayout.Popup("Min Size", setting.output.atlasSizeIndex, studio.atlasSizes);
                EditorGUI.indentLevel--;

                setting.output.spritePadding = EditorGUILayout.IntField("Padding (0~5)", setting.output.spritePadding);
                setting.output.spritePadding = Mathf.Clamp(setting.output.spritePadding, 0, 5);

                if (setting.IsStaticModel())
                    EditorGUILayout.HelpBox("Can't make animation clips for StaticModel.", MessageType.Info);
                else
                    setting.output.makeAnimationClip = EditorGUILayout.Toggle("Animation Clip", setting.output.makeAnimationClip);

                if (setting.IsStaticModel())
                    setting.output.allInOneAtlas = EditorGUILayout.Toggle("All In One Atlas", setting.output.allInOneAtlas);

                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical(); // HelpBox
        }

        private void DrawOutputLocationFields()
        {
            GUILayout.BeginVertical(Global.HELP_BOX_STYLE);

            setting.autoFileNaming = EditorGUILayout.Toggle("Auto File Naming", setting.autoFileNaming);
            if (setting.autoFileNaming)
            {
                AutoMakeFileName();
                
                GUI.enabled = false;
                EditorGUILayout.TextField("File Name", setting.fileName);
                GUI.enabled = true;
            }
            else
            {
                setting.fileName = EditorGUILayout.TextField("File Name", setting.fileName);

                if (setting.fileName.Length == 0)
                {
                    EditorGUILayout.HelpBox("No file name!", MessageType.Warning);
                    studio.isBakingReady = false;
                }
            }

            setting.outputPath = EditorGUILayout.TextField("Output Directory", setting.outputPath);
            if (setting.outputPath == null || setting.outputPath.Length == 0 || !Directory.Exists(setting.outputPath))
            {
                EditorGUILayout.HelpBox("Wrong directory!", MessageType.Warning);
                studio.isBakingReady = false;
            }
            else
            {
                int assetIndex = setting.outputPath.IndexOf(Application.dataPath);
                if (assetIndex < 0)
                    EditorGUILayout.HelpBox("A directory should be in this project's Asset directory!", MessageType.Warning);
            }

            if (DrawingUtils.DrawMiddleButton("Choose Directory"))
                setting.outputPath = EditorUtility.SaveFolderPanel("Choose a directory", Application.dataPath, "spritesheets");

            GUILayout.EndVertical(); // HelpBox
        }

        private void AutoMakeFileName()
        {
            if (setting.model == null)
                return;

            setting.fileName = setting.model.name;

            if (setting.model is StudioAnimatedModel)
            {
                StudioAnimatedModel animatedModel = setting.model as StudioAnimatedModel;
                if (animatedModel.animClip != null)
                    setting.fileName += "_" + animatedModel.animClip.name;
            }
        }

        private void DrawBakingFields()
        {
            if (!IsCapturable() || !studio.isBakingReady)
                return;

            if ((studio.samplings.Count > 0 && studio.selectedFrames.Count > 0) || studio.samplings.Count == 0)
            {
                string postText = studio.selectedFrames.Count > 0 ? "selected frames" : "all frames";

                if (DrawingUtils.DrawWideButton("Bake " + postText))
                {
                    HideSelectorAndViewer();
                    studio.BakeSprites();
                }
            }
        }

        private GameObject CreateObject(string name, Vector3 position, Transform parent = null)
        {
            GameObject obj = GameObject.Find(name);

            if (obj == null)
            {
                GameObject prefab = null;
                
                string[] assetGuids = AssetDatabase.FindAssets(name);
                foreach (string guid in assetGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prf = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                    if (prf != null)
                    {
                        prefab = prf;
                        break;
                    }
                }

                if (prefab != null)
                {
                    obj = Instantiate(prefab, position, Quaternion.identity);
                    obj.name = name;

                    if (parent != null)
                    {
                        obj.transform.parent = parent;
                        obj.transform.localRotation = Quaternion.identity;
                    }
                }
            }

            return obj;
        }

        private void DeleteObject(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
                DestroyImmediate(obj);
        }

        private void DeleteObjectUnder(string name, Transform parent)
        {
            Transform child = parent.Find(name);
            if (child != null && child.gameObject != null)
                DestroyImmediate(child.gameObject);
        }

        private void AttachDontApplyUniformShader(GameObject obj)
        {
            Debug.Assert(obj.GetComponentsInChildren<Renderer>().Length == 1);

            Renderer renderer = obj.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                if (renderer.gameObject.GetComponent<DontApplyUniformShader>() == null)
                    renderer.gameObject.AddComponent<DontApplyUniformShader>();
            }
        }

        private void UpdatePreviewTexture()
        {
            Camera.main.targetTexture = new RenderTexture((int)setting.textureResolution.x, (int)setting.textureResolution.y, 24, RenderTextureFormat.ARGB32);
            CameraClearFlags tmpCamClearFlags = Camera.main.clearFlags;
            Color tmpCamBgColor = Camera.main.backgroundColor;

            TileUtils.HideAllTiles();

            previewTexture = StudioUtility.PrepareShadowAndExtractTexture(studio);

            TileUtils.UpdateTile(studio);

            Camera.main.targetTexture = null;
            Camera.main.clearFlags = tmpCamClearFlags;
            Camera.main.backgroundColor = tmpCamBgColor;
        }

        public override bool HasPreviewGUI()
        {
            return setting != null ? setting.preview.on : false;
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("Preview");
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (rect.width <= 1 || rect.height <= 1)
                return;

            if (previewTexture == null)
                return;

            Rect scaledRect = ScalePreviewRect(rect);
            Texture2D scaledTex = TextureUtils.ScaleTexture(previewTexture, (int)scaledRect.width, (int)scaledRect.height);

            if (setting.preview.backgroundType == PreviewBackgroundType.Checker)
            {
                EditorGUI.DrawTextureTransparent(scaledRect, scaledTex);
            }
            else if (setting.preview.backgroundType == PreviewBackgroundType.SingleColor)
            {
                EditorGUI.DrawRect(scaledRect, setting.preview.backgroundColor);
                GUI.DrawTexture(scaledRect, scaledTex);
            }
        }

        private Rect ScalePreviewRect(Rect rect)
        {
            float aspectRatio = (float)previewTexture.width / (float)previewTexture.height;

            float widthScaleRatio = previewTexture.width / rect.width;
            float heightScaleRatio = previewTexture.height / rect.height;

            float scaledWidth = rect.width, scaledHeight = rect.height;

            if (previewTexture.width > rect.width && previewTexture.height > rect.height)
            {
                if (widthScaleRatio < heightScaleRatio)
                    ScaleByHeight(rect.height, aspectRatio, out scaledWidth, out scaledHeight);
                else
                    ScaleByWidth(rect.width, aspectRatio, out scaledWidth, out scaledHeight);
            }
            else if (previewTexture.width > rect.width && previewTexture.height < rect.height)
            {
                ScaleByHeight(rect.height, aspectRatio, out scaledWidth, out scaledHeight);
                ScaleMoreByWidthIfOver(rect.width, ref scaledWidth, ref scaledHeight);
            }
            else if (previewTexture.width < rect.width && previewTexture.height > rect.height)
            {
                ScaleByWidth(rect.width, aspectRatio, out scaledWidth, out scaledHeight);
                ScaleMoreByHeightIfOver(rect.height, ref scaledWidth, ref scaledHeight);
            }
            else
            {
                if (widthScaleRatio < heightScaleRatio)
                {
                    ScaleByHeight(rect.height, aspectRatio, out scaledWidth, out scaledHeight);
                    ScaleMoreByWidthIfOver(rect.width, ref scaledWidth, ref scaledHeight);
                }
                else
                {
                    ScaleByWidth(rect.width, aspectRatio, out scaledWidth, out scaledHeight);
                    ScaleMoreByHeightIfOver(rect.height, ref scaledWidth, ref scaledHeight);
                }
            }

            float scaledX = rect.x + (rect.width - scaledWidth) / 2;
            float scaledY = rect.y + (rect.height - scaledHeight) / 2;

            return new Rect(scaledX, scaledY, scaledWidth, scaledHeight);
        }

        private void ScaleByHeight(float height, float aspectRatio, out float outWidth, out float outHeight)
        {
            outWidth = height * aspectRatio;
            outHeight = height;
        }

        private void ScaleByWidth(float width, float aspectRatio, out float outWidth, out float outHeight)
        {
            outWidth = width;
            outHeight = width / aspectRatio;
        }

        private void ScaleMoreByWidthIfOver(float width, ref float scaledWidth, ref float scaledHeight)
        {
            if (scaledWidth > width)
            {
                float scale = scaledWidth / width;
                scaledWidth /= scale;
                scaledHeight /= scale;
            }
        }

        private void ScaleMoreByHeightIfOver(float height, ref float scaledWidth, ref float scaledHeight)
        {
            if (scaledHeight > height)
            {
                float scale = scaledHeight / height;
                scaledWidth /= scale;
                scaledHeight /= scale;
            }
        }
    }
}
