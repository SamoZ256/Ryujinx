using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using SharpMetal.Foundation;
using SharpMetal.Metal;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace Ryujinx.Graphics.Metal
{
    [SupportedOSPlatform("macos")]
    public class Pipeline : IPipeline, IDisposable
    {
        private readonly MTLDevice _device;
        private readonly MTLCommandQueue _mtlCommandQueue;

        private MTLCommandBuffer _commandBuffer;
        private MTLCommandEncoder _currentEncoder;

        public MTLCommandEncoder CurrentEncoder;

        private RenderEncoderState _renderEncoderState;

        private MTLBuffer _indexBuffer;
        private MTLIndexType _indexType;
        private ulong _indexBufferOffset;

        public Pipeline(MTLDevice device, MTLCommandQueue commandQueue)
        {
            _device = device;
            _mtlCommandQueue = commandQueue;

            var renderPipelineDescriptor = new MTLRenderPipelineDescriptor();
            var error = new NSError(IntPtr.Zero);
            _renderEncoderState = new(_device.NewRenderPipelineState(renderPipelineDescriptor, ref error));
            if (error != IntPtr.Zero)
            {
                Logger.Error?.PrintMsg(LogClass.Gpu, $"Failed to create Render Pipeline State: {StringHelper.String(error.LocalizedDescription)}");
            }

            _commandBuffer = _mtlCommandQueue.CommandBuffer();
        }

        public void EndCurrentPass()
        {
            if (_currentEncoder != null)
            {
                _currentEncoder.EndEncoding();
                _currentEncoder = null;
            }
        }

        public MTLRenderCommandEncoder BeginRenderPass()
        {
            EndCurrentPass();

            var descriptor = new MTLRenderPassDescriptor { };
            var renderCommandEncoder = _commandBuffer.RenderCommandEncoder(descriptor);
            _renderEncoderState.SetEncoderState(renderCommandEncoder);

            _currentEncoder = renderCommandEncoder;
            return renderCommandEncoder;
        }

        public MTLBlitCommandEncoder BeginBlitPass()
        {
            EndCurrentPass();

            var descriptor = new MTLBlitPassDescriptor { };
            var blitCommandEncoder = _commandBuffer.BlitCommandEncoder(descriptor);

            _currentEncoder = blitCommandEncoder;
            return blitCommandEncoder;
        }

        public void Present()
        {
            EndCurrentPass();

            // TODO: Give command buffer a valid MTLDrawable
            // _commandBuffer.PresentDrawable();
            _commandBuffer.Commit();

            _commandBuffer = _mtlCommandQueue.CommandBuffer();
        }

        public void Barrier()
        {
            throw new NotImplementedException();
        }

        public void ClearBuffer(BufferHandle destination, int offset, int size, uint value)
        {
            MTLBlitCommandEncoder blitCommandEncoder;

            if (_currentEncoder is MTLBlitCommandEncoder encoder)
            {
                blitCommandEncoder = encoder;
            }
            else
            {
                blitCommandEncoder = BeginBlitPass();
            }

            // Might need a closer look, range's count, lower, and upper bound
            // must be a multiple of 4
            MTLBuffer mtlBuffer = new(Unsafe.As<BufferHandle, IntPtr>(ref destination));
            blitCommandEncoder.FillBuffer(mtlBuffer,
                new NSRange
                {
                    location = (ulong)offset,
                    length = (ulong)size
                },
                (byte)value);
        }

        public void ClearRenderTargetColor(int index, int layer, int layerCount, uint componentMask, ColorF color)
        {
            throw new NotImplementedException();
        }

        public void ClearRenderTargetDepthStencil(int layer, int layerCount, float depthValue, bool depthMask, int stencilValue,
            int stencilMask)
        {
            throw new NotImplementedException();
        }

        public void CommandBufferBarrier()
        {
            // TODO: Only required for MTLHeap or untracked resources
        }

        public void CopyBuffer(BufferHandle source, BufferHandle destination, int srcOffset, int dstOffset, int size)
        {
            throw new NotImplementedException();
        }

        public void DispatchCompute(int groupsX, int groupsY, int groupsZ)
        {
            throw new NotImplementedException();
        }

        public void Draw(int vertexCount, int instanceCount, int firstVertex, int firstInstance)
        {
            MTLRenderCommandEncoder renderCommandEncoder;

            if (_currentEncoder is MTLRenderCommandEncoder encoder)
            {
                renderCommandEncoder = encoder;
            }
            else
            {
                renderCommandEncoder = BeginRenderPass();
            }

            // TODO: Support topology re-indexing to provide support for TriangleFans
            var primitiveType = _renderEncoderState.Topology.Convert();

            renderCommandEncoder.DrawPrimitives(primitiveType, (ulong)firstVertex, (ulong)vertexCount, (ulong)instanceCount, (ulong)firstInstance);
        }

        public void DrawIndexed(int indexCount, int instanceCount, int firstIndex, int firstVertex, int firstInstance)
        {
            MTLRenderCommandEncoder renderCommandEncoder;

            if (_currentEncoder is MTLRenderCommandEncoder encoder)
            {
                renderCommandEncoder = encoder;
            }
            else
            {
                renderCommandEncoder = BeginRenderPass();
            }

            // TODO: Support topology re-indexing to provide support for TriangleFans
            var primitiveType = _renderEncoderState.Topology.Convert();

            renderCommandEncoder.DrawIndexedPrimitives(primitiveType, (ulong)indexCount, _indexType, _indexBuffer, _indexBufferOffset, (ulong)instanceCount, firstVertex, (ulong)firstInstance);
        }

        public void DrawIndexedIndirect(BufferRange indirectBuffer)
        {
            throw new NotImplementedException();
        }

        public void DrawIndexedIndirectCount(BufferRange indirectBuffer, BufferRange parameterBuffer, int maxDrawCount, int stride)
        {
            throw new NotImplementedException();
        }

        public void DrawIndirect(BufferRange indirectBuffer)
        {
            throw new NotImplementedException();
        }

        public void DrawIndirectCount(BufferRange indirectBuffer, BufferRange parameterBuffer, int maxDrawCount, int stride)
        {
            throw new NotImplementedException();
        }

        public void DrawTexture(ITexture texture, ISampler sampler, Extents2DF srcRegion, Extents2DF dstRegion)
        {
            throw new NotImplementedException();
        }

        public void SetAlphaTest(bool enable, float reference, CompareOp op)
        {
            // Metal does not support alpha test.
        }

        public void SetBlendState(AdvancedBlendDescriptor blend)
        {
            throw new NotImplementedException();
        }

        public void SetBlendState(int index, BlendDescriptor blend)
        {
            throw new NotImplementedException();
        }

        public void SetDepthBias(PolygonModeMask enables, float factor, float units, float clamp)
        {
            throw new NotImplementedException();
        }

        public void SetDepthClamp(bool clamp)
        {
            throw new NotImplementedException();
        }

        public void SetDepthMode(DepthMode mode)
        {
            throw new NotImplementedException();
        }

        public void SetDepthTest(DepthTestDescriptor depthTest)
        {
            throw new NotImplementedException();
        }

        public void SetFaceCulling(bool enable, Face face)
        {
            var cullMode = enable ? face.Convert() : MTLCullMode.None;

            if (_currentEncoder is MTLRenderCommandEncoder renderCommandEncoder)
            {
                renderCommandEncoder.SetCullMode(cullMode);
            }

            _renderEncoderState.CullMode = cullMode;
        }

        public void SetFrontFace(FrontFace frontFace)
        {
            var winding = frontFace.Convert();

            if (_currentEncoder is MTLRenderCommandEncoder renderCommandEncoder)
            {
                renderCommandEncoder.SetFrontFacingWinding(winding);
            }

            _renderEncoderState.Winding = winding;
        }

        public void SetIndexBuffer(BufferRange buffer, IndexType type)
        {
            if (buffer.Handle != BufferHandle.Null)
            {
                _indexType = type.Convert();
                _indexBufferOffset = (ulong)buffer.Offset;
                var handle = buffer.Handle;
                _indexBuffer = new(Unsafe.As<BufferHandle, IntPtr>(ref handle));
            }
        }

        public void SetImage(int binding, ITexture texture, Format imageFormat)
        {
            throw new NotImplementedException();
        }

        public void SetLineParameters(float width, bool smooth)
        {
            // Not supported in Metal
        }

        public void SetLogicOpState(bool enable, LogicalOp op)
        {
            // Not supported in Metal
        }

        public void SetMultisampleState(MultisampleDescriptor multisample)
        {
            throw new NotImplementedException();
        }

        public void SetPatchParameters(int vertices, ReadOnlySpan<float> defaultOuterLevel, ReadOnlySpan<float> defaultInnerLevel)
        {
            throw new NotImplementedException();
        }

        public void SetPointParameters(float size, bool isProgramPointSize, bool enablePointSprite, Origin origin)
        {
            throw new NotImplementedException();
        }

        public void SetPolygonMode(PolygonMode frontMode, PolygonMode backMode)
        {
            // Not supported in Metal
        }

        public void SetPrimitiveRestart(bool enable, int index)
        {
            // TODO: Supported for LineStrip and TriangleStrip
            // https://github.com/gpuweb/gpuweb/issues/1220#issuecomment-732483263
            // https://developer.apple.com/documentation/metal/mtlrendercommandencoder/1515520-drawindexedprimitives
            // https://stackoverflow.com/questions/70813665/how-to-render-multiple-trianglestrips-using-metal
        }

        public void SetPrimitiveTopology(PrimitiveTopology topology)
        {
            _renderEncoderState.Topology = topology;
        }

        public void SetProgram(IProgram program)
        {
            throw new NotImplementedException();
        }

        public void SetRasterizerDiscard(bool discard)
        {
            throw new NotImplementedException();
        }

        public void SetRenderTargetColorMasks(ReadOnlySpan<uint> componentMask)
        {
            throw new NotImplementedException();
        }

        public void SetRenderTargets(ITexture[] colors, ITexture depthStencil)
        {
            throw new NotImplementedException();
        }

        public unsafe void SetScissors(ReadOnlySpan<Rectangle<int>> regions)
        {
            // TODO: Test max allowed scissor rects on device
            var mtlScissorRects = new MTLScissorRect[regions.Length];

            for (int i = 0; i < regions.Length; i++)
            {
                var region = regions[i];
                mtlScissorRects[i] = new MTLScissorRect
                {
                    height = (ulong)region.Height,
                    width = (ulong)region.Width,
                    x = (ulong)region.X,
                    y = (ulong)region.Y
                };
            }

            fixed (MTLScissorRect* pMtlScissorRects = mtlScissorRects)
            {
                // TODO: Fix this function which currently wont accept pointer as intended
                // _renderCommandEncoder.SetScissorRects(pMtlScissorRects, regions.Length);
            }
        }

        public void SetStencilTest(StencilTestDescriptor stencilTest)
        {
            // TODO
        }

        public void SetStorageBuffers(ReadOnlySpan<BufferAssignment> buffers)
        {
            throw new NotImplementedException();
        }

        public void SetTextureAndSampler(ShaderStage stage, int binding, ITexture texture, ISampler sampler)
        {
            throw new NotImplementedException();
        }

        public void SetUniformBuffers(ReadOnlySpan<BufferAssignment> buffers)
        {
            throw new NotImplementedException();
        }

        public void SetUserClipDistance(int index, bool enableClip)
        {
            throw new NotImplementedException();
        }

        public void SetVertexAttribs(ReadOnlySpan<VertexAttribDescriptor> vertexAttribs)
        {
            throw new NotImplementedException();
        }

        public void SetVertexBuffers(ReadOnlySpan<VertexBufferDescriptor> vertexBuffers)
        {
            throw new NotImplementedException();
        }

        public unsafe void SetViewports(ReadOnlySpan<Viewport> viewports)
        {
            // TODO: Test max allowed viewports on device
            var mtlViewports = new MTLViewport[viewports.Length];

            for (int i = 0; i < viewports.Length; i++)
            {
                var viewport = viewports[i];
                mtlViewports[i] = new MTLViewport
                {
                    originX = viewport.Region.X,
                    originY = viewport.Region.Y,
                    width = viewport.Region.Width,
                    height = viewport.Region.Height,
                    znear = viewport.DepthNear,
                    zfar = viewport.DepthFar
                };
            }

            fixed (MTLViewport* pMtlViewports = mtlViewports)
            {
                // TODO: Fix this function which currently wont accept pointer as intended
                // _renderCommandEncoder.SetViewports(pMtlViewports, viewports.Length);
            }
        }

        public void TextureBarrier()
        {
            throw new NotImplementedException();
        }

        public void TextureBarrierTiled()
        {
            throw new NotImplementedException();
        }

        public bool TryHostConditionalRendering(ICounterEvent value, ulong compare, bool isEqual)
        {
            // TODO: Implementable via indirect draw commands
            return false;
        }

        public bool TryHostConditionalRendering(ICounterEvent value, ICounterEvent compare, bool isEqual)
        {
            // TODO: Implementable via indirect draw commands
            return false;
        }

        public void EndHostConditionalRendering()
        {
            // TODO: Implementable via indirect draw commands
        }

        public void BeginTransformFeedback(PrimitiveTopology topology)
        {
            // Metal does not support Transform Feedback
        }

        public void EndTransformFeedback()
        {
            // Metal does not support Transform Feedback
        }

        public void SetTransformFeedbackBuffers(ReadOnlySpan<BufferRange> buffers)
        {
            // Metal does not support Transform Feedback
        }

        public void Dispose()
        {

        }
    }
}
