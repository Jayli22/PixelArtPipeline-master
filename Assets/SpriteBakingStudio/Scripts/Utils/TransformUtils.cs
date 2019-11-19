﻿using System;
using UnityEngine;

namespace SBS
{
    public class TransformUtils
    {
        static public void UpdateCamera(SpriteBakingStudio studio)
        {
            StudioSetting setting = studio.setting;

            if (Camera.main == null || setting.model == null)
                return;

            if (!studio.isSamplingReady)
                return;

            float dist = setting.model.GetSize().magnitude * 2.0f;
            Vector3 dirVec = new Vector3(0, Mathf.Sin(setting.view.slopeAngle * Mathf.Deg2Rad), -Mathf.Cos(setting.view.slopeAngle * Mathf.Deg2Rad));
            Camera.main.transform.position = setting.model.ComputedCenter + dirVec.normalized * dist;

            if (setting.light.obj != null && setting.light.followCamera)
                setting.light.obj.transform.position = Camera.main.transform.position;

            RotateCameraToModel(setting);
        }

        static private void RotateCameraToModel(StudioSetting setting)
        {
            if (Camera.main == null || setting.model == null)
                return;

            Vector3 cameraToModel = setting.model.ComputedCenter - Camera.main.transform.position;
            Camera.main.transform.rotation = Quaternion.LookRotation(cameraToModel.normalized);

            if (setting.light.obj != null && setting.light.followCamera)
                setting.light.obj.transform.rotation = Camera.main.transform.rotation;
        }

        static public void LookAtModel(Transform transf, StudioModel model)
        {
            if (Camera.main == null || model == null)
                return;

            Vector3 dirToModel = model.ComputedCenter - transf.position;
            //transf.rotation = Quaternion.LookRotation(dirToModel.normalized, transf.position.z <= 0 ? Vector3.up : Vector3.down);
            Vector3 rightDir = Vector3.Cross(dirToModel, Vector3.up);
            transf.rotation = Quaternion.LookRotation(dirToModel.normalized, Vector3.Cross(dirToModel, rightDir));
        }

        static public void AdjustCamera(StudioSetting setting)
        {
            if (setting.model == null)
                return;

            if (Camera.main == null || !Camera.main.orthographic)
                return;

            Camera.main.orthographicSize = 1;

            float cameraWidth = Camera.main.pixelWidth;
            float cameraHeight = Camera.main.pixelHeight;

            Vector2 modelScreenMinPos = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 modelScreenMaxPos = new Vector2(float.MinValue, float.MinValue);

            Vector3 modelWorldMinPos = setting.model.GetMinPos();
            Vector3 modelWorldMaxPos = setting.model.GetMaxPos();
            WorldToScreenMinMaxPoints(modelWorldMinPos, modelWorldMaxPos, ref modelScreenMinPos, ref modelScreenMaxPos);

            float modelScreenWidth = modelScreenMaxPos.x - modelScreenMinPos.x;
            float modelScreenHeight = modelScreenMaxPos.y - modelScreenMinPos.y;

            if (modelScreenMinPos.x < 0f)
                modelScreenWidth += Math.Abs(modelScreenMinPos.x) * 2f;
            if (modelScreenMaxPos.x > cameraWidth)
                modelScreenWidth += (modelScreenMaxPos.x - cameraWidth) * 2f;
            if (modelScreenMinPos.y < 0f)
                modelScreenHeight += Math.Abs(modelScreenMinPos.y) * 2f;
            if (modelScreenMaxPos.y > cameraHeight)
                modelScreenHeight += (modelScreenMaxPos.y - cameraHeight) * 2f;

            if (modelScreenWidth <= 0.0f && modelScreenHeight <= 0.0f)
            {
                Debug.Assert(false, "modelScreenWidth <= 0.0f && modelScreenHeight <= 0.0f");
                return;
            }

            if (modelScreenWidth > cameraWidth || modelScreenHeight > cameraHeight)
            {
                float widthScaleRatio = modelScreenWidth / cameraWidth;
                float heightScaleRatio = modelScreenHeight / cameraHeight;
                float maxScaleRatio = Mathf.Max(widthScaleRatio, heightScaleRatio);
                Camera.main.orthographicSize *= maxScaleRatio;
            }
            else if (modelScreenWidth < cameraWidth && modelScreenHeight < cameraHeight)
            {
                float widthScaleRatio = cameraWidth / modelScreenWidth;
                float heightScaleRatio = cameraHeight / modelScreenHeight;
                float minScaleRatio = Mathf.Min(widthScaleRatio, heightScaleRatio);
                Camera.main.orthographicSize /= minScaleRatio;
            }
        }

        static private void WorldToScreenMinMaxPoints(Vector3 worldMinPos, Vector3 worldMaxPos, ref Vector2 screenMinPos, ref Vector2 screenMaxPos)
        {
            Vector3[] worldPositions = new Vector3[8];
            worldPositions[0] = new Vector3(worldMinPos.x, worldMinPos.y, worldMinPos.z);
            worldPositions[1] = new Vector3(worldMinPos.x, worldMaxPos.y, worldMinPos.z);
            worldPositions[2] = new Vector3(worldMinPos.x, worldMinPos.y, worldMaxPos.z);
            worldPositions[3] = new Vector3(worldMinPos.x, worldMaxPos.y, worldMaxPos.z);
            worldPositions[4] = new Vector3(worldMaxPos.x, worldMinPos.y, worldMinPos.z);
            worldPositions[5] = new Vector3(worldMaxPos.x, worldMaxPos.y, worldMinPos.z);
            worldPositions[6] = new Vector3(worldMaxPos.x, worldMinPos.y, worldMaxPos.z);
            worldPositions[7] = new Vector3(worldMaxPos.x, worldMaxPos.y, worldMaxPos.z);

            foreach (Vector3 worldPos in worldPositions)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                screenMinPos.x = Mathf.Min(screenMinPos.x, screenPos.x);
                screenMaxPos.x = Mathf.Max(screenMaxPos.x, screenPos.x);
                screenMinPos.y = Mathf.Min(screenMinPos.y, screenPos.y);
                screenMaxPos.y = Mathf.Max(screenMaxPos.y, screenPos.y);
            }
        }

        static public void RotateModel(StudioModel model, float angle)
        {
            if (model == null)
                return;

            model.transform.localRotation = Quaternion.identity;
            float angleDiff = Vector3.Angle(model.ComputedForward, model.transform.forward);
            if (model.forwordType == StudioModel.ForwardType.PositiveX)
                angleDiff *= -1.0f;
            model.transform.localRotation = Quaternion.Euler(new Vector3(0, angleDiff + angle, 0));
        }
    }
}
