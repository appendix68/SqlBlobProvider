﻿using EPiServer.Framework.Blobs;
using System;
using System.IO;

namespace EPiCode.SqlBlobProvider
{
    public class SqlBlob : Blob
    {
        public string FilePath { get; internal set; }
        public bool LoadFromDisk { get; internal set; }
        public SqlBlob(Uri id, string filePath,bool loadFromDisk)
            : base(id)
        {
            FilePath = filePath;
            LoadFromDisk = loadFromDisk;
        }

        public override Stream OpenRead()
        {
            if (!LoadFromDisk)
                return new MemoryStream(SqlBlobModelRepository.Get(ID).Blob);
            return FileHelper.GetOrCreateFileBlob(FilePath, ID);
        }

        public override Stream OpenWrite()
        {
            string tempFileName = Path.GetTempFileName();
            var trackableStream = new TrackableStream(new FileStream(tempFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None));
            trackableStream.Closing += delegate(object source, EventArgs e)
            {
                var innerStream = ((TrackableStream)source).InnerStream;
                innerStream.Seek(0L, SeekOrigin.Begin);
                Write(innerStream);
            };
            trackableStream.Closed += delegate
            {
                var fileInfo = new FileInfo(tempFileName);
                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }
            };
            return trackableStream;
        }
        public override void Write(Stream stream)
        {
            SqlBlobModel blobModel;
            if ((blobModel = SqlBlobModelRepository.Get(ID)) == null)
            {
                blobModel = new SqlBlobModel
                {
                    BlobId = ID
                };
            }
            var sqlBlobModel = blobModel;
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                sqlBlobModel.Blob = memoryStream.ToArray();
            }
            SqlBlobModelRepository.Save(sqlBlobModel);
        }
    }
}
