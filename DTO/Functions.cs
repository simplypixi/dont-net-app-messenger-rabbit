using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DTO
{
    public static class Functions
    {
        public static void saveFile(string file, string message)
        {
            if (!File.Exists(file))
            {
                FileStream aFile = new FileStream(file, FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(aFile);
                sw.WriteLine(message);
                sw.Close();
                aFile.Close();
            }
            else
            {
                FileStream aFile = new FileStream(file, FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(aFile);
                sw.WriteLine(message);
                sw.Close();
                aFile.Close();
            }
        }
    }
}
