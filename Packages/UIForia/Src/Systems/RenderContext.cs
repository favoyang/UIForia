using System;
using UIForia.Layout;
using UIForia.Rendering.Vertigo;
using UIForia.Util;
using UnityEngine;
using UnityEngine.Rendering;
using PooledMesh = UIForia.Rendering.Vertigo.PooledMesh;

namespace UIForia.Rendering {

    public struct FontData {

        public FontAsset fontAsset;
        public float gradientScale;
        public float scaleRatioA;
        public float scaleRatioB;
        public float scaleRatioC;
        public int textureWidth;
        public int textureHeight;

    }


    internal enum RenderOperationType {

        DrawBatch,
        PushRenderTexture,
        ClearRenderTextureRegion,
        BlitRenderTexture,
        SetScissorRect,
        SetCameraViewMatrix,
        SetCameraProjectionMatrix

    }

    internal struct RenderOperation {

        public int batchIndex;
        public RenderOperationType operationType;
        public RenderTexture renderTexture;
        public SimpleRectPacker.PackedRect rect;

        public RenderOperation(int batchIndex) {
            this.rect = default;
            this.renderTexture = null;
            this.batchIndex = batchIndex;
            this.operationType = RenderOperationType.DrawBatch;
        }

    }

    public class RenderContext {

        internal const int k_ObjectCount_Small = 8;
        internal const int k_ObjectCount_Medium = 16;
        internal const int k_ObjectCount_Large = 32;
        internal const int k_ObjectCount_Huge = 64;
        internal const int k_ObjectCount_Massive = 128;

        public StructList<Vector3> positionList;
        public StructList<Vector3> normalList;
        public StructList<Vector4> colorList;
        public StructList<Vector4> texCoordList0;
        public StructList<Vector4> texCoordList1;
        public StructList<Vector4> texCoordList2;
        public StructList<Vector4> texCoordList3;
        public StructList<int> triangleList;

        private Batch currentBatch;
        private Material activeMaterial;

        private readonly MeshPool uiforiaMeshPool;
        private readonly UIForiaMaterialPool uiforiaMaterialPool;
        private readonly StructStack<Rect> clipStack;

        private int defaultRTDepth;
        private RenderTextureDescriptor defaultRTDesc;
        private RenderTextureFormat defaultRTFormat;

        private static readonly int s_MaxTextureSize;

        private readonly MaterialPropertyBlock effectBlitBlock;
        private readonly Mesh unitQuadMesh;
        private readonly Material effectBlitMaterial;
        private readonly StructList<ScratchRenderTexture> scratchTextures;
        private readonly StructList<RenderOperation> renderCommandList;
        private readonly StructList<Batch> pendingBatches;
        private RenderTexture pingPongTexture;
        
        static RenderContext() {
            int maxTextureSize = SystemInfo.maxTextureSize;
            s_MaxTextureSize = Mathf.Min(maxTextureSize, 4096);
        }

        internal RenderContext(Material batchedMaterial) {
            this.pendingBatches = new StructList<Batch>();
            this.uiforiaMeshPool = new MeshPool();
            this.uiforiaMaterialPool = new UIForiaMaterialPool(batchedMaterial);
            this.positionList = new StructList<Vector3>(128);
            this.texCoordList0 = new StructList<Vector4>(128);
            this.texCoordList1 = new StructList<Vector4>(128);
            this.triangleList = new StructList<int>(128 * 3);
            this.clipStack = new StructStack<Rect>();
            this.renderCommandList = new StructList<RenderOperation>();
            this.scratchTextures = new StructList<ScratchRenderTexture>();

            this.effectBlitBlock = new MaterialPropertyBlock();

            this.unitQuadMesh = UIForiaGeometry.CreateQuadMesh(1, 1);
            this.effectBlitMaterial = new Material(Shader.Find("UIForia/EffectBlit"));
        }

        public void SetMaterial(Material material) {
            if (currentBatch.batchType == BatchType.Custom) {
                if (activeMaterial != material) {
                    FinalizeCurrentBatch(false);
                }
            }

            activeMaterial = material;
        }

        public void DrawMesh(Mesh mesh, Material material, in Matrix4x4 transform) {
            FinalizeCurrentBatch(false);
        }

        public void DrawGeometry(UIForiaGeometry geometry, Material material) {
            if (currentBatch.batchType != BatchType.Custom) {
                FinalizeCurrentBatch(false);
            }

            if (currentBatch.material != material) {
                FinalizeCurrentBatch(false);
            }

            int start = positionList.size;
            GeometryRange range = new GeometryRange(0, geometry.positionList.size, 0, geometry.triangleList.size);

            positionList.AddRange(geometry.positionList, range.vertexStart, range.vertexEnd);
            texCoordList0.AddRange(geometry.texCoordList0, range.vertexStart, range.vertexEnd);
            texCoordList1.AddRange(geometry.texCoordList1, range.vertexStart, range.vertexEnd);

            currentBatch.drawCallSize++;
            currentBatch.material = material;
            currentBatch.batchType = BatchType.Custom;

            triangleList.EnsureAdditionalCapacity(range.triangleEnd - range.triangleStart);

            int offset = triangleList.size;
            int[] triangles = triangleList.array;
            int[] geometryTriangles = geometry.triangleList.array;

            for (int i = range.triangleStart; i < range.triangleEnd; i++) {
                triangles[offset + i] = start + geometryTriangles[i];
            }

            triangleList.size += (range.triangleEnd - range.triangleStart);

            FinalizeCurrentBatch(false);
        }

        public void DrawBatchedText(UIForiaGeometry geometry, in GeometryRange range, in Matrix4x4 transform, in FontData fontData) {
            if (currentBatch.batchType == BatchType.Custom) {
                FinalizeCurrentBatch(false);
            }

            if (currentBatch.batchType == BatchType.Unset) {
                currentBatch.batchType = BatchType.UIForia;
                currentBatch.uiforiaData = new UIForiaData(); // todo -- pool
            }

            // todo -- in the future see if we can use atlased font textures, only need to filter by channel 
            if (currentBatch.uiforiaData.fontData.fontAsset != null && currentBatch.uiforiaData.fontData.fontAsset != fontData.fontAsset) {
                FinalizeCurrentBatch(false);
                currentBatch.batchType = BatchType.UIForia;
                currentBatch.uiforiaData = new UIForiaData(); // todo -- pool
            }

            currentBatch.uiforiaData.transformData.Add(transform);
            currentBatch.uiforiaData.objectData0.Add(geometry.objectData);
            currentBatch.uiforiaData.colors.Add(geometry.packedColors);
            currentBatch.uiforiaData.fontData = fontData;

            UpdateUIForiaGeometry(geometry, range);
        }

        public void DrawBatchedGeometry(UIForiaGeometry geometry, in GeometryRange range, in Matrix4x4 transform) {
            if (currentBatch.batchType == BatchType.Custom) {
                FinalizeCurrentBatch(false);
            }

            if (currentBatch.batchType == BatchType.Unset) {
                currentBatch.batchType = BatchType.UIForia;
                currentBatch.uiforiaData = new UIForiaData(); // todo -- pool
            }

            if (currentBatch.uiforiaData.mainTexture != null && currentBatch.uiforiaData.mainTexture != geometry.mainTexture) {
                FinalizeCurrentBatch(false);
                currentBatch.batchType = BatchType.UIForia;
                currentBatch.uiforiaData = new UIForiaData(); // todo -- pool
            }

            currentBatch.uiforiaData.mainTexture = geometry.mainTexture != null ? geometry.mainTexture : currentBatch.uiforiaData.mainTexture;
            currentBatch.uiforiaData.colors.Add(geometry.packedColors);
            currentBatch.uiforiaData.objectData0.Add(geometry.objectData);
            currentBatch.uiforiaData.transformData.Add(transform);

            UpdateUIForiaGeometry(geometry, range);
        }

        private void UpdateUIForiaGeometry(UIForiaGeometry geometry, in GeometryRange range) {
            int start = positionList.size;

            positionList.AddRange(geometry.positionList, range.vertexStart, range.vertexEnd);
            texCoordList0.AddRange(geometry.texCoordList0, range.vertexStart, range.vertexEnd);
            texCoordList1.AddRange(geometry.texCoordList1, range.vertexStart, range.vertexEnd);

            for (int i = range.vertexStart; i < range.vertexEnd; i++) {
                texCoordList1.array[start + i].w = currentBatch.drawCallSize;
            }

            currentBatch.drawCallSize++;

            triangleList.EnsureAdditionalCapacity(range.triangleEnd - range.triangleStart);

            int offset = triangleList.size;
            int[] triangles = triangleList.array;
            int[] geometryTriangles = geometry.triangleList.array;

            for (int i = range.triangleStart; i < range.triangleEnd; i++) {
                triangles[offset + i] = start + geometryTriangles[i];
            }

            triangleList.size += (range.triangleEnd - range.triangleStart);
        }

        private void FinalizeCurrentBatch(bool cloneMaterial = true) {
            // if have pending things to draw, create batch from them

            if (positionList.size == 0) {
                return;
            }

            if (currentBatch.batchType == BatchType.UIForia) {
                // select material based on batch size
                PooledMesh mesh = uiforiaMeshPool.Get(); // todo -- maybe worth trying to find a large mesh
                UIForiaPropertyBlock propertyBlock = uiforiaMaterialPool.GetPropertyBlock(currentBatch.drawCallSize);

                int vertexCount = positionList.size;
                int triangleCount = triangleList.size;

                mesh.SetVertices(positionList.array, vertexCount);
                mesh.SetTextureCoord0(texCoordList0.array, vertexCount);
                mesh.SetTextureCoord1(texCoordList1.array, vertexCount);
                mesh.SetTriangles(triangleList.array, triangleCount);

                positionList.size = 0;
                texCoordList0.size = 0;
                texCoordList1.size = 0;
                triangleList.size = 0;

                currentBatch.uiforiaPropertyBlock = propertyBlock;
                currentBatch.pooledMesh = mesh;
                pendingBatches.Add(currentBatch);
                renderCommandList.Add(new RenderOperation(pendingBatches.size - 1));
                currentBatch = new Batch();
            }
            else if (currentBatch.batchType == BatchType.Path) { }

            else {
                PooledMesh mesh = uiforiaMeshPool.Get(); // todo -- maybe worth trying to find a large mesh
                int vertexCount = positionList.size;
                int triangleCount = triangleList.size;
                mesh.SetVertices(positionList.array, vertexCount);
                mesh.SetTextureCoord0(texCoordList0.array, vertexCount);
                mesh.SetTextureCoord1(texCoordList1.array, vertexCount);
                mesh.SetTriangles(triangleList.array, triangleCount);

                positionList.size = 0;
                texCoordList0.size = 0;
                texCoordList1.size = 0;
                triangleList.size = 0;
                currentBatch.pooledMesh = mesh;
                pendingBatches.Add(currentBatch);

                renderCommandList.Add(new RenderOperation(pendingBatches.size - 1));
                currentBatch = new Batch();
            }

            // todo -- only set for enabled channels
//            mesh.SetVertices(positionList.array, vertexCount);
//            mesh.SetNormals(normalList.array, vertexCount);
//            mesh.SetColors(colorList.array, vertexCount);
//            mesh.SetTextureCoord0(texCoordList0.array, vertexCount);
//            mesh.SetTextureCoord1(texCoordList1.array, vertexCount);
//            mesh.SetTextureCoord2(texCoordList2.array, vertexCount);
//            mesh.SetTextureCoord3(texCoordList3.array, vertexCount);
//            mesh.SetTriangles(triangleList.array, triangleCount);
//
//            positionList.size = 0;
//            normalList.size = 0;
//            colorList.size = 0;
//            texCoordList0.size = 0;
//            texCoordList1.size = 0;
//            texCoordList2.size = 0;
//            texCoordList3.size = 0;
//            triangleList.size = 0;
//
//
//            currentBatch.material = cloneMaterial ? activeMaterial.Clone() : activeMaterial;
//            currentBatch.mesh = mesh;
//
//            pendingBatches.Add(currentBatch);
//
//            materialsToRelease.Add(activeMaterial);
//
//            currentBatch = new Batch();
        }

        public void Render(Camera camera, CommandBuffer commandBuffer) {
            commandBuffer.Clear();
            if (camera != null && camera.targetTexture != null) {
                RenderTexture targetTexture = camera.targetTexture;
                defaultRTFormat = targetTexture.format;
                defaultRTDepth = targetTexture.depth;
                defaultRTDesc = targetTexture.descriptor;
            }

#if DEBUG
            commandBuffer.BeginSample("UIForia Render Main");
#endif
            FinalizeCurrentBatch(false);


            ProcessDrawCommands(camera, commandBuffer);

#if DEBUG

            commandBuffer.EndSample("UIForia Render Main");
#endif

            // Graphics.ExecuteCommandBuffer(commandBuffer);
        }

        public void PushClip(Rect clipRect) {
            // todo -- transform
            if (clipStack.size > 0) {
                clipRect = Extensions.RectExtensions.Intersect(clipStack.array[clipStack.size - 1], clipRect);
            }

            clipStack.Push(clipRect);
        }

        public void PopClip() {
            clipStack.Pop();
        }

        public void Clear() {
            currentBatch = new Batch();

            for (int i = 0; i < pendingBatches.size; i++) {
                pendingBatches[i].pooledMesh.Release();
                // pendingBatches[i].uiforiaData?.Release();
            }

            for (int i = 0; i < scratchTextures.size; i++) {
                // todo -- pool the packer
                RenderTexture.ReleaseTemporary(scratchTextures[i].renderTexture);
            }

            if (pingPongTexture != null) {
                RenderTexture.ReleaseTemporary(pingPongTexture);
                pingPongTexture = null;
            }

            renderCommandList.QuickClear();
            scratchTextures.QuickClear();
            pendingBatches.QuickClear();
        }

        private struct ScratchRenderTexture {

            public RenderTexture renderTexture;
            public SimpleRectPacker packer;

        }

        public void PushPostEffect(Material material, Vector2 position, Size size) {
            SimpleRectPacker packer = null;
            RenderTexture renderTexture = null;
            SimpleRectPacker.PackedRect rect = default;

            for (int i = 0; i < scratchTextures.size; i++) {
                if (scratchTextures.array[i].packer.TryPackRect((int) size.width, (int) size.height, out rect)) {
                    packer = scratchTextures.array[i].packer;
                    renderTexture = scratchTextures.array[i].renderTexture;
                    break;
                }
            }

            // todo -- do not allocate

            if (packer == null) {
                packer = new SimpleRectPacker(Screen.width, Screen.height, 5);

                if (!packer.TryPackRect((int) size.width, (int) size.height, out rect)) {
                    throw new Exception($"Cannot fit size {size} in a render texture. Max texture size is {s_MaxTextureSize}");
                }

                renderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, defaultRTDepth, RenderTextureFormat.DefaultHDR);
                scratchTextures.Add(new ScratchRenderTexture() {
                    packer = packer,
                    renderTexture = renderTexture
                });
            }

            
            renderCommandList.Add(new RenderOperation() {
                operationType = RenderOperationType.PushRenderTexture,
                renderTexture = renderTexture,
                rect = rect
            });
        }

        public void PopPostEffect() {
            
            FinalizeCurrentBatch(false);
            
            EffectData effectData = effectStack.PopUnchecked();

            currentBatch.material = effectData.material;
            currentBatch.batchType = BatchType.Custom;
            currentBatch.drawCallSize = 1;
            
            renderCommandList.Add(new RenderOperation() {
                operationType = RenderOperationType.BlitRenderTexture,
                batchIndex = pendingBatches.size
            });
            
            pendingBatches.Add(currentBatch);
            currentBatch = new Batch();
            
        }

        private struct EffectData {

            public Material material;
            public MaterialPropertyBlock block;
            public Rect rect;

        }
        
        private void ProcessDrawCommands(Camera camera, CommandBuffer commandBuffer) {
            Matrix4x4 cameraMatrix = camera.cameraToWorldMatrix;
            commandBuffer.SetViewProjectionMatrices(cameraMatrix, camera.projectionMatrix);

            RenderOperation[] renderCommands = this.renderCommandList.array;
            int commandCount = renderCommandList.size;

            StructStack<RenderArea> rtStack = StructStack<RenderArea>.Get();

            rtStack.Push(new RenderArea(null, default));
            
            // assert camera & has texture

            Vector3 cameraOrigin = camera.transform.position;
            cameraOrigin.x -= 0.5f * Screen.width;
            cameraOrigin.y += (0.5f * Screen.height);
            cameraOrigin.z += 2;

            Matrix4x4 origin = Matrix4x4.TRS(cameraOrigin, Quaternion.identity, Vector3.one);

            Batch[] batches = pendingBatches.array;

            for (int i = 0; i < commandCount; i++) {
                ref RenderOperation cmd = ref renderCommands[i];

                switch (cmd.operationType) {
                    case RenderOperationType.DrawBatch:

                        ref Batch batch = ref batches[cmd.batchIndex];
                      
                        if (batch.batchType == BatchType.UIForia) {
                            UIForiaPropertyBlock uiForiaPropertyBlock = uiforiaMaterialPool.GetPropertyBlock(batch.drawCallSize);

                            uiForiaPropertyBlock.SetData(batch.uiforiaData);

                            commandBuffer.DrawMesh(batch.pooledMesh.mesh, origin, uiForiaPropertyBlock.material, 0, 0, uiForiaPropertyBlock.matBlock);
                        }
                        else if (batch.batchType == BatchType.Custom) {
                            commandBuffer.DrawMesh(batch.pooledMesh.mesh, origin, batch.material, 0, 0, null);
                        }

                        break;

                    case RenderOperationType.PushRenderTexture:

                        if (rtStack.array[rtStack.size - 1].renderTexture != cmd.renderTexture) {
                            // todo -- figure out the weirdness with perspective or view when texture is larger than camera texture
                            commandBuffer.SetRenderTarget(cmd.renderTexture);
                            int width = cmd.renderTexture.width / 2;
                            int height = cmd.renderTexture.height / 2;
                            Matrix4x4 projection = Matrix4x4.Ortho(-width, width, -height, height, 0.1f, 9999);
                            commandBuffer.SetViewProjectionMatrices(cameraMatrix, projection);
                        }

                        // always push so pop will pop the right texture, duplicate refs are ok
                        rtStack.Push(new RenderArea(cmd.renderTexture, cmd.rect));
                        break;

                    case RenderOperationType.ClearRenderTextureRegion:
                        break;

                    case RenderOperationType.BlitRenderTexture:

                        // pop texture
                        // blit to next one up the stack
                        // some platforms can't use CopyTexture. Need a shader for that

                        RenderArea area = rtStack.PopUnchecked();
                        RenderArea next = rtStack.PeekUnchecked();
                        RenderTexture rt = area.renderTexture;

                        int srcWidth = area.renderArea.xMax - area.renderArea.xMin;
                        int srcHeight = area.renderArea.yMax - area.renderArea.yMin;

                        int srcX = area.renderArea.xMin;
                        int srcY = rt.height - srcHeight;
                        int dstX = 0; // todo -- need to figure out where this goes, maybe part of the push?
                        int dstY = rt.height - srcHeight;

                        if (next.renderTexture == rt) {
                            if (pingPongTexture == null) {
                                pingPongTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.DefaultHDR);
                            }
                            commandBuffer.CopyTexture(rt, 0, 0, srcX, srcY, srcWidth, srcHeight, pingPongTexture, 0, 0, dstX, dstY);
                            commandBuffer.CopyTexture(pingPongTexture, 0, 0, srcX, srcY, srcWidth, srcHeight, rt, 0, 0, dstX, dstY);
                        }
                        else {

                            if (next.renderTexture == null) {
                                commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                                commandBuffer.CopyTexture(rt, 0, 0, srcX, srcY, srcWidth, srcHeight, BuiltinRenderTextureType.CurrentActive, 0, 0, dstX + 100, dstY - 100);
                            }
                            else {
                                commandBuffer.CopyTexture(rt, 0, 0, srcX, srcY, srcWidth, srcHeight, next.renderTexture, 0, 0, dstX, dstY);
                            }
                            
                        }

                        break;

                    case RenderOperationType.SetScissorRect:
                        break;

                    case RenderOperationType.SetCameraViewMatrix:
                        break;

                    case RenderOperationType.SetCameraProjectionMatrix:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            StructStack<RenderArea>.Release(ref rtStack);
        }

        private struct RenderArea {

            public readonly SimpleRectPacker.PackedRect renderArea;
            public readonly RenderTexture renderTexture;
            
            public RenderArea(RenderTexture renderTexture, SimpleRectPacker.PackedRect renderArea) {
                this.renderTexture = renderTexture;
                this.renderArea = renderArea;
            }

        }
    }

}