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

        public long RefreshFolder()
        {
            ClearFolder();
            return SearchFolder();
        }

        public void ClearFolder()
        {
            status = FileStatus.LOADING;
            subFolders.Clear();
            files.Clear();
            size = 0;
        }

        public long SearchFolder()
        {
            long totalSize = 0;
            try
            {
                foreach (string folderPath in Directory.GetDirectories(path))
                {
                    Folder folder = new Folder(this, folderPath);
                    FileInfo folderInfo = new FileInfo(folderPath);

                    lock (subFolders)
                    {
                        subFolders.Add(folder);
                    }
                    totalSize += folder.RefreshFolder();
                }

                
                foreach (string filePath in Directory.GetFiles(path))
                {
                    FileInfo fileInfo = new FileInfo(filePath);

                    lock (files)
                    {
                        try
                        {
                            FileData file = new FileData
                            {
                                name = fileInfo.Name,
                                path = fileInfo.FullName,
                                ext = fileInfo.Extension,
                                size = fileInfo.Length
                            };
                            files.Add(file);
                            totalSize += file.size;
                        }
                        catch (FileNotFoundException e)
                        {
                            Console.WriteLine("The file: \"" + filePath + "\" not found");
                        }
                    }
                }
                
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine("The folder: \"" + path + "\" not found");
            }
            catch (UnauthorizedAccessException e) {}

            size = totalSize;
            status = FileStatus.DONE;
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
            string[] fileInfoText;
            lock (subFolders)
            {
                lock (files)
                {
                    fileInfoText = new string[subFolders.Count + files.Count];
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
                            fileInfoText[i++] = file.ext + "\t" + SizeToString(file.size) + "\t" + file.name;
                        }
                    }
                    else
                    {
                        foreach (Folder folder in subFolders)
                        {
                            fileInfoText[i++] = "Dir\t" + SizeToString(folder.GetSize()) + "\t" + GetNameFromPath(folder.path);
                        }
                        foreach (FileData file in files)
                        {
                            fileInfoText[i++] = file.ext + "\t" + SizeToString(file.size) + "\t" + file.name;
                        }
                    }
                }
            }
            return fileInfoText;
        }

        private List<FileData> ConvertFoldersToFileData(List<Folder> folders)
        {
            List<FileData> fileDataList = new List<FileData>();
            foreach (Folder folder in folders)
            {
                FileData filedata = new FileData
                {
                    name = GetNameFromPath(folder.path),
                    path = folder.GetPath(),
                    ext = "Dir",
                    size = folder.GetSize()
                };
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

            string[] suffixes = { "K", "M", "G", "T" };
            for (int i = 0;  size >= 1000 && i < suffixes.Length; i++)
            {
                size /= 1000;
                suffix = suffixes[i];
            }

            return size + suffix;
        }

        public void UpdateParentWithSize(long diff)
        {
            size += diff;

            if (parentFolder != null)
            {
                parentFolder.UpdateParentWithSize(diff);
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
