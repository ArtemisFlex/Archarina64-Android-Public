using Android.Content;
using Android.Graphics;
using Android.Opengl;
using Android.Views;
using Archarina64.Core;
using Java.Nio;
using EGLConfig = Javax.Microedition.Khronos.Egl.EGLConfig;
using Javax.Microedition.Khronos.Opengles;
using Vector3 = System.Numerics.Vector3;
using Vector2 = System.Numerics.Vector2;
using GlesMatrix = Android.Opengl.Matrix;

namespace Archarina64.Android;

public sealed class RoomViewport : GLSurfaceView
{
    private readonly RoomRenderer renderer = new();
    private readonly ScaleGestureDetector scaling;
    private float lastX, lastY;

    public RoomViewport(Context context) : base(context)
    {
        SetEGLContextClientVersion(2);
        SetRenderer(renderer);
        RenderMode = Rendermode.WhenDirty;
        ContentDescription = "Room preview. Drag to orbit; pinch to zoom. Select rooms in the room list.";
        scaling = new ScaleGestureDetector(context, new ZoomListener(factor =>
        {
            QueueEvent(() => renderer.Zoom = Math.Clamp(renderer.Zoom / factor, 0.2f, 8f));
            RequestRender();
        }));
    }

    public void ShowLayout(MobileLayout layout, int selected)
    {
        // Build an immutable draw buffer before handing it to the GL thread.
        var data = RoomRenderer.BuildFrame(layout, selected);
        QueueEvent(() => renderer.SetFrame(data));
        RequestRender();
    }

    public void ShowNativeRoom(RomRoomGeometry? geometry, RomCollisionData? collision, bool showGeometry, bool showCollision)
    {
        var data = RoomRenderer.BuildNativeFrame(geometry, collision, showGeometry, showCollision);
        QueueEvent(() => renderer.SetFrame(data));
        RequestRender();
    }

    public void ResetCamera()
    {
        QueueEvent(() => { renderer.Yaw = 0.65f; renderer.Pitch = 0.65f; renderer.Zoom = 1; });
        RequestRender();
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null) return false;
        scaling.OnTouchEvent(e);
        if (e.ActionMasked == MotionEventActions.Down) Parent?.RequestDisallowInterceptTouchEvent(true);
        if (e.ActionMasked == MotionEventActions.Move && e.PointerCount == 1 && !scaling.IsInProgress)
        {
            float dx = (e.GetX() - lastX) * 0.008f, dy = (e.GetY() - lastY) * 0.008f;
            QueueEvent(() => { renderer.Yaw -= dx; renderer.Pitch = Math.Clamp(renderer.Pitch + dy, -1.35f, 1.35f); });
            RequestRender();
        }
        lastX = e.GetX(); lastY = e.GetY();
        if (e.ActionMasked == MotionEventActions.Up) PerformClick();
        return true;
    }

    public override bool PerformClick() { base.PerformClick(); return true; }
    private sealed class ZoomListener(Action<float> zoom) : ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        public override bool OnScale(ScaleGestureDetector detector) { zoom(detector.ScaleFactor); return true; }
    }
}

internal sealed class RoomRenderer : Java.Lang.Object, GLSurfaceView.IRenderer
{
    internal sealed record RenderFrame(float[] Vertices, byte[]? TextureBytes);
    private FloatBuffer? buffer;
    private int count, program, position, color, uv, useTexture, mvp, sampler, textureId;
    private float aspect = 1;
    public float Yaw = 0.65f, Pitch = 0.65f, Zoom = 1;
    private readonly float[] projection = new float[16], view = new float[16], matrix = new float[16];

    public void SetFrame(RenderFrame frame)
    {
        float[] vertices = frame.Vertices;
        buffer?.Dispose();
        buffer = ByteBuffer.AllocateDirect(vertices.Length * sizeof(float))!.Order(ByteOrder.NativeOrder()!)!.AsFloatBuffer()!;
        buffer.Put(vertices); buffer.Position(0); count = vertices.Length / 9;
        if (textureId != 0) { GLES20.GlDeleteTextures(1, [textureId], 0); textureId = 0; }
        if (frame.TextureBytes is { Length: > 0 })
        {
            using var bitmap = BitmapFactory.DecodeByteArray(frame.TextureBytes, 0, frame.TextureBytes.Length) ?? throw new InvalidDataException("Embedded texture could not be decoded.");
            int[] ids = new int[1]; GLES20.GlGenTextures(1, ids, 0); textureId = ids[0];
            GLES20.GlBindTexture(GLES20.GlTexture2d, textureId);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMinFilter, GLES20.GlLinear);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMagFilter, GLES20.GlLinear);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapS, GLES20.GlRepeat);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapT, GLES20.GlRepeat);
            GLUtils.TexImage2D(GLES20.GlTexture2d, 0, bitmap, 0);
            GLES20.GlBindTexture(GLES20.GlTexture2d, 0);
        }
    }

    public static RenderFrame BuildFrame(MobileLayout layout, int selected)
    {
        var meshes = layout.Tiles.Select(t => (Tile: t, Mesh: DesktopImport.ReadTile(t.SourceXml))).ToArray();
        if (meshes.Length == 0) return new RenderFrame([], null);
        string? selectedTexture = null;
        byte[]? selectedTextureBytes = null;
        foreach (var item in layout.Tiles.SelectMany(t => t.EmbeddedTextures))
            if (item.Value.Length > 0) { selectedTexture = item.Key; selectedTextureBytes = Convert.FromBase64String(item.Value); break; }
        Vector3 minimum = new(float.MaxValue), maximum = new(float.MinValue);
        foreach (var item in meshes)
            foreach (var point in item.Mesh.Vertices) { var p = item.Tile.Transform(point); minimum = Vector3.Min(minimum, p); maximum = Vector3.Max(maximum, p); }
        Vector3 center = (minimum + maximum) / 2;
        float scale = 2f / MathF.Max((maximum - minimum).Length(), 1);
        var vertices = new List<float>();
        for (int room = 0; room < meshes.Length; room++)
        {
            var (tile, mesh) = meshes[room];
            for (int i = 0; i < mesh.Indices.Length; i += 3)
            {
                var a = tile.Transform(mesh.Vertices[mesh.Indices[i]]);
                var b = tile.Transform(mesh.Vertices[mesh.Indices[i + 1]]);
                var c = tile.Transform(mesh.Vertices[mesh.Indices[i + 2]]);
                var normal = Vector3.Cross(b - a, c - a);
                float light = normal.LengthSquared() < 1e-10f ? 0.7f : 0.45f + 0.55f * MathF.Abs(Vector3.Dot(Vector3.Normalize(normal), Vector3.Normalize(new Vector3(1, 2, 3))));
                Vector3 materialColor = mesh.FaceColors[Math.Min(i / 3, mesh.FaceColors.Length - 1)];
                Vector3 roomColor = room == selected ? new Vector3(0.80f, 1.0f, 0.88f) : Vector3.One;
                int face = i / 3;
                string faceTexture = face < mesh.FaceTextures.Length ? mesh.FaceTextures[face] : "";
                bool textured = selectedTexture is not null && string.Equals(faceTexture, selectedTexture, StringComparison.OrdinalIgnoreCase);
                Vector3[] points = [a, b, c];
                for (int corner = 0; corner < points.Length; corner++)
                {
                    var p = points[corner];
                    var v = (p - center) * scale;
                    var color = Vector3.Min(materialColor * roomColor * light, Vector3.One);
                    int uvIndex = i + corner < mesh.TexCoordIndices.Length ? mesh.TexCoordIndices[i + corner] : -1;
                    Vector2 uvValue = textured && uvIndex >= 0 && uvIndex < mesh.UVs.Length ? mesh.UVs[uvIndex] : Vector2.Zero;
                    vertices.AddRange([v.X, v.Y, v.Z, color.X, color.Y, color.Z, uvValue.X, 1 - uvValue.Y, textured ? 1 : 0]);
                }
            }
        }
        return new RenderFrame(vertices.ToArray(), selectedTextureBytes);
    }

    public static RenderFrame BuildNativeFrame(RomRoomGeometry? geometry, RomCollisionData? collision, bool showGeometry, bool showCollision)
    {
        var points = new List<Vector3>();
        var geometryTriangles = new List<(RomGeometryVertex A, RomGeometryVertex B, RomGeometryVertex C)>();
        if (showGeometry && geometry is not null)
        {
            var candidates = geometry.Triangles
                .Where(t => t.A >= 0 && t.B >= 0 && t.C >= 0 && t.A < geometry.Vertices.Count && t.B < geometry.Vertices.Count && t.C < geometry.Vertices.Count)
                .Select(t => (A: geometry.Vertices[t.A], B: geometry.Vertices[t.B], C: geometry.Vertices[t.C]))
                .ToArray();
            // A malformed display-list pointer can still produce an in-range
            // vertex with a huge edge. Drop those outliers before framing or
            // drawing; otherwise one bad triangle turns into viewport-sized
            // spikes and makes the whole room unusable.
            var edgeLengths = candidates.SelectMany(t =>
            {
                var a = new Vector3(t.A.X, t.A.Y, t.A.Z); var b = new Vector3(t.B.X, t.B.Y, t.B.Z); var c = new Vector3(t.C.X, t.C.Y, t.C.Z);
                return new[] { Vector3.Distance(a, b), Vector3.Distance(b, c), Vector3.Distance(c, a) };
            }).OrderBy(value => value).ToArray();
            float medianEdge = edgeLengths.Length == 0 ? 0 : edgeLengths[edgeLengths.Length / 2];
            float edgeLimit = MathF.Max(8000f, medianEdge * 12f);
            geometryTriangles.AddRange(candidates.Where(t =>
            {
                var a = new Vector3(t.A.X, t.A.Y, t.A.Z); var b = new Vector3(t.B.X, t.B.Y, t.B.Z); var c = new Vector3(t.C.X, t.C.Y, t.C.Z);
                return Vector3.Distance(a, b) <= edgeLimit && Vector3.Distance(b, c) <= edgeLimit && Vector3.Distance(c, a) <= edgeLimit;
            }));
            foreach (var triangle in geometryTriangles)
            {
                points.Add(new Vector3(triangle.A.X, triangle.A.Y, triangle.A.Z));
                points.Add(new Vector3(triangle.B.X, triangle.B.Y, triangle.B.Z));
                points.Add(new Vector3(triangle.C.X, triangle.C.Y, triangle.C.Z));
            }
        }
        if (showCollision && collision?.Vertices is { } collisionVertices) points.AddRange(collisionVertices.Select(v => new Vector3(v.X, v.Y, v.Z)));
        if (points.Count == 0) return new RenderFrame([], null);
        Vector3 minimum = points[0], maximum = points[0];
        foreach (var point in points.Skip(1)) { minimum = Vector3.Min(minimum, point); maximum = Vector3.Max(maximum, point); }
        Vector3 center = (minimum + maximum) / 2;
        float scale = 2f / MathF.Max((maximum - minimum).Length(), 1);
        var vertices = new List<float>();

        if (showGeometry && geometry is not null)
        {
            foreach (var triangle in geometryTriangles)
            {
                var a = triangle.A; var b = triangle.B; var c = triangle.C;
                var normal = Vector3.Cross(new Vector3(b.X - a.X, b.Y - a.Y, b.Z - a.Z), new Vector3(c.X - a.X, c.Y - a.Y, c.Z - a.Z));
                float light = normal.LengthSquared() < 1e-10f ? 0.7f : 0.45f + 0.55f * MathF.Abs(Vector3.Dot(Vector3.Normalize(normal), Vector3.Normalize(new Vector3(1, 2, 3))));
                Vector3[] pointsForTriangle = [new(a.X, a.Y, a.Z), new(b.X, b.Y, b.Z), new(c.X, c.Y, c.Z)];
                Vector3 faceColor = new((a.R + b.R + c.R) / (3f * 255f), (a.G + b.G + c.G) / (3f * 255f), (a.B + b.B + c.B) / (3f * 255f));
                foreach (var point in pointsForTriangle) { var p = (point - center) * scale; var color = Vector3.Min(faceColor * light, Vector3.One); vertices.AddRange([p.X, p.Y, p.Z, color.X, color.Y, color.Z, 0, 0, 0]); }
            }
        }

        if (showCollision && collision?.Vertices is { } collisionTriangleVertices && collision.Triangles is { } collisionTriangles)
        {
            foreach (var triangle in collisionTriangles)
            {
                if (triangle.A >= collisionTriangleVertices.Count || triangle.B >= collisionTriangleVertices.Count || triangle.C >= collisionTriangleVertices.Count) continue;
                var a = collisionTriangleVertices[triangle.A]; var b = collisionTriangleVertices[triangle.B]; var c = collisionTriangleVertices[triangle.C];
                foreach (var point in new[] { new Vector3(a.X, a.Y, a.Z), new Vector3(b.X, b.Y, b.Z), new Vector3(c.X, c.Y, c.Z) }) { var p = (point - center) * scale; vertices.AddRange([p.X, p.Y, p.Z, 0.9f, 0.18f, 0.22f, 0, 0, 0]); }
            }
        }
        return new RenderFrame(vertices.ToArray(), null);
    }

    public void OnSurfaceCreated(IGL10? gl, EGLConfig? config)
    {
        int vs = Compile(GLES20.GlVertexShader, "attribute vec3 aPosition; attribute vec3 aColor; attribute vec2 aUv; attribute float aUseTexture; uniform mat4 uMvp; varying vec3 vColor; varying vec2 vUv; varying float vUseTexture; void main(){ vColor=aColor; vUv=aUv; vUseTexture=aUseTexture; gl_Position=uMvp*vec4(aPosition,1.0); }");
        int fs = Compile(GLES20.GlFragmentShader, "precision mediump float; varying vec3 vColor; varying vec2 vUv; varying float vUseTexture; uniform sampler2D uTexture; void main(){ vec4 base=vec4(vColor,1.0); if(vUseTexture>0.5) base*=texture2D(uTexture,vUv); gl_FragColor=base; }");
        program = GLES20.GlCreateProgram();
        GLES20.GlAttachShader(program, vs); GLES20.GlAttachShader(program, fs); GLES20.GlLinkProgram(program);
        int[] linked = new int[1]; GLES20.GlGetProgramiv(program, GLES20.GlLinkStatus, linked, 0);
        if (linked[0] == 0) throw new InvalidOperationException(GLES20.GlGetProgramInfoLog(program));
        GLES20.GlDeleteShader(vs); GLES20.GlDeleteShader(fs);
        position = GLES20.GlGetAttribLocation(program, "aPosition"); color = GLES20.GlGetAttribLocation(program, "aColor"); uv = GLES20.GlGetAttribLocation(program, "aUv"); useTexture = GLES20.GlGetAttribLocation(program, "aUseTexture"); mvp = GLES20.GlGetUniformLocation(program, "uMvp"); sampler = GLES20.GlGetUniformLocation(program, "uTexture");
        GLES20.GlEnable(GLES20.GlDepthTest);
        GLES20.GlClearColor(0.055f, 0.075f, 0.11f, 1);
    }

    public void OnSurfaceChanged(IGL10? gl, int width, int height) { aspect = (float)width / Math.Max(height, 1); GLES20.GlViewport(0, 0, width, height); }
    public void OnDrawFrame(IGL10? gl)
    {
        GLES20.GlClear(GLES20.GlColorBufferBit | GLES20.GlDepthBufferBit);
        if (buffer is null || count == 0) return;
        float distance = 3.2f * Zoom;
        GlesMatrix.PerspectiveM(projection, 0, 45, aspect, 0.01f, 100);
        GlesMatrix.SetLookAtM(view, 0, distance * MathF.Cos(Pitch) * MathF.Sin(Yaw), distance * MathF.Sin(Pitch), distance * MathF.Cos(Pitch) * MathF.Cos(Yaw), 0, 0, 0, 0, 1, 0);
        GlesMatrix.MultiplyMM(matrix, 0, projection, 0, view, 0);
        GLES20.GlUseProgram(program); GLES20.GlUniformMatrix4fv(mvp, 1, false, matrix, 0); GLES20.GlActiveTexture(GLES20.GlTexture0); GLES20.GlBindTexture(GLES20.GlTexture2d, textureId); GLES20.GlUniform1i(sampler, 0);
        buffer.Position(0); GLES20.GlVertexAttribPointer(position, 3, GLES20.GlFloat, false, 36, buffer); GLES20.GlEnableVertexAttribArray(position);
        buffer.Position(3); GLES20.GlVertexAttribPointer(color, 3, GLES20.GlFloat, false, 36, buffer); GLES20.GlEnableVertexAttribArray(color);
        buffer.Position(6); GLES20.GlVertexAttribPointer(uv, 2, GLES20.GlFloat, false, 36, buffer); GLES20.GlEnableVertexAttribArray(uv);
        buffer.Position(8); GLES20.GlVertexAttribPointer(useTexture, 1, GLES20.GlFloat, false, 36, buffer); GLES20.GlEnableVertexAttribArray(useTexture);
        GLES20.GlDrawArrays(GLES20.GlTriangles, 0, count);
    }

    private static int Compile(int type, string source)
    {
        int shader = GLES20.GlCreateShader(type); GLES20.GlShaderSource(shader, source); GLES20.GlCompileShader(shader);
        int[] result = new int[1]; GLES20.GlGetShaderiv(shader, GLES20.GlCompileStatus, result, 0);
        if (result[0] == 0) throw new InvalidOperationException(GLES20.GlGetShaderInfoLog(shader));
        return shader;
    }
}
