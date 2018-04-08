using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MobileShadow : MonoBehaviour {
    public Transform root;
    public MeshRenderer plane;
    public Transform direction;
    public Material drawingMaterial;
    public LayerMask layers;
    public float offset;
    public float size;

    protected CommandBuffer command;
    protected RenderTexture texture;
    protected Camera camera;
    protected Vector3 prevEuler;
    protected Renderer[] casters;

    void Awake () {
        texture = new RenderTexture(512, 512, 0);
        command = new CommandBuffer();
        camera = Camera.main;

        List<Renderer> renderers = new List<Renderer>();
        renderers.AddRange(root.GetComponentsInChildren<Renderer>(true));

        for (int i = renderers.Count - 1; i >= 0; i--)
        {
            if(layers != (layers | (1 << renderers[i].gameObject.layer)))
            {
                renderers.RemoveAt(i);
            }
        }

        casters = renderers.ToArray();

        plane.material.mainTexture = texture;
    }

    private void OnEnable()
    {
        Camera.onPostRender += OnCamPostRender;
        Camera.onPreRender += OnCamPreRender;

        camera.AddCommandBuffer(CameraEvent.AfterEverything, command);
    }

    private void OnDisable()
    {
        Camera.onPostRender -= OnCamPostRender;
        Camera.onPreRender -= OnCamPreRender;

        camera.RemoveCommandBuffer(CameraEvent.AfterEverything, command);
    }

    private void OnCamPreRender(Camera cam)
    {
        //if (casters.Length <= 0 || cam != camera)
        //    return;

        SetupTransform();
        SetupPlaneTransform();
    }

    private void OnCamPostRender(Camera cam)
    {
        //if (casters.Length <= 0 || cam != camera)
        //    return;

        SetupCommand();
    }

    void SetupCommand()
    {
        Matrix4x4 proj = Matrix4x4.Ortho(-size, size, -size, size, 0, 100);
        Matrix4x4 view = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one); //no scale...

        view = Matrix4x4.Inverse(view);
        view.m20 *= -1f;
        view.m21 *= -1f;
        view.m22 *= -1f;
        view.m23 *= -1f;

        command.Clear();

        command.SetRenderTarget(texture);
        command.SetViewProjectionMatrices(view, proj);

        command.ClearRenderTarget(false, true, Color.clear);
        foreach (var item in casters)
        {
            if (item.gameObject.activeInHierarchy)
            {
                command.DrawRenderer(item, drawingMaterial);
            }
        }

    }

    void SetupTransform()
    {
        transform.eulerAngles = new Vector3(direction.eulerAngles.x, direction.eulerAngles.y, 0);
        transform.position = root.position - direction.forward * offset;
    }


    void SetupPlaneTransform()
    {
        Vector3[] points = new Vector3[]
        {
            transform.position + transform.up * size + transform.right * size,
            transform.position + transform.up * size - transform.right * size,
            transform.position - transform.up * size + transform.right * size,
        };
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = GetZeroPlaneProjectedVector(points[i], transform.forward);
        }

        Vector3 scale = new Vector3((points[0] - points[1]).magnitude, (points[0] - points[2]).magnitude, 1);
        SetGlobalScale(plane.transform, scale);
        plane.transform.localScale = scale;
        plane.transform.position = (points[1] + points[2]) * 0.5f;
        plane.transform.eulerAngles = new Vector3(90, direction.transform.eulerAngles.y, 0);
    }

    void SetGlobalScale(Transform transform, Vector3 scale)
    {
        transform.localScale = Vector3.one;

        Vector3 disp = transform.lossyScale;

        transform.localScale = new Vector3(scale.x / disp.x, scale.y / disp.y, scale.z / disp.z);
    }

    Vector3 GetZeroPlaneProjectedVector(Vector3 startPos, Vector3 dir)
    {
        float ratio = Mathf.Abs(startPos.y / dir.y);
        Vector3 result = startPos + dir * ratio;
        result.y = 0.01f;

        return result;
    }


    void Update()
    {
        //if(direction.eulerAngles != prevEuler)
        //{
        //    prevEuler = direction.eulerAngles;
        //    SetupTransform();
        //    SetupPlaneTransform();
        //    SetupCommand();
        //}
    }
}
