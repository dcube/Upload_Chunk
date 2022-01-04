using System;

namespace VideoChunk.Domain
{
    public class FileMessage
    {
        public Guid? FileId { get; set; }
        public string FileName { get; set; }
    }
}
