using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace FileSizer
{
    class Folder
    {
        public struct FileData
        {
            public string name;
            public string path;
            public string ext;
            public long size;
        }

        private long size = 0;
        private List<Folder> childs;
        private List<FileData> files;
        private Folder parent;
        private string path;
        private bool loading = true;

        public Folder(Folder parent, string path)
        {
            this.parent = parent;
            this.path = path;
            childs = new List<Folder>();
            files = new List<FileData>();
        }

        public long FindChilds()
        {
            childs.Clear();
            files.Clear();
            long totalSize = 0;
            try
            {
                foreach (string f in Directory.GetDirectories(path))
                {
                    Folder folder = new Folder(this, f);
                    lock (childs)
                    {
                        childs.Add(folder);
                    }
                    totalSize += folder.FindChilds();
                }

                
                foreach (string f in Directory.GetFiles(path))
                {
                    FileInfo fileInfo = new FileInfo(f);
                    lock (files)
                    {
                        try
                        {
                            FileData file = new FileData();
                            file.name = fileInfo.Name;
                            file.path = fileInfo.FullName;
                            file.ext = fileInfo.Extension;
                            file.size = fileInfo.Length;
                            files.Add(file);
                            totalSize += file.size;
                        }
                        catch (FileNotFoundException e)
                        {
                            Console.WriteLine("The file: \"" + f + "\" not found");
                        }
                    }
                }
                
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine("The folder: \"" + path + "\" not found");
            }
            catch (UnauthorizedAccessException e)
            {
                //Console.WriteLine("Can't access path: " + path);
            }
            loading = false;
            size = totalSize;
            return totalSize;
        }

        public Folder GetFolder(string name)
        {
            lock (childs)
            {
                foreach (Folder f in childs)
                {
                    if (name.Equals(f.path.Substring(f.path.LastIndexOf('\\') + 1)))
                    {
                        return f;
                    }
                }
            }
            
            return null;
        }

        public string[] GetFileInfo()
        {
            string[] info;
            lock (childs)
            {
                lock (files)
                {
                    info = new string[childs.Count + files.Count];
                    int i = 0;
                    foreach (Folder f in childs)
                    {
                        info[i++] = "Dir\t" + f.path.Substring(f.path.LastIndexOf('\\') + 1)
                            + "\t" + SizeToString(f.GetSize());
                    }
                    foreach (FileData f in files)
                    {
                        info[i++] = f.ext + "\t" + f.name;
                    }
                }
            }
            return info;
        }

        private string SizeToString(long size)
        {
            string suffix = "B";
            if (size >= 1000000000000)
            {
                suffix = "T" + suffix;
                size /= 1000000000000;
            }
            else if (size >= 1000000000)
            {
                suffix = "G" + suffix;
                size /= 1000000000;
            }
            else if (size >= 1000000)
            {
                suffix = "M" + suffix;
                size /= 1000000;
            }
            else if (size >= 1000)
            {
                suffix = "K" + suffix;
                size /= 1000;
            }
            return size + suffix;
        }

        public Folder GetParent()
        {
            return parent;
        }

        public string GetPath()
        {
            return path;
        }

        public long GetSize()
        {
            return size;
        }

        public bool IsLoading()
        {
            return loading;
        }
    }
}
