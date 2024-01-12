using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CNCMaps.FileFormats;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using TSMapEditor.CCEngine;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public class VxlRenderer
    {
        private const float ModelScale = 0.025f;
        private static readonly Vector3 CameraPosition = new (0.0f, 0.0f, -20.0f);
        private static readonly Vector3 CameraTarget = Vector3.Zero;
        private static readonly Matrix View = Matrix.CreateLookAt(CameraPosition, CameraTarget, Vector3.Up);

        private const float NearClip = 0.01f; // the near clipping plane distance
        private const float FarClip = 100f; // the far clipping plane distance
        private static readonly Matrix Projection = Matrix.CreateOrthographic(10, 10, NearClip, FarClip);

        public static Texture2D Render(GraphicsDevice graphicsDevice, byte facing, RampType ramp, VxlFile vxl, HvaFile hva, Palette palette, VplFile vpl = null)
        {
            var renderTarget = new RenderTarget2D(graphicsDevice, 400, 400, false, SurfaceFormat.Color, DepthFormat.Depth24);
            Renderer.PushRenderTarget(renderTarget);
            
            graphicsDevice.Clear(Color.Transparent);
            graphicsDevice.DepthStencilState = DepthStencilState.Default;

            float rotationFromFacing = 2 * (float)Math.PI * ((float)facing / Constants.FacingMax);

            // Rotates to the game's north
            Matrix rotateToWorld = Matrix.CreateRotationZ(-(MathHelper.ToRadians(135) + rotationFromFacing));
            rotateToWorld *= Matrix.CreateRotationX(MathHelper.ToRadians(-120));
            Matrix scale = Matrix.CreateScale(ModelScale, ModelScale, ModelScale);

            BasicEffect basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = View;
            basicEffect.Projection = Projection;

            Matrix tilt = Matrix.Identity;

            // Rotate the X axis to be parallel to the tilt of the slope, then rotate around this axis
            if (ramp is > RampType.None and < RampType.DoubleUpSWNE)
            {
                Matrix rotateTiltAxis = Matrix.CreateRotationZ(-(MathHelper.ToRadians(SlopeAxisZAngles[(int)ramp - 1])));
                rotateTiltAxis *= Matrix.CreateRotationX(MathHelper.ToRadians(-120));

                Vector3 tiltAxis = Vector3.Transform(Vector3.UnitX, rotateTiltAxis);
                tilt = Matrix.CreateFromAxisAngle(tiltAxis, MathHelper.ToRadians(30));
            }

            var vertexColorIndexedData = new List<VertexData>();

            foreach (var section in vxl.Sections)
            {
                var sectionVertexData = new List<VertexData>();
                for (int x = 0; x < section.SizeX; x++)
                {
                    for (int y = 0; y < section.SizeY; y++)
                    {
                        foreach (VxlFile.Voxel voxel in section.Spans[x, y].Voxels)
                        {
                            sectionVertexData.AddRange(RenderVoxel(voxel, section.GetNormals()[voxel.NormalIndex],
                                palette, section.Scale));
                        }
                    }
                }

                var sectionRotation = hva.LoadMatrix(section.Index);
                sectionRotation.M41 *= section.HvaMultiplier * section.ScaleX;
                sectionRotation.M42 *= section.HvaMultiplier * section.ScaleY;
                sectionRotation.M43 *= section.HvaMultiplier * section.ScaleZ;

                var sectionTranslation = Matrix.CreateTranslation(section.MinBounds);
                var sectionTransform = sectionTranslation * sectionRotation;

                foreach (var vertex in sectionVertexData)
                    vertex.Position = Vector3.Transform(vertex.Position, sectionTransform);

                ApplyLighting(sectionVertexData, vpl, section.NormalsMode == 4 ? 31 : 15,
                    rotationFromFacing);

                vertexColorIndexedData.AddRange(sectionVertexData);
            }

            VertexBuffer vertexBuffer = new VertexBuffer(graphicsDevice,
                typeof(VertexPositionColorNormal), vertexColorIndexedData.Count, BufferUsage.None);

            var vertexData = vertexColorIndexedData.Select(v => v.ToVertexPositionColorNormal());
            vertexBuffer.SetData(vertexData.ToArray());

            var triangleListIndices = new int[vertexColorIndexedData.Count];
            for (int i = 0; i < triangleListIndices.Length; i++)
                triangleListIndices[i] = i;

            IndexBuffer triangleListIndexBuffer = new IndexBuffer(
                graphicsDevice,
                IndexElementSize.ThirtyTwoBits,
                triangleListIndices.Length,
                BufferUsage.None);
            triangleListIndexBuffer.SetData(triangleListIndices);

            graphicsDevice.Indices = triangleListIndexBuffer;
            graphicsDevice.SetVertexBuffer(vertexBuffer);

            Matrix sectionWorld = rotateToWorld * tilt * scale;
            basicEffect.World = sectionWorld;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexColorIndexedData.Count / 3);
            }

            var colorData = new Color[renderTarget.Width * renderTarget.Height];
            renderTarget.GetData(colorData);

            Texture2D tex = new Texture2D(graphicsDevice, renderTarget.Width, renderTarget.Height);
            tex.SetData(colorData);
            
            Renderer.PopRenderTarget();

            return tex;
        }

        private static readonly int[] SlopeAxisZAngles =
        {
            -45, 45, 135, -135,
            0, 90, 180, -90,
            0, 90, 180, -90,
            0, 90, 180, -90,
            0, 90, 180, -90
        };


        private static List<VertexData> RenderVoxel(VxlFile.Voxel voxel, Vector3 normal, Palette palette, Vector3 scale)
        {
            const float radius = 0.5f;
            Vector3 voxelCoordinates = new Vector3(voxel.X, voxel.Y, voxel.Z);

            Vector3[] vertices =
            {
                new(-1, 1, -1), // A1 // 0
                new(1, 1, -1), // B1 // 1           
                new(1, 1, 1), // C1 // 2
                new(-1, 1, 1), // D1 // 3

                new(-1, -1, -1), // A2 // 4
                new(1, -1, -1), // B2 // 5
                new(1, -1, 1), // C2 // 6
                new(-1, -1, 1) // D2 // 7
            };

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] *= radius;
                vertices[i] += voxelCoordinates;
            }

            int[][] triangles =
            {
                new [] { 0, 1, 2 }, new [] { 2, 3, 0 }, // up
                new [] { 7, 6, 5 }, new [] { 5, 4, 7 }, // down
                new [] { 4, 5, 1 }, new [] { 1, 0, 4 }, // forward
                new [] { 3, 2, 6 }, new [] { 6, 7, 3 }, // backward
                new [] { 1, 5, 6 }, new [] { 6, 2, 1 }, // right
                new [] { 4, 0, 3 }, new [] { 3, 7, 4 }, // left
            };

            List<VertexData> newVertices = new();

            foreach (var triangle in triangles)
            {
                AddTriangle(vertices[triangle[0]], vertices[triangle[1]], vertices[triangle[2]]);
            }

            void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
            {
                newVertices.Add(new VertexData(v1 * scale, palette, voxel.ColorIndex, normal));
                newVertices.Add(new VertexData(v2 * scale, palette, voxel.ColorIndex, normal));
                newVertices.Add(new VertexData(v3 * scale, palette, voxel.ColorIndex, normal));
            }

            return newVertices;
        }

        private static void ApplyLighting(List<VertexData> vertices, VplFile vpl, int maxPage, float rotation)
        {
            Vector3 light = Vector3.Transform(-Vector3.UnitX, Matrix.CreateRotationZ(rotation));
            foreach (var vertex in vertices)
            {
                float dot = Vector3.Dot(vertex.Normal, light);
                byte page = Convert.ToByte((dot + 1) / 2 * maxPage);
                vertex.ColorIndex = vpl.GetPaletteIndex(page, vertex.ColorIndex);
            }

        }

        private class VertexData
        {
            public VertexData(Vector3 position, Palette palette, byte colorIndex, Vector3 normal)
            {
                Position = position;
                Palette = palette;
                ColorIndex = colorIndex;
                Normal = normal;
            }

            public VertexPositionColorNormal ToVertexPositionColorNormal()
            {
                return new VertexPositionColorNormal(Position, Palette.Data[ColorIndex].ToXnaColor(), Normal);
            }

            public Vector3 Position;
            public Palette Palette;
            public byte ColorIndex;
            public Vector3 Normal;
        }
    }
}