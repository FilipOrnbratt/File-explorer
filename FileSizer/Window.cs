using System;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;

namespace FileSizer
{
    class Window : Form
    {
        private const int DRAWX = 10, DRAWY = 80, DRAWYOFFSET = 20, DRAWWIDTH = 1000,
            DRAWHEIGHT = 16, MAXFILES = 25, SORTX = 5, SORTY = 30, REFRESHX = 800, REFRESHY = 30;
        private Button sortByNameButton, sortBySizeButton, sortByTypeButton, refreshButton, backButton;
        private Folder root;
        private string currentPath, lastPath;
        private string[] files;
        private Folder currentFolder;
        private int scroll, maxScroll;
        private bool scrollBarPressed;
        private Folder.SortStatus sortStatus;
        private Folder.SortStatus lastSortStatus;

        public static void Main()
        {
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                Folder drive = new Folder(null, d.Name);
                new Window(drive);
            }
        }

        public Window(Folder drive)
        {
            files = new string[0];

            sortStatus = Folder.SortStatus.SIZE;
            lastSortStatus = sortStatus;

            root = drive;
            Thread driveThread = new Thread(new ParameterizedThreadStart(StartDriveSearch));
            driveThread.Start(drive);

            new Thread(Init).Start();
            new Thread(UpdateFilesPeriodically).Start();
        }

        private void StartDriveSearch(object folder)
        {
            ((Folder)folder).SearchFolder();
            bool drivesStillSearching = false;
            foreach (Folder drive in root.GetFolders())
            {
                if (drive.GetStatus() == Folder.FileStatus.LOADING)
                {
                    drivesStillSearching = true;
                }
            }
            if (!drivesStillSearching)
            {
                root.SetStatus(Folder.FileStatus.DONE);
            }
        }

        private void Init()
        {
            InitButtons();
            this.DoubleBuffered = true;
            this.Size = new Size(1280, 720);
            this.Paint += Draw;
            this.MouseDown += MouseDownEvent;
            this.MouseUp += MouseUpEvent;
            this.MouseMove += MouseMoveEvent;
            this.FormClosed += CloseEvent;
            this.MouseWheel += MouseWheelEvent;
            ShowDialog();
        }

        private void InitButtons()
        {
            backButton = new Button(new Rectangle(5, 10, 40, 20), "Back", Color.Red, true);
            sortBySizeButton = new Button(new Rectangle(SORTX + 60, SORTY, 35, 20), "Size", Color.Green, false, Color.Gray);
            sortByNameButton = new Button(new Rectangle(SORTX + 60 + 40, SORTY, 45, 20), "Name", Color.Green, false, Color.Gray);
            sortByTypeButton = new Button(new Rectangle(SORTX + 60 + 40 + 50, SORTY, 40, 20), "Type", Color.Green, false, Color.Gray);
            refreshButton = new Button(new Rectangle(REFRESHX, REFRESHY, 60, 20), "Refresh", Color.Green, false, Color.Gray);
        }

        private void UpdateFilesPeriodically()
        {
            currentFolder = root;
            currentPath = currentFolder.GetPath();
            lastPath = currentPath;
            while (true)
            {
                UpdateFiles();
                Thread.Sleep(1000);
            }
        }

        private void UpdateFiles()
        {
            if(currentFolder != null)
            {
                lock (files)
                {
                    string[] currentFiles = currentFolder.GetFileInfo(sortStatus);
                    if (!files.SequenceEqual<string>(currentFiles) || lastSortStatus != sortStatus)
                    {
                        files = currentFiles;
                        UpdateScroll();
                        UpdateButtons();
                        lastSortStatus = sortStatus;
                        Repaint();
                    }
                }
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
            {}
        }

        private void Draw(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            backButton.Draw(g);
            refreshButton.Draw(g);
            if (currentFolder != null)
            {
                string statusMessage = "";
                switch (currentFolder.GetStatus())
                {
                    case Folder.FileStatus.LOADING:
                        statusMessage = "Loading files...";
                        break;
                    case Folder.FileStatus.DONE:
                        statusMessage = "";
                        break;

                }
                g.DrawString(statusMessage, new Font(FontFamily.GenericSerif, 12), new SolidBrush(Color.Red),
                   new PointF(REFRESHX + 70, REFRESHY));
            }

            g.DrawString("Sort by:", new Font(FontFamily.GenericSerif, 12), new SolidBrush(Color.Black),
                new PointF(SORTX, SORTY));
            sortBySizeButton.Draw(g);
            sortByNameButton.Draw(g);
            sortByTypeButton.Draw(g);

            g.DrawString(currentPath, new Font(FontFamily.GenericSerif, 20), new SolidBrush(Color.Black), 
                new PointF(50, 2));
            g.DrawString("Type\tSize\tName", new Font(FontFamily.GenericSerif, 12), new SolidBrush(Color.Black), 
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
            if(e.Delta > 0 && scroll > 0)
            {
                scroll--;
            }
            else if (e.Delta < 0 && scroll < maxScroll)
            {
                scroll++;
            }
            Repaint();
        }

        private void CloseEvent(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void MouseDownEvent(object sender, MouseEventArgs e)
        {
            Folder folder;
            if ((e.Button == System.Windows.Forms.MouseButtons.Right || 
                backButton.IsPressed(e.X, e.Y)) && backButton.IsActivated())
            {
                folder = currentFolder.GetParent();
                if (folder != null)
                {
                    currentFolder = folder;
                    currentPath = currentFolder.GetPath();
                    refreshButton.SetActivated(false);
                    UpdateFiles();
                }
            }
            else if (sortBySizeButton.IsPressed(e.X, e.Y) && sortBySizeButton.IsActivated())
            {
                sortStatus = Folder.SortStatus.SIZE;
                UpdateFiles();
            }
            else if (sortByNameButton.IsPressed(e.X, e.Y) && sortByNameButton.IsActivated())
            {
                sortStatus = Folder.SortStatus.NAME;
                UpdateFiles();
            }
            else if (sortByTypeButton.IsPressed(e.X, e.Y) && sortByTypeButton.IsActivated())
            {
                sortStatus = Folder.SortStatus.TYPE;
                UpdateFiles();
            }
            else if (refreshButton.IsPressed(e.X, e.Y) && refreshButton.IsActivated())
            {
                new Thread(RefreshCurrentFolder).Start();
                UpdateFiles();
            }
            else if (new Rectangle(DRAWX + DRAWWIDTH + 10,
                    DRAWY + (int)(((double)scroll / (double)maxScroll) * (DRAWYOFFSET * MAXFILES
                    - (int)(((double)MAXFILES / (double)files.Length) * DRAWYOFFSET * MAXFILES))), 20,
                    (int)(((double)MAXFILES / (double)files.Length) * DRAWYOFFSET * MAXFILES)).Contains(e.X, e.Y) 
                    && files.Length > MAXFILES)
            {
                scrollBarPressed = true; 
            }
            else
            {
                lock (files)
                {
                    for (int i = scroll; i < Math.Min(files.Length, MAXFILES + scroll); i++)
                    {
                        bool folderPressed = e.X > DRAWX && e.X < DRAWX + DRAWWIDTH && e.Y > DRAWY + DRAWYOFFSET * (i - scroll) &&
                            e.Y < DRAWY + DRAWYOFFSET * (i - scroll) + DRAWHEIGHT;
                        if (folderPressed)
                        {
                            folder = currentFolder.GetFolder(files[i].Split('\t')[2]);
                            if (folder != null)
                            {
                                currentPath = folder.GetPath();
                                currentFolder = folder;
                                UpdateFiles();
                            }
                            break;
                        }
                    }
                }
            }
            
        }

        private void MouseUpEvent(object sender, MouseEventArgs e)
        {
            if (scrollBarPressed)
            {
                scrollBarPressed = false;
            }
        }

        private void MouseMoveEvent(object sender, MouseEventArgs e)
        {
            if (scrollBarPressed)
            {
                int scrollValue = (int)(((double)(e.Y - DRAWY ) - (((double)MAXFILES / (double)files.Length) 
                    * DRAWYOFFSET * MAXFILES) / 2) / ((double)(DRAWYOFFSET * MAXFILES) / maxScroll));
                scroll = Math.Min(Math.Max(scrollValue + scrollValue, 0), maxScroll);
                Repaint();
            }
        }

        private void RefreshCurrentFolder()
        {
            long originalSize = currentFolder.GetSize();
            long newSize = currentFolder.SearchFolder();
            Folder parent = currentFolder.GetParent();
            if (parent != null)
            {
                currentFolder.GetParent().UpdateParentWithSize(currentFolder.GetPath(), originalSize);
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

        private void UpdateButtons()
        {
            if (currentFolder.GetStatus() == Folder.FileStatus.DONE)
            {
                refreshButton.SetActivated(true);
            }
            else
            {
                refreshButton.SetActivated(false);
            }
            sortBySizeButton.SetActivated(sortStatus != Folder.SortStatus.SIZE);
            sortByNameButton.SetActivated(sortStatus != Folder.SortStatus.NAME);
            sortByTypeButton.SetActivated(sortStatus != Folder.SortStatus.TYPE);
        }
    }
}
