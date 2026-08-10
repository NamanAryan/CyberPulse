using System;

namespace CyberPulseAdministration.Models
{
    public class HrFile
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; } // 'pdf', 'word', 'video', 'photo'
        public DateTime UploadDate { get; set; }
        public long FileSize { get; set; }
    }
}
