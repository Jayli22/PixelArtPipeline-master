using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SBS
{
    [Serializable]
    public class StaticModelPair
    {
        public StudioStaticModel Model { get; set; }
        public bool Checking { get; set; }

        public StaticModelPair(StudioStaticModel model_, bool check_)
        {
            Model = model_;
            Checking = check_;
        }
    }
    
    public class StaticModelGroup : StudioModel
    {
        [SerializeField]
        public List<StaticModelPair> modelPairs = new List<StaticModelPair>();

        public string rootDirectory;

        private StudioStaticModel biggestModel = null;

        public override Vector3 GetSize()
        {
            if (biggestModel == null)
                RefreshBiggestModel();
            return biggestModel != null ? biggestModel.GetSize() : Vector3.one;
        }

        public override Vector3 GetMinPos()
        {
            if (biggestModel == null)
                RefreshBiggestModel();
            return biggestModel != null ? biggestModel.GetMinPos() : Vector3.zero;
        }

        public override Vector3 GetMaxPos()
        {
            if (biggestModel == null)
                RefreshBiggestModel();
            return biggestModel != null ? biggestModel.GetMaxPos() : Vector3.zero;
        }

        public override float GetTimeForRatio(float ratio) { return 0f; }

        public override void UpdateModel(Frame frame) { }

        public override bool IsReady()
        {
            bool ready = false;
            foreach (StaticModelPair pair in modelPairs)
            {
                if (!pair.Checking)
                    continue;
                if (!pair.Model.IsReady())
                    return false;
                ready = true;
            }

            return ready;
        }

        public override bool IsTileAvailable()
        {
            return true;
        }

        public void InitModelsPosition()
        {
            foreach (StaticModelPair pair in modelPairs)
                pair.Model.transform.position = Vector3.zero;
        }

        public void RefreshModels()
        {
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform transf = transform.GetChild(i);
                if (transf != null)
                    DestroyImmediate(transf.gameObject);
            }

            modelPairs.Clear();
            biggestModel = null;

#if UNITY_EDITOR
            int assetIndex = rootDirectory.IndexOf("Assets");
            string assetDirectory = rootDirectory.Substring(assetIndex, rootDirectory.Length - assetIndex);

            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (string assetPath in allAssetPaths)
            {
                if (assetPath.IndexOf(assetDirectory) < 0)
                    continue;

                GameObject prf = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
                if (prf == null)
                    continue;

                if (modelPairs.Find(x => x.Model.name == prf.name) != null)
                    continue;

                GameObject obj = null;

                Transform transf = transform.Find(prf.name);
                if (transf == null)
                {
                    obj = Instantiate(prf, Vector3.zero, Quaternion.identity);
                    obj.name = prf.name;
                }
                else
                {
                    obj = transf.gameObject;
                }

                obj.transform.parent = transform;
                obj.transform.localRotation = Quaternion.identity;

                StudioStaticModel model = obj.GetComponent<StudioStaticModel>();
                if (model == null)
                    model = obj.AddComponent<StudioStaticModel>();

                model.AutoFindMeshRenderer();

                modelPairs.Add(new StaticModelPair(model, true));
            }
#endif
        }

        private void RefreshBiggestModel()
        {
            Vector3 maxSize = Vector3.one;
            foreach (StaticModelPair pair in modelPairs)
            {
                Vector3 size = pair.Model.GetSize();
                if (size.sqrMagnitude > maxSize.sqrMagnitude)
                {
                    maxSize = size;
                    biggestModel = pair.Model;
                }
                pair.Model.gameObject.SetActive(false);
            }

            if (biggestModel != null)
                biggestModel.gameObject.SetActive(true);
        }

        public List<StudioModel> GetCheckedModels()
        {
            List<StudioModel> checkedModels = new List<StudioModel>();
            foreach (StaticModelPair pair in modelPairs)
            {
                if (pair.Checking)
                    checkedModels.Add(pair.Model);
            }

            return checkedModels;
        }

        public void InactivateAllModels()
        {
            foreach (StaticModelPair pair in modelPairs)
                pair.Model.gameObject.SetActive(false);
        }

        public void ActivateBiggestModel()
        {
            if (biggestModel == null)
                RefreshBiggestModel();
            if (biggestModel != null)
                biggestModel.gameObject.SetActive(true);
        }
    }
}
