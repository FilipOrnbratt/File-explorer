using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public enum FileStatus
        {
            DONE,
            LOADING
        }

        public enum SortStatus
        {
            TYPE,
            NAME,
            SIZE
        }

        private long size = 0;
        private List<Folder> subFolders;
        private List<FileData> files;
        private Folder parentFolder;
        private string path;
        private FileStatus status = FileStatus.LOADING;

        public Folder(Folder parent, string path)
        {
            parentFolder = parent;
            this.path = path;
            subFolders = new List<Folder>();
            files = new List<FileData>();
        }

        public long SearchFolder()
        {
            status = FileStatus.LOADING;
            subFolders.Clear();
            files.Clear();
            long totalSize = 0;
            try
            {
                foreach (string f in Directory.GetDirectories(path))
                {
                    
                    Folder folder = new Folder(this, f);
                    FileInfo folderInfo = new FileInfo(f);
                    if (folderInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        continue;
                    }
                    lock (subFolders)
                    {
                        subFolders.Add(folder);
                    }
                    totalSize += folder.SearchFolder();
                }

                
                foreach (string f in Directory.GetFiles(path))
                {
                    FileInfo fileInfo = new FileInfo(f);
                    if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        continue;
                    }
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
            status = FileStatus.DONE;
            size = totalSize;
            return totalSize;
        }

        public Folder GetFolder(string name)
        {
            lock (subFolders)
            {
                foreach (Folder folder in subFolders)
                {
                    if (name.Equals(GetNameFromPath(folder.path)))
                    {
                        return folder;
                    }
                }
            }
            
            return null;
        }

        public List<Folder> GetFolders()
        {
            lock (subFolders)
            {
                return subFolders;
            }
        }

        public void AddFolder(Folder folder)
        {
            lock (subFolders)
            {
                subFolders.Add(folder);
            }
        }

        public string[] GetFileInfo(SortStatus sortStatus)
        {
            string[] info;
            lock (subFolders)
            {
                lock (files)
                {
                    info = new string[subFolders.Count + files.Count];
                    int i = 0;
                    if (sortStatus != SortStatus.TYPE)
                    {
                        List<FileData> sortFiles = new List<FileData>();
                        sortFiles.AddRange(files);
                        sortFiles.AddRange(ConvertFoldersToFileData(subFolders));
                        switch (sortStatus)
                        {
                            case SortStatus.SIZE:
                                sortFiles = sortFiles.OrderByDescending(x => x.size).ToList();
                                break;
                            case SortStatus.NAME:
                                sortFiles = sortFiles.OrderBy(x => x.name).ToList();
                                break;
                        }
                        foreach (FileData file in sortFiles)
                        {
                            info[i++] = file.ext + "\t" + SizeToString(file.size) + "\t" + file.name;
                        }
                    }
                    else
                    {
                        foreach (Folder folder in subFolders)
                        {
                            info[i++] = "Dir\t" + SizeToString(folder.GetSize()) + "\t" + GetNameFromPath(folder.path);
                        }
                        foreach (FileData file in files)
                        {
                            info[i++] = file.ext + "\t" + SizeToString(file.size) + "\t" + file.name;
                        }
                    }
                }
            }
            return info;
        }

        private List<FileData> ConvertFoldersToFileData(List<Folder> folders)
        {
            List<FileData> fileDataList = new List<FileData>();
            foreach (Folder folder in folders)
            {
                FileData filedata = new FileData();
                filedata.name = GetNameFromPath(folder.path);
                filedata.path = folder.GetPath();
                filedata.ext = "Dir";
                filedata.size = folder.GetSize();
                fileDataList.Add(filedata);
            }
            return fileDataList;
        }

        private string GetNameFromPath(string path)
        {
            string name = path.Substring(path.LastIndexOf('\\') + 1);
            if (name.Length == 0)
            {
                name = path.Substring(0, path.LastIndexOf('\\') + 1);
            }
            return name;
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

        public void UpdateParentWithSize(string folderPath, long oldSize)
        {
            Folder changedFolder = null;
            foreach (Folder folder in subFolders)
            {
                if (folder.path == folderPath)
                {
                    changedFolder = folder;
                    break;
                }
            }
            if (changedFolder == null)
            {
                return;
            }
            long diff = changedFolder.size - oldSize;
            if (diff == 0)
            {
                return;
            }
            long originalSize = size;
            size += diff;
            if (parentFolder != null)
            {
                parentFolder.UpdateParentWithSize(path, originalSize);
            }
        }

        public Folder GetParent()
        {
            return parentFolder;
        }

        public string GetPath()
        {
            return path;
        }

        public long GetSize()
        {
            return size;
        }

        public FileStatus GetStatus()
        {
            return status;
        }

        public void SetStatus(FileStatus status)
        {
            this.status = status;
        }
    }
}
