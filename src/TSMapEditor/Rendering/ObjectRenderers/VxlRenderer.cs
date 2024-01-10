using System.Collections.Generic;
using System.IO;
using CNCMaps.FileFormats;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.WIC;
using static CNCMaps.FileFormats.VxlFile;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public class VxlRenderer
    {
        public GraphicsDevice GraphicsDevice { get; set; }
        public Texture2D TestDraw()
        {
            var renderTarget = new RenderTarget2D(GraphicsDevice, 400, 400, false, SurfaceFormat.Color, DepthFormat.Depth24);
            GraphicsDevice.SetRenderTarget(renderTarget, 0);
            GraphicsDevice.Clear(Color.Transparent);
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            float modelScale = 0.028f;

            Matrix world = Matrix.CreateTranslation(0, 0, 10);
            world *= Matrix.CreateRotationX(MathHelper.ToRadians(60));
            world *= Matrix.CreateRotationY(MathHelper.ToRadians(180));
            world *= Matrix.CreateRotationZ(MathHelper.ToRadians(-45));
            world *= Matrix.CreateScale(modelScale, modelScale, modelScale);

            Vector3 cameraPosition = new Vector3(0.0f, 0.0f, -10.0f);
            Vector3 cameraTarget = new Vector3(0.0f, 0.0f, 0.0f);
            Matrix view = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);

            float near = 0.01f; // the near clipping plane distance
            float far = 100f; // the far clipping plane distance
            Matrix projection = Matrix.CreateOrthographic(10, 10, near, far);

            BasicEffect basicEffect = new BasicEffect(GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.World = world;
            basicEffect.View = view;
            basicEffect.Projection = projection;

            VxlFile vxlFile;
            using (Stream stream = File.OpenRead(@"C:\Users\Parasite03\Desktop\flata.vxl"))
            {
                vxlFile = new VxlFile(stream);
                vxlFile.Initialize();
            }

            byte[] paletteData = File.ReadAllBytes(@"C:\Users\Parasite03\Desktop\unittem.pal");
            CCEngine.Palette unittem = new (paletteData);

            foreach (var section in vxlFile.Sections)
            {
                var vertexData = new List<VertexPositionColorNormal>();
                for (int x = 0; x < section.SizeX; x++)
                {
                    for (int y = 0; y < section.SizeY; y++)
                    {
                        foreach (Voxel voxel in section.Spans[x, y].Voxels)
                        {
                            vertexData.AddRange(
                                RenderVoxel(voxel, section.GetNormals()[voxel.NormalIndex], unittem));
                        }
                    }
                }

                VertexBuffer vertexBuffer = new VertexBuffer(GraphicsDevice,
                    typeof(VertexPositionColorNormal), vertexData.Count, BufferUsage.None);
                vertexBuffer.SetData(vertexData.ToArray());

                var triangleListIndices = new int[vertexData.Count];
                for (int i = 0; i < triangleListIndices.Length; i++)
                    triangleListIndices[i] = i;

                IndexBuffer triangleListIndexBuffer = new IndexBuffer(
                    GraphicsDevice,
                    IndexElementSize.ThirtyTwoBits,
                    triangleListIndices.Length,
                    BufferUsage.None);
                triangleListIndexBuffer.SetData(triangleListIndices);

                GraphicsDevice.Indices = triangleListIndexBuffer;
                GraphicsDevice.SetVertexBuffer(vertexBuffer);

                Matrix sectionWorld = world * Matrix.CreateScale(section.Scale);
                basicEffect.World = sectionWorld;

                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexData.Count / 3);
                }
            }

            GraphicsDevice.SetRenderTarget(null);

            var colorData = new Color[renderTarget.Width * renderTarget.Height];
            renderTarget.GetData(colorData);

            Texture2D tex = new Texture2D(GraphicsDevice, renderTarget.Width, renderTarget.Height);
            tex.SetData(colorData);

            return tex;
        }

        private List<VertexPositionColorNormal> RenderVoxel(Voxel voxel, Vector3 normal, CCEngine.Palette palette)
        {
            const float radius = 0.5f;
            float left = voxel.X - radius;
            float right = voxel.X + radius;
            float bottom = voxel.Y - radius;
            float top = voxel.Y + radius;
            float front = voxel.Z - radius;
            float back = voxel.Z + radius;

            List<VertexPositionColorNormal> newVertices = new();

            // Base
            AddTriangle(new Vector3(left, bottom, front), new Vector3(right, bottom, front), new Vector3(left, bottom, back));
            AddTriangle(new Vector3(left, bottom, back), new Vector3(right, bottom, front), new Vector3(right, bottom, back));

            // Back
            AddTriangle(new Vector3(left, bottom, back), new Vector3(right, bottom, back), new Vector3(left, top, back));
            AddTriangle(new Vector3(left, top, back), new Vector3(right, bottom, back), new Vector3(right, top, back));

            // Top
            AddTriangle(new Vector3(left, top, front), new Vector3(right, top, front), new Vector3(left, top, back));
            AddTriangle(new Vector3(left, top, back), new Vector3(right, top, front), new Vector3(right, top, back));

            // Right
            AddTriangle(new Vector3(right, bottom, front), new Vector3(right, top, front), new Vector3(right, bottom, back));
            AddTriangle(new Vector3(right, top, front), new Vector3(right, top, back), new Vector3(right, bottom, back));

            // Front
            AddTriangle(new Vector3(left, bottom, front), new Vector3(right, bottom, front), new Vector3(left, top, front));
            AddTriangle(new Vector3(left, top, front), new Vector3(right, bottom, front), new Vector3(right, top, front));

            // Left
            AddTriangle(new Vector3(left, bottom, front), new Vector3(left, top, front), new Vector3(left, bottom, back));
            AddTriangle(new Vector3(left, top, front), new Vector3(left, top, back), new Vector3(left, bottom, back));

            void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
            {
                newVertices.Add(new VertexPositionColorNormal(v1, palette.Data[voxel.ColorIndex].ToXnaColor(), normal));
                newVertices.Add(new VertexPositionColorNormal(v2, palette.Data[voxel.ColorIndex].ToXnaColor(), normal));
                newVertices.Add(new VertexPositionColorNormal(v3, palette.Data[voxel.ColorIndex].ToXnaColor(), normal));
            }

            return newVertices;
        }
    }
}