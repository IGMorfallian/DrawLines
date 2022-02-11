using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Curve : MonoBehaviour
{
    public event Action OnCurveUpdate;

    private const int MinimalLineLength = 50;

    [SerializeField] private RawImage _rawImage;
    [SerializeField] private ComputeShader _shader;
    [SerializeField] private EventSystem _eventSystem;

    GraphicRaycaster _raycaster;
    PointerEventData _pointerEventData;

    private Vector4[] _textureBuffer;
    private ComputeBuffer _computeBuffer;

    private Texture2D _targetTexture;

    private Vector3 _previousMousePosition;

    private readonly List<Vector2> _curvePoints = new List<Vector2>();
    private readonly List<bool> _alreadyDrawnPoints = new List<bool>();

    public Vector3 GetScreenPositionFromNormalizedValue(float value)
    {
        var allCurveLength = GetCurveLength();
        var curveLengthBeforeValue = value * allCurveLength;

        var screenPosition = GetScreenPositionByLength(curveLengthBeforeValue);

        return screenPosition;
    }

    private float GetCurveLength()
    {
        float currentLength = 0;

        for (var i = 0; i < _curvePoints.Count - 1; i++)
        {
            currentLength += Vector2.Distance(_curvePoints[i], _curvePoints[i + 1]);
        }

        return currentLength;
    }

    private Vector2 GetScreenPositionByLength(float length)
    {
        Vector2 screenPosition = default;
        float currentLength = 0;

        for (var i = 1; i < _curvePoints.Count; i++)
        {
            currentLength += Vector2.Distance(_curvePoints[i], _curvePoints[i - 1]);

            if (currentLength >= length)
            {
                var directionToTarget = _curvePoints[i - 1] - _curvePoints[i];
                var distanceBetweenTargetAndPreviousPoints = currentLength - length;

                screenPosition = _curvePoints[i] + directionToTarget.normalized * distanceBetweenTargetAndPreviousPoints;
                break;
            }
        }

        return screenPosition;
    }


    private void Awake()
    {
        _raycaster = GetComponent<GraphicRaycaster>();

        _computeBuffer = new ComputeBuffer(512 * 512, sizeof(float) * 4, ComputeBufferType.Structured);
        _targetTexture = new Texture2D(512, 512);
        _rawImage.texture = _targetTexture;

        ClearTexture();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _curvePoints.Clear();
            _previousMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            if (!OverTheDrawPanel()) return;

            DrawCurve();
        }

        if (Input.GetMouseButtonUp(0))
        {
            ClearTexture();
        }
    }

    private bool OverTheDrawPanel()
    {
        _pointerEventData = new PointerEventData(_eventSystem);
        _pointerEventData.position = Input.mousePosition;

        var raycastResults = new List<RaycastResult>();

        _raycaster.Raycast(_pointerEventData, raycastResults);

        foreach (var raycastResult in raycastResults)
        {
            if (raycastResult.gameObject.CompareTag("Panel"))
            {
                return true;
            }
        }

        return false;
    }

    private void DrawCurve()
    {
        var distanceVector = _previousMousePosition - Input.mousePosition;
        if (distanceVector.magnitude > MinimalLineLength)
        {
            _curvePoints.Add(new Vector2(_previousMousePosition.x, _previousMousePosition.y));
            _alreadyDrawnPoints.Add(false);
            
            _curvePoints.Add(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
            _alreadyDrawnPoints.Add(false);
            
            _previousMousePosition = Input.mousePosition;
            
            UpdateCurve();
            OnCurveUpdate?.Invoke();
        }
    }

    private void ClearTexture()
    {
        _textureBuffer = new Vector4[512 * 512];
        _alreadyDrawnPoints.Clear();

        int kernelHandle = _shader.FindKernel("Clear");
        RenderTexture texture = new RenderTexture(512, 512, 12);
        texture.enableRandomWrite = true;
        texture.Create();

        _shader.SetTexture(kernelHandle, "Result", texture);
        _shader.Dispatch(kernelHandle, 512 / 8, 512 / 8, 1);

        RenderTexture.active = texture;
        _targetTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        _targetTexture.Apply();
    }

    private void UpdateCurve()
    {
        for (var i = 0; i < _curvePoints.Count - 1; i++)
        {
            var point1 = ToLocalCoordinates(_curvePoints[i]);
            var point2 = ToLocalCoordinates(_curvePoints[i + 1]);

            if (!_alreadyDrawnPoints[i])
            {
                BresenhamLine(point1.x, point1.y, point2.x, point2.y);
                _alreadyDrawnPoints[i] = true;
            }
        }

        ToTargetTexture();
    }

    private void ToTargetTexture()
    {
        int kernelHandle = _shader.FindKernel("CSMain");
        RenderTexture texture = new RenderTexture(512, 512, 12);
        texture.enableRandomWrite = true;
        texture.Create();

        _computeBuffer.SetData(_textureBuffer);
        _shader.SetInt("Stride", 512);
        _shader.SetBuffer(kernelHandle, "ImageInput", _computeBuffer);
        _shader.SetTexture(kernelHandle, "Result", texture);
        _shader.Dispatch(kernelHandle, 512 / 8, 512 / 8, 1);

        RenderTexture.active = texture;
        _targetTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        _targetTexture.Apply();
    }

    private Vector2Int ToLocalCoordinates(Vector2 point)
    {
        var width = _rawImage.rectTransform.sizeDelta.x;
        var height = _rawImage.rectTransform.sizeDelta.y;

        return new Vector2Int((int) (point.x / width * 512), (int) (point.y / height * 512));
    }

    void BresenhamLine(int x0, int y0, int x1, int y1)
    {
        var steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
        if (steep)
        {
            Swap(ref x0, ref y0);
            Swap(ref x1, ref y1);
        }

        if (x0 > x1)
        {
            Swap(ref x0, ref x1);
            Swap(ref y0, ref y1);
        }

        int dx = x1 - x0;
        int dy = Math.Abs(y1 - y0);
        int error = dx / 2;
        int ystep = (y0 < y1) ? 1 : -1;
        int y = y0;
        for (int x = x0; x <= x1; x++)
        {
            DrawDarkPoint(steep, x, y);
            error -= dy;
            if (error < 0)
            {
                y += ystep;
                error += dx;
            }
        }
    }

    void Swap<T>(ref T lhs, ref T rhs)
    {
        (lhs, rhs) = (rhs, lhs);
    }

    void DrawDarkPoint(bool steep, int x, int y)
    {
        if (!steep)
        {
            DrawFilledCircle(x, y, 8);
        }
        else
        {
            DrawFilledCircle(y, x, 8);
        }
    }

    void DrawFilledCircle(int x0, int y0, int radius)
    {
        int x = radius;
        int y = 0;
        int xChange = 1 - (radius << 1);
        int yChange = 0;
        int radiusError = 0;

        while (x >= y)
        {
            for (int i = x0 - x; i <= x0 + x; i++)
            {
                SetPixel(i, y0 + y);
                SetPixel(i, y0 - y);
            }

            for (int i = x0 - y; i <= x0 + y; i++)
            {
                SetPixel(i, y0 + x);
                SetPixel(i, y0 - x);
            }

            y++;
            radiusError += yChange;
            yChange += 2;
            if (((radiusError << 1) + xChange) > 0)
            {
                x--;
                radiusError += xChange;
                xChange += 2;
            }
        }
    }

    public void SetPixel(int x, int y)
    {
        var index = x + y * 512;
        if (index > 0 && index < _textureBuffer.Length)
        {
            _textureBuffer[index] = Vector4.one;
        }
    }
}