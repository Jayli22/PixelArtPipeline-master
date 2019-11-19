using UnityEngine;

namespace SBS
{
    public class TileUtils
    {
        static public void UpdateTile(SpriteBakingStudio studio)
        {
            StudioSetting setting = studio.setting;

            if (!studio.isSamplingReady)
                return;

            GameObject quarterViewTilesObj = GameObject.Find(Global.TILES_OBJECT_NAME);
            if (quarterViewTilesObj == null)
                return;

            Transform squareTrsf = quarterViewTilesObj.transform.Find("Square");
            Transform hexagonTrsf = quarterViewTilesObj.transform.Find("Hexagon");
            if (squareTrsf == null || hexagonTrsf == null)
                return;

            GameObject squareTileObj = squareTrsf.gameObject, hexagonTileObj = hexagonTrsf.gameObject;

            if (!setting.model.IsTileAvailable() || !setting.view.showTile)
            {
                squareTileObj.SetActive(false);
                hexagonTileObj.SetActive(false);
                return;
            }

            if (setting.view.tileType == TileType.Square)
            {
                UpdateTile(setting.model, squareTileObj, setting.view.tileType, setting.view.initialDegree);
                hexagonTileObj.SetActive(false);
            }
            else if (setting.view.tileType == TileType.Hexagon)
            {
                UpdateTile(setting.model, hexagonTileObj, setting.view.tileType, setting.view.initialDegree);
                squareTileObj.SetActive(false);
            }
        }

        static private void UpdateTile(StudioModel model, GameObject tileObj, TileType gridType, float baseAngle = 0)
        {
            if (model == null)
                return;

            Renderer tileRndr = tileObj.GetComponent<Renderer>();
            if (tileRndr == null)
                return;

            tileObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            float animMaxLength = Mathf.Max(model.GetSize().x, model.GetSize().z);
            float tileLength = 0.0f;
            if (gridType == TileType.Square)
                tileLength = (tileRndr.bounds.size.x / 2.0f) * (1.0f / Mathf.Sqrt(2.0f)) * 2.0f;
            else if (gridType == TileType.Hexagon)
                tileLength = (tileRndr.bounds.size.x / 2.0f) * (Mathf.Sqrt(3.0f) / 2.0f) * 1.5f;

            float diffRatio = animMaxLength / tileLength;
            tileObj.transform.localScale = new Vector3
            (
                tileObj.transform.localScale.x * diffRatio,
                1.0f,
                tileObj.transform.localScale.z * diffRatio
            );

            tileObj.transform.rotation = Quaternion.identity;
            tileObj.transform.RotateAround(tileObj.transform.position, Vector3.up, baseAngle);

            tileObj.SetActive(true);
        }

        static public void HideAllTiles()
        {
            GameObject tilesObj = GameObject.Find(Global.TILES_OBJECT_NAME);
            if (tilesObj == null)
                return;

            Transform squareTrsf = tilesObj.transform.Find("Square");
            Transform hexagonTrsf = tilesObj.transform.Find("Hexagon");
            if (squareTrsf == null || hexagonTrsf == null)
                return;

            squareTrsf.gameObject.SetActive(false);
            hexagonTrsf.gameObject.SetActive(false);
        }
    }
}
