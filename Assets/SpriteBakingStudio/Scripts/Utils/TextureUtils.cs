using System;
using System.IO;
using UnityEngine;

namespace SBS
{
    public class TextureUtils
    {
        static public bool CalcTextureBound(Texture2D tex, ScreenPoint pivot, TextureBound bound)
        {
            bound.minX = int.MaxValue;
            bound.maxX = int.MinValue;
            bound.minY = int.MaxValue;
            bound.maxY = int.MinValue;

            bool validPixelExist = false;
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    float alpha = tex.GetPixel(x, y).a;
                    if (alpha != 0)
                    {
                        bound.minX = x;
                        validPixelExist = true;
                        goto ENDMINX;
                    }
                }
            }

        ENDMINX:
            if (!validPixelExist)
                return false;

            validPixelExist = false;  
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = bound.minX; x < tex.width; x++)
                {
                    float alpha = tex.GetPixel(x, y).a;
                    if (alpha != 0)
                    {
                        bound.minY = y;
                        validPixelExist = true;
                        goto ENDMINY;
                    }
                }
            }

        ENDMINY:
            if (!validPixelExist)
                return false;

            validPixelExist = false;
            for (int x = tex.width - 1; x >= bound.minX; x--)
            {
                for (int y = bound.minY; y < tex.height; y++)
                {
                    float alpha = tex.GetPixel(x, y).a;
                    if (alpha != 0)
                    {
                        bound.maxX = x;
                        validPixelExist = true;
                        goto ENDMAXX;
                    }
                }
            }

        ENDMAXX:
            if (!validPixelExist)
                return false;

            validPixelExist = false;
            for (int y = tex.height - 1; y >= bound.minY; y--)
            {
                for (int x = bound.minX; x <= bound.maxX; x++)
                {
                    float alpha = tex.GetPixel(x, y).a;
                    if (alpha != 0)
                    {
                        bound.maxY = y;
                        validPixelExist = true;
                        goto ENDMAXY;
                    }
                }
            }

        ENDMAXY:
            if (!validPixelExist)
                return false;

            pivot.x = Mathf.Clamp(pivot.x, bound.minX, bound.maxX);
            pivot.y = Mathf.Clamp(pivot.y, bound.minY, bound.maxY);

            return true;
        }

        static public void CalcTextureSymmetricBound(
            bool symmetricAroundPivot, bool verticalSymmectric,
            ScreenPoint pivot, int maxWidth, int maxHeight,
            TextureBound bound, TextureBound exBound)
        {
            if (pivot == null)
                return;

            if (symmetricAroundPivot)
            {
                int stt2pivot = pivot.x - bound.minX;
                int pivot2end = bound.maxX - pivot.x;
                if (stt2pivot > pivot2end)
                {
                    bound.maxX = pivot.x + stt2pivot;
                    if (bound.maxX >= maxWidth)
                        bound.maxX = maxWidth - 1;
                }
                else if (pivot2end > stt2pivot)
                {
                    bound.minX = pivot.x - pivot2end;
                    if (bound.minX < 0)
                        bound.minX = 0;
                }

                if (verticalSymmectric)
                {
                    stt2pivot = pivot.y - bound.minY;
                    pivot2end = bound.maxY - pivot.y;
                    if (stt2pivot > pivot2end)
                    {
                        bound.maxY = pivot.y + stt2pivot;
                        if (bound.maxY >= maxHeight)
                            bound.maxY = maxHeight - 1;
                    }
                    else if (pivot2end > stt2pivot)
                    {
                        bound.minY = pivot.y - pivot2end;
                        if (bound.minY < 0)
                            bound.minY = 0;
                    }
                }
            }

            exBound.minX = Mathf.Min(exBound.minX, bound.minX);
            exBound.maxX = Mathf.Max(exBound.maxX, bound.maxX);
            exBound.minY = Mathf.Min(exBound.minY, bound.minY);
            exBound.maxY = Mathf.Max(exBound.maxY, bound.maxY);
        }

        static public Texture2D TrimTexture(Texture2D tex, TextureBound bound)
        {
            if (tex == null)
                return Texture2D.whiteTexture;

            int newTexWidth = bound.maxX - bound.minX + 1;
            int newTexHeight = bound.maxY - bound.minY + 1;

            Texture2D trimmedTex = new Texture2D(newTexWidth, newTexHeight, TextureFormat.ARGB32, false);
            for (int y = 0; y < newTexHeight; y++)
            {
                for (int x = 0; x < newTexWidth; x++)
                {
                    Color color = tex.GetPixel(bound.minX + x, bound.minY + y);
                    trimmedTex.SetPixel(x, y, color);
                }
            }

            return trimmedTex;
        }

        static public void UpdatePivot(ScreenPoint pivot, Texture2D tex, TextureBound bound)
        {
            pivot.x -= bound.minX;
            pivot.x = Mathf.Clamp(pivot.x, 0, tex.width - 1);
            pivot.y -= bound.minY;
            pivot.y = Mathf.Clamp(pivot.y, 0, tex.height - 1);
        }

        static public Texture2D MoveTextureBy(Texture2D tex, int moveX, int moveY)
        {
            if (tex == null)
                return Texture2D.whiteTexture;

            int newTexWidth = tex.width + Math.Abs(moveX);
            int newTexHeight = tex.height + Math.Abs(moveY);

            Texture2D movedTex = new Texture2D(newTexWidth, newTexHeight, TextureFormat.ARGB32, false);
            for (int y = 0; y < newTexHeight; y++)
            {
                for (int x = 0; x < newTexWidth; x++)
                {
                    Color color = new Color(0f, 0f, 0f, 0f);

                    int refX = x - moveX, refY = y - moveY;
                    if (refX >= 0 && refY >= 0 && refX <= tex.width && refY <= tex.height)
                        color = tex.GetPixel(refX, refY);

                    movedTex.SetPixel(x, y, color);
                }
            }

            return movedTex;
        }

        static public Texture2D ScaleTexture(Texture2D source, int destWidth, int desetHeight)
        {
            Texture2D dest = new Texture2D(destWidth, desetHeight, source.format, true);
            Color[] pixels = dest.GetPixels(0);
            float incX = 1.0f / (float)destWidth;
            float incY = 1.0f / (float)desetHeight;
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = source.GetPixelBilinear(incX * ((float)i % destWidth), incY * ((float)Mathf.Floor(i / destWidth)));

            dest.SetPixels(pixels, 0);
            dest.Apply();

            return dest;
        }

        static public void SetPixels(Texture2D tex, Color color)
        {
            for (int y = 0; y < tex.height; ++y)
            {
                for (int x = 0; x < tex.width; ++x)
                {
                    tex.SetPixel(x, y, color);
                }
            }
        }

        static public void WriteTexture(Texture2D dest, Texture2D src, int startX, int startY)
        {
            for (int y = 0; y < src.height; ++y)
            {
                for (int x = 0; x < src.width; ++x)
                {
                    dest.SetPixel(startX + x, startY + y, src.GetPixel(x, y));
                }
            }
        }

        static public void SaveTexture(string dirPath, string fileName, Texture2D tex)
        {
            try
            {
                string filePath = Path.Combine(dirPath, fileName + ".png");
                byte[] bytes = tex.EncodeToPNG();
#if UNITY_WEBPLAYER
                Debug.Log("Don't set 'Build Setting > Platform' to WebPlayer!");
#else
                File.WriteAllBytes(filePath, bytes);
#endif
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
