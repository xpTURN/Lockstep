using System;
using System.IO;
using System.Collections.Generic;

namespace Lockstep.Core.Impl
{
    /// <summary>
    /// MemoryStream and byte array pooling utility (GC prevention)
    /// </summary>
    public static class StreamPool
    {
        private static readonly Stack<MemoryStream> _streamPool = new Stack<MemoryStream>();
        private static readonly Stack<byte[]> _bufferPool = new Stack<byte[]>();
        
        private const int DEFAULT_BUFFER_SIZE = 4096;
        private const int MAX_POOL_SIZE = 16;
        
        #region MemoryStream Pool
        
        /// <summary>
        /// Get a MemoryStream from the pool
        /// </summary>
        public static MemoryStream GetStream()
        {
            lock (_streamPool)
            {
                if (_streamPool.Count > 0)
                {
                    var stream = _streamPool.Pop();
                    stream.SetLength(0);
                    stream.Position = 0;
                    return stream;
                }
            }
            return new MemoryStream(DEFAULT_BUFFER_SIZE);
        }
        
        /// <summary>
        /// Return a MemoryStream to the pool
        /// </summary>
        public static void ReturnStream(MemoryStream stream)
        {
            if (stream == null)
                return;
                
            lock (_streamPool)
            {
                if (_streamPool.Count < MAX_POOL_SIZE)
                {
                    stream.SetLength(0);
                    stream.Position = 0;
                    _streamPool.Push(stream);
                }
                // If pool is full, discard (let GC handle it)
            }
        }
        
        #endregion
        
        #region Buffer Pool
        
        /// <summary>
        /// Get a byte array from the pool (minimum size guaranteed)
        /// </summary>
        public static byte[] GetBuffer(int minSize)
        {
            lock (_bufferPool)
            {
                if (_bufferPool.Count > 0)
                {
                    var buffer = _bufferPool.Pop();
                    if (buffer.Length >= minSize)
                        return buffer;
                    // If size is insufficient, return to pool and create new
                    _bufferPool.Push(buffer);
                }
            }
            return new byte[System.Math.Max(minSize, DEFAULT_BUFFER_SIZE)];
        }
        
        /// <summary>
        /// Return a byte array to the pool
        /// </summary>
        public static void ReturnBuffer(byte[] buffer)
        {
            if (buffer == null || buffer.Length > DEFAULT_BUFFER_SIZE * 4)
                return; // Don't pool buffers that are too large
                
            lock (_bufferPool)
            {
                if (_bufferPool.Count < MAX_POOL_SIZE)
                {
                    _bufferPool.Push(buffer);
                }
            }
        }
        
        /// <summary>
        /// Copy MemoryStream contents to a new byte array (from pool)
        /// Note: Returned array should be returned via ReturnBuffer after use
        /// </summary>
        public static byte[] ToArrayPooled(MemoryStream stream)
        {
            int length = (int)stream.Length;
            byte[] result = GetBuffer(length);
            
            // Copy only actual data
            stream.Position = 0;
            stream.Read(result, 0, length);
            
            // If exact size array is needed, allocate new
            if (result.Length != length)
            {
                byte[] exact = new byte[length];
                Array.Copy(result, exact, length);
                ReturnBuffer(result);
                return exact;
            }
            
            return result;
        }
        
        /// <summary>
        /// Copy MemoryStream contents to a byte array (always exact size)
        /// Use when exact size is needed, such as snapshot storage
        /// </summary>
        public static byte[] ToArrayExact(MemoryStream stream)
        {
            int length = (int)stream.Length;
            byte[] result = new byte[length];
            
            stream.Position = 0;
            stream.Read(result, 0, length);
            
            return result;
        }
        
        #endregion
        
        /// <summary>
        /// Clear MemoryStream and Buffer pools (for testing)
        /// </summary>
        public static void Clear()
        {
            lock (_streamPool)
            {
                _streamPool.Clear();
            }
            lock (_bufferPool)
            {
                _bufferPool.Clear();
            }
        }
    }
    
    /// <summary>
    /// PooledMemoryStream - Use with using statement for automatic return
    /// </summary>
    public struct PooledMemoryStream : IDisposable
    {
        public MemoryStream Stream { get; private set; }
        private bool _disposed;
        
        public static PooledMemoryStream Create()
        {
            return new PooledMemoryStream
            {
                Stream = StreamPool.GetStream(),
                _disposed = false
            };
        }
        
        public void Dispose()
        {
            if (!_disposed && Stream != null)
            {
                StreamPool.ReturnStream(Stream);
                Stream = null;
                _disposed = true;
            }
        }
    }
}
