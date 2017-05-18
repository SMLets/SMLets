using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SMLets.MPBMaker
{
    class FileInformation
    {
        public string FileName
        {
            get
            {
                return _file.Name;
            }
        }
        public string FullPath
        {
            get
            {
                return _file.FullName;
            }
        }

        public System.IO.FileInfo Info
        {
            get
            {
                return _file;
            }
        }

        private System.IO.FileInfo _file;

        public FileInformation(string FilePath)
        {
            _file = new System.IO.FileInfo(FilePath);
            if (!_file.Exists)
            {
                throw new System.IO.FileNotFoundException("File not found", FilePath);
            }
        }
    }
}
