using System;
using System.Collections.Generic;
using CNCMaps.FileFormats;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using Microsoft.Xna.Framework.Graphics;
using TSMapEditor.CCEngine;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public class VxlRenderer
    {
        private const float ModelScale = 0.028f;
        private static readonly Vector3 CameraPosition = new (0.0f, 0.0f, -20.0f);
        private static readonly Vector3 CameraTarget = Vector3.Zero;
        private static readonly Matrix View = Matrix.CreateLookAt(CameraPosition, CameraTarget, Vector3.Up);

        private const float NearClip = 0.01f; // the near clipping plane distance
        private const float FarClip = 100f; // the far clipping plane distance
        private static readonly Matrix Projection = Matrix.CreateOrthographic(10, 10, NearClip, FarClip);

        public static Texture2D Render(GraphicsDevice graphicsDevice, byte rotation, RampType ramp, VxlFile vxl, HvaFile hva, CCEngine.Palette palette, VplFile vpl = null)
        {
            var renderTarget = new RenderTarget2D(graphicsDevice, 400, 400, false, SurfaceFormat.Color, DepthFormat.Depth24);
            graphicsDevice.SetRenderTarget(renderTarget, 0);
            graphicsDevice.Clear(Color.Transparent);
            graphicsDevice.DepthStencilState = DepthStencilState.Default;

            Matrix world = Matrix.CreateRotationZ(-(MathHelper.ToRadians(135) + 2 * (float)Math.PI * ((float)rotation / Constants.FacingMax)));
            world *= Matrix.CreateRotationX(MathHelper.ToRadians(-120));
            world *= Matrix.CreateScale(ModelScale, ModelScale, ModelScale);

            BasicEffect basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = View;
            basicEffect.Projection = Projection;

            Matrix tilt = Matrix.Identity;
            int tiltPitch, tiltYaw;
            
            if (ramp is RampType.None or >= RampType.DoubleUpSWNE)
            {
                tiltPitch = tiltYaw = 0;
            }
            else if (ramp <= RampType.South)
            {
                // screen-diagonal facings (perpendicular to axes)
                tiltPitch = 25;
                tiltYaw = -90 * (byte)ramp;
            }
            else
            {
                // world-diagonal facings (perpendicular to screen)
                tiltPitch = 25;
                tiltYaw = 225 - 90 * (((byte)ramp - 1) % 4);
            }

            tilt *= Matrix.CreateRotationX(MathHelper.ToRadians(tiltPitch));
            tilt *= Matrix.CreateRotationZ(MathHelper.ToRadians(tiltYaw));
            
            foreach (var section in vxl.Sections)
            {
                var vertexData = new List<VertexPositionColorNormal>();
                for (int x = 0; x < section.SizeX; x++)
                {
                    for (int y = 0; y < section.SizeY; y++)
                    {
                        foreach (VxlFile.Voxel voxel in section.Spans[x, y].Voxels)
                        {
                            vertexData.AddRange(RenderVoxel(voxel, section.GetNormals()[voxel.NormalIndex],
                                palette, section.Scale));
                        }
                    }
                }

                VertexBuffer vertexBuffer = new VertexBuffer(graphicsDevice,
                    typeof(VertexPositionColorNormal), vertexData.Count, BufferUsage.None);
                vertexBuffer.SetData(vertexData.ToArray());

                var triangleListIndices = new int[vertexData.Count];
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

                var modelRotation = hva.LoadMatrix(section.Index);
                modelRotation.M41 *= section.HvaMultiplier * section.ScaleX;
                modelRotation.M42 *= section.HvaMultiplier * section.ScaleY;
                modelRotation.M43 *= section.HvaMultiplier * section.ScaleZ;

                var modelTranslation = Matrix.CreateTranslation(section.MinBounds);
                var modelTransform = modelTranslation * modelRotation;

                Matrix sectionWorld = modelTransform * tilt * world;
                basicEffect.World = sectionWorld;

                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexData.Count / 3);
                }
            }

            var colorData = new Color[renderTarget.Width * renderTarget.Height];
            renderTarget.GetData(colorData);

            Texture2D tex = new Texture2D(graphicsDevice, renderTarget.Width, renderTarget.Height);
            tex.SetData(colorData);

            return tex;
        }

        private static List<VertexPositionColorNormal> RenderVoxel(VxlFile.Voxel voxel, Vector3 normal, CCEngine.Palette palette, Vector3 scale)
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

            List<VertexPositionColorNormal> newVertices = new();

            foreach (var triangle in triangles)
            {
                AddTriangle(vertices[triangle[0]], vertices[triangle[1]], vertices[triangle[2]]);
            }

            void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
            {
                newVertices.Add(new VertexPositionColorNormal(v1 * scale, palette.Data[voxel.ColorIndex].ToXnaColor(), normal));
                newVertices.Add(new VertexPositionColorNormal(v2 * scale, palette.Data[voxel.ColorIndex].ToXnaColor(), normal));
                newVertices.Add(new VertexPositionColorNormal(v3 * scale, palette.Data[voxel.ColorIndex].ToXnaColor(), normal));
            }

            return newVertices;
        }
    }
}