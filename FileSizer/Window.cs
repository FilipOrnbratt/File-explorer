using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;

namespace FileSizer
{
    class Window : Form
    {
        private const int DRAWX = 10, DRAWY = 52, DRAWYOFFSET = 20, DRAWWIDTH = 1000, 
            DRAWHEIGHT = 16, MAXFILES = 20;
        private List<Folder> drives;
        private string currentPath, lastPath;
        private string[] files;
        private string[] fileNames;
        private Folder currentFolder;
        private int scroll, maxScroll;

        public static void Main()
        {
            new Window();
        }

        public Window()
        {
            files = new string[0];
            fileNames = new string[0];
            drives = new List<Folder>();
            new Thread(init).Start();
            new Thread(UpdateFiles).Start();
            int i = 0;
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                if (d.DriveType == DriveType.Fixed)
                {
                    Folder folder = new Folder(null, d.Name);
                    drives.Add(folder);
                    folder.FindChilds();
                }
            }
            Console.WriteLine("All files loaded");
        }

        private void init()
        {
            this.DoubleBuffered = true;
            this.Size = new Size(1280, 720);
            this.Paint += Draw;
            this.MouseClick += MouseEvent;
            this.FormClosed += CloseEvent;
            this.MouseWheel += MouseWheelEvent;
            ShowDialog();
        }

        private void UpdateFiles()
        {
            while(drives.Count == 0)
            {
                Thread.Sleep(100);
            }
            currentFolder = drives[0];
            currentPath = currentFolder.GetPath().Substring(0, currentFolder.GetPath().Length - 1);
            lastPath = currentPath;
            while (true)
            {
                if(currentFolder != null)
                {
                    lock (files)
                    {
                        if(!files.SequenceEqual<string>(currentFolder.GetFileInfo()))
                        {
                            files = currentFolder.GetFileInfo();
                            UpdateScroll();
                        }
                    }
                    Repaint();
                }
                Thread.Sleep(500);
            }
        }

        private void Repaint()
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    Refresh();
                });
            }
            catch (InvalidOperationException e)
            {

            }
        }

        private void Draw(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.FillRectangle(new SolidBrush(Color.Red), new Rectangle(10, 2, 30, 20));
            if (currentFolder != null)
            {
                g.DrawString(currentFolder.IsLoading() ? "Loading files..." : "", new Font(FontFamily.GenericSerif, 12), new SolidBrush(Color.Red),
                   new PointF(600, 40));
            }
            
            g.DrawString(currentPath, new Font(FontFamily.GenericSerif, 20), new SolidBrush(Color.Black), 
                new PointF(50, 2));
            g.DrawString("Type\tName\t\t\tSize", new Font(FontFamily.GenericSerif, 12), new SolidBrush(Color.Black), 
                new PointF(DRAWX - 2, DRAWY - 4 - DRAWYOFFSET));
            g.FillRectangle(new SolidBrush(Color.Gray), new Rectangle(DRAWX + DRAWWIDTH + 10, DRAWY, 20,
                DRAWYOFFSET * MAXFILES));
            if(files.Length > MAXFILES)
            {
                g.FillRectangle(new SolidBrush(Color.Aqua), new Rectangle(DRAWX + DRAWWIDTH + 10,
                    DRAWY + (int)(((double)scroll / (double)maxScroll) * (DRAWYOFFSET * MAXFILES
                    - (int)(((double)MAXFILES / (double)files.Length) * DRAWYOFFSET * MAXFILES))), 20,
                    (int)(((double)MAXFILES / (double)files.Length) * DRAWYOFFSET * MAXFILES)));
            }
            lock (files)
            {
                for (int i = scroll; i < Math.Min(files.Length, MAXFILES + scroll); i++)
                {
                    g.FillRectangle(new SolidBrush(Color.Aqua), 
                        new Rectangle(DRAWX, DRAWY + DRAWYOFFSET * (i - scroll), DRAWWIDTH, DRAWHEIGHT));
                    g.DrawString(files[i], new Font(FontFamily.GenericSerif, 12), 
                        new SolidBrush(Color.Black), new PointF(DRAWX - 2, DRAWY - 4 + DRAWYOFFSET * (i - scroll)));
                }
            }

        }

        private void MouseWheelEvent(object sender, MouseEventArgs e)
        {
            Console.WriteLine(e.Delta);
            if(e.Delta > 0 && scroll > 0)
            {
                scroll--;
            }else if (e.Delta < 0 && scroll < maxScroll)
            {
                scroll++;
            }
            Console.WriteLine(maxScroll + ": " + scroll);
        }

        private void CloseEvent(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void MouseEvent(object sender, MouseEventArgs e)
        {
            Folder folder;
            if (e.X > 10 && e.X < 40 && e.Y > 2 && e.Y < 22)
            {
                if ((folder = currentFolder.GetParent()) != null)
                {
                    currentPath = currentPath.Substring(0, currentPath.LastIndexOf('\\'));
                    currentFolder = folder;
                    Repaint();
                }
            }
            lock (files)
            {
                for (int i = scroll; i < Math.Min(files.Length, MAXFILES + scroll); i++)
                {
                    if (e.X > DRAWX && e.X < DRAWX + DRAWWIDTH && e.Y > DRAWY + DRAWYOFFSET * (i - scroll) && 
                        e.Y < DRAWY + DRAWYOFFSET * (i - scroll) + DRAWHEIGHT)
                    {
                        if((folder = currentFolder.GetFolder(files[i].Split('\t')[1])) != null)
                        {
                            currentPath = currentPath + "\\" + files[i].Split('\t')[1];
                            currentFolder = folder;
                            Repaint();
                        }
                        break;
                    }
                }
            }
        }

        private void UpdateScroll()
        {
            if(lastPath != currentPath)
            {
                scroll = 0;
                lastPath = currentPath;
            }
            maxScroll = Math.Max(files.Length - MAXFILES, 0);
        }
    }
}
