using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using SharpMetal.Foundation;
using SharpMetal.Metal;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace Ryujinx.Graphics.Metal
{
    [SupportedOSPlatform("macos")]
    class TextureBuffer : ITexture, IDisposable
    {
        private readonly TextureCreateInfo _info;
        private readonly Pipeline _pipeline;
        private readonly MTLDevice _device;

        private MTLBuffer? _bufferHandle;
        private int _offset;
        private int _size;

        public MTLTexture? MTLTexture;
        public TextureCreateInfo Info => _info;
        public int Width => Info.Width;
        public int Height => Info.Height;
        public int Depth => Info.Depth;

        public TextureBuffer(MTLDevice device, Pipeline pipeline, TextureCreateInfo info)
        {
            _device = device;
            _pipeline = pipeline;
            _info = info;
        }

        public void CreateView()
        {
            Console.WriteLine($"Creating texture buffer view -> pixelFormat: {FormatTable.GetFormat(Info.Format)} width: {Info.Width}, height: {Info.Height}, depth: {Info.Depth}");
            var descriptor = new MTLTextureDescriptor
            {
                PixelFormat = FormatTable.GetFormat(Info.Format),
                Usage = MTLTextureUsage.ShaderRead | MTLTextureUsage.ShaderWrite,
                StorageMode = MTLStorageMode.Shared,
                TextureType = Info.Target.Convert(),
                Width = (ulong)Info.Width,
                Height = (ulong)Info.Height
            };

            MTLTexture = _bufferHandle.Value.NewTexture(descriptor, (ulong)_offset, (ulong)_size);
        }

        public void CopyTo(ITexture destination, int firstLayer, int firstLevel)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(ITexture destination, int srcLayer, int dstLayer, int srcLevel, int dstLevel)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(ITexture destination, Extents2D srcRegion, Extents2D dstRegion, bool linearFilter)
        {
            throw new NotSupportedException();
        }

        public ITexture CreateView(TextureCreateInfo info, int firstLayer, int firstLevel)
        {
            throw new NotSupportedException();
        }

        // TODO: Implement this method
        public PinnedSpan<byte> GetData()
        {
            throw new NotImplementedException();
        }

        public PinnedSpan<byte> GetData(int layer, int level)
        {
            return GetData();
        }

        public void CopyTo(BufferRange range, int layer, int level, int stride)
        {
            throw new NotImplementedException();
        }

        public void Release()
        {
            Dispose();
        }

        public void Dispose()
        {
            MTLTexture?.Dispose();
        }

        public void SetData(IMemoryOwner<byte> data)
        {
            // TODO
            //_gd.SetBufferData(_bufferHandle, _offset, data.Memory.Span);
            data.Dispose();
        }

        public void SetData(IMemoryOwner<byte> data, int layer, int level)
        {
            throw new NotSupportedException();
        }

        public void SetData(IMemoryOwner<byte> data, int layer, int level, Rectangle<int> region)
        {
            throw new NotSupportedException();
        }

        public void SetStorage(BufferRange buffer)
        {
            if (buffer.Handle != BufferHandle.Null)
            {
                var handle = buffer.Handle;
                MTLBuffer bufferHandle = new(Unsafe.As<BufferHandle, IntPtr>(ref handle));
                if (_bufferHandle == bufferHandle &&
                    _offset == buffer.Offset &&
                    _size == buffer.Size)
                {
                    return;
                }

                _bufferHandle = bufferHandle;
                _offset = buffer.Offset;
                _size = buffer.Size;

                Release();

                CreateView();
            }
        }
    }
}
