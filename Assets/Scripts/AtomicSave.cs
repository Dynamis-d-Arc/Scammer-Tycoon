using System.IO;
using System.Text;

namespace ScammerTown
{
    public static class AtomicSave
    {
        public static void Write(string path, string json)
        {
            string temp=path+".tmp";
            // Keep staging on the same filesystem and flush before atomic replacement.
            byte[] bytes=Encoding.UTF8.GetBytes(json);
            using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
            {
                stream.Write(bytes,0,bytes.Length);
                stream.Flush(true);
            }
            if(File.Exists(path))File.Replace(temp,path,path+".bak");
            else File.Move(temp,path);
        }
    }
}
