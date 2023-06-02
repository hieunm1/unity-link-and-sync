// Link & Sync // Copyright 2023 Kybernetik //

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using UnityEditor;
using UnityEngine;

namespace LinkAndSync
{
    public abstract class FileComparer
    {

        /// <summary>
        /// Fileinfo for source file
        /// </summary>
        protected readonly FileInfo FileInfo1;

        /// <summary>
        /// Fileinfo for target file
        /// </summary>
        protected readonly FileInfo FileInfo2;

        /// <summary>
        /// Base class for creating a file comparer
        /// </summary>
        /// <param name="filePath01">Absolute path to source file</param>
        /// <param name="filePath02">Absolute path to target file</param>
        protected FileComparer(string filePath01, string filePath02)
        {
            FileInfo1 = new FileInfo(filePath01);
            FileInfo2 = new FileInfo(filePath02);
            // EnsureFilesExist();
        }

        /// <summary>
        /// Compares the two given files and returns true if the files are the same
        /// </summary>
        /// <returns>true if the files are the same, false otherwise</returns>
        public bool Compare()
        {
            if (FileInfo1.Exists == false) return false;
            if (FileInfo1.Exists == false) return true;

            if (IsDifferentLength())
            {
                return false;
            }
            if (IsSameFile())
            {
                return true;
            }
            return OnCompare();
        }

        /// <summary>
        /// Compares the two given files and returns true if the files are the same
        /// </summary>
        /// <returns>true if the files are the same, false otherwise</returns>
        protected abstract bool OnCompare();

        private bool IsSameFile()
        {
            return string.Equals(FileInfo1.FullName, FileInfo2.FullName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Does an early comparison by checking files Length, if lengths are not the same, files are definetely different
        /// </summary>
        /// <returns>true if different length</returns>
        private bool IsDifferentLength()
        {
            return FileInfo1.Length != FileInfo2.Length;
        }

        /// <summary>
        /// Makes sure files exist
        /// </summary>
        private void EnsureFilesExist()
        {
            if (FileInfo1.Exists == false)
            {
                throw new ArgumentNullException(nameof(FileInfo1) + " " + FileInfo1?.FullName);
            }
            if (FileInfo2.Exists == false)
            {
                throw new ArgumentNullException(nameof(FileInfo2)+ " " + FileInfo2?.FullName);
            }
        }

    }
    public abstract class ReadIntoByteBufferInChunks : FileComparer
    {

        protected readonly int ChunkSize;

        protected ReadIntoByteBufferInChunks(string filePath01, string filePath02, int chunkSize) : base(filePath01, filePath02)
        {
            ChunkSize = chunkSize;
        }

        protected int ReadIntoBuffer(in Stream stream, in byte[] buffer)
        {
            var bytesRead = 0;
            while (bytesRead < buffer.Length)
            {
                var read = stream.Read(buffer, bytesRead, buffer.Length - bytesRead);
                // Reached end of stream.
                if (read == 0)
                {
                    return bytesRead;
                }
                bytesRead += read;
            }
            return bytesRead;
        }

    }


    public class ReadFileInChunksAndCompareVector : ReadIntoByteBufferInChunks
    {
        public ReadFileInChunksAndCompareVector(string filePath01, string filePath02, int chunkSize)
            : base(filePath01, filePath02, chunkSize)
        {
        }

        protected override bool OnCompare()
        {
            return StreamAreEqual(FileInfo1.OpenRead(), FileInfo2.OpenRead());
        }

        private bool StreamAreEqual(in Stream stream1, in Stream stream2)
        {
            var buffer1 = new byte[ChunkSize];
            var buffer2 = new byte[ChunkSize];

            while (true)
            {
                var count1 = ReadIntoBuffer(stream1, buffer1);
                var count2 = ReadIntoBuffer(stream2, buffer2);

                if (count1 != count2)
                {
                    return false;
                }

                if (count1 == 0)
                {
                    return true;
                }

                var totalProcessed = 0;
                while (totalProcessed < buffer1.Length)
                {
                    if (Vector.EqualsAll(new Vector<byte>(buffer1, totalProcessed), new Vector<byte>(buffer2, totalProcessed)) == false)
                    {
                        return false;
                    }
                    totalProcessed += Vector<byte>.Count;
                }
            }
        }

        public static bool IsSame(string filePath01, string filePath02, int chunkSize = 131072)
        {
            return new ReadFileInChunksAndCompareVector(filePath01, filePath02, chunkSize).Compare();
        }
        public static bool IsDiff(string filePath01, string filePath02, int chunkSize = 131072)
        {
            return !IsSame(filePath01, filePath02, chunkSize);
        }
    }
}