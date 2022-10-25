using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCaptureSink : DependencyRoot
{
    public GstUnityImageGrabber _grabber;
    private CameraCapture[] cameraCaptures;

    public enum MergeDirection { Horizontal, Vertical };
    public MergeDirection mergeDirection;
    private bool _started;

    public int Width;
    public int Height;

    public TextureFormat Format { get; private set; }

    public Texture2D _tempTex;

    protected override void Start()
    {
        _grabber = new GstUnityImageGrabber();
        cameraCaptures = GetComponentsInChildren<CameraCapture>();

        StartCoroutine(UpdateRoutine());
    }

    private bool GetCameraImages(out List<Texture2D> textures)
    {
        if(cameraCaptures.Length == 0)
        {
            Debug.LogWarning("No CameraCaptures found.");
            
            textures = null;
            return false;
        }
        textures = new List<Texture2D>();
        for (int i = 0; i < cameraCaptures.Length; i++)
        {
            textures.Add(cameraCaptures[i]._tempTex);
        }

        return true;
    }

    private Texture2D MergeTextures(List<Texture2D> textures)
    {
        byte[] data = MergeTextures(textures, out int width, out int height, out TextureFormat format);
        var newT2D = new Texture2D(width, height, format, false);
        newT2D.LoadRawTextureData(data);
        return newT2D;
    }

    private byte[] MergeTextures(List<Texture2D> textures, out int width, out int height, out TextureFormat format)
    {
        width = 0; height = 0;
        format = default;

        int length = 0;
        List<byte[]> dataList = new List<byte[]>();
        for (int i = 0; i < textures.Count; i++)
        {
            var tex = textures[i];
            if (format != tex.format && format != default)
            {
                Debug.LogWarning("Merging textures have different formats.");
            }
            format = tex.format;

            //if(MergeDirection.Horizontal == )
            byte[] data = tex.GetRawTextureData();
            length += data.Length;

            width = Mathf.Max(width, tex.width);
            height += tex.height;
            
            dataList.Add(data);
        }

        byte[] mergedTexturesData = new byte[length];
        int offset = 0;
        for (int i = 0; i < dataList.Count; i++)
        {

            var data = dataList[i];
            Buffer.BlockCopy(data, 0, mergedTexturesData, offset, data.Length);
            offset += data.Length;
        }

        return mergedTexturesData;
    }

    IEnumerator UpdateRoutine()
    {
        while (true)
        {
            //yield return new WaitForSeconds((float)1 / FPS);
            yield return new WaitForEndOfFrame();

            if (GetCameraImages(out List<Texture2D> textures))
            {
                //byte[] mergedData = MergeTextures(textures,
                //    out int width, out int height,
                //    out TextureFormat format);
                Texture2D merged = textures[0];// MergeTextures(textures);
                _tempTex = merged;
                byte[] mergedData = merged.GetRawTextureData();

                if (!_started)
                {
                    _started = true;
                    Width = merged.width;
                    Height = merged.height;
                    Format = merged.format;

                    base.Start();
                }

                if (mergedData.Length > 0)
                {
                    _grabber.SetTexture2D(mergedData, Width, Height, Format);
                    _grabber.Update();
                }
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _grabber.Destroy();
    }

    void Update()
    {

    }
}
