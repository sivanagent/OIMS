using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.IO;

namespace OIMS
{
    public class FileWrite
    {
        private String fileName;
        private FileStream fs;
        private StreamWriter sw; 

        public FileWrite(String name)
        {
            fileName = name;
            fs = new FileStream(fileName, FileMode.Append, FileAccess.Write);
            sw = new StreamWriter(fs);
        }

        ~FileWrite()
        {
            sw.Close();
            fs.Close();
            sw.Dispose();
            fs.Dispose();
        }

        public void WriteData(String strdata)
        {
            sw.WriteLine(strdata);
            sw.Flush();
        }
    }//end of class filewrite

}//end of OIMS
