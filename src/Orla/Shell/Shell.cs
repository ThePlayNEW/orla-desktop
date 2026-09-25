using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Orla
{
    // Files, folders and virtual locations as Explorer presents them: localized names, native icons and
    // thumbnails, opening, revealing and native file operations.
    public static class Shell
    {
        public const string DesktopFolder = "desktop";

        static readonly string[] virtualPaths = { "shell:MyComputerFolder", "shell:RecycleBinFolder", "shell:Downloads",
                                                   "shell:Personal", "shell:ControlPanelFolder", "shell:NetworkPlacesFolder" };

        public static bool isVirtual(string path) => path != null && path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase);

        public static bool exists(string path)
        {
            if (isVirtual(path))
                return virtualPaths.Contains(path, StringComparer.OrdinalIgnoreCase);
            return File.Exists(path) || Directory.Exists(path);
        }

        // Whether a path is a folder or something inside it, ignoring case and trailing separators.
        public static bool within(string path, string folder)
        {
            path = path.TrimEnd('\\');
            folder = folder.TrimEnd('\\');
            return path.Equals(folder, StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(folder + "\\", StringComparison.OrdinalIgnoreCase);
        }

        public static string RecentFolder => Environment.GetFolderPath(Environment.SpecialFolder.Recent);

        public static bool isRecent(string folderPath) =>
            !String.IsNullOrEmpty(folderPath) && !isVirtual(folderPath) && folderPath != DesktopFolder && within(folderPath, RecentFolder) &&
            within(RecentFolder, folderPath);

        public static bool isHidden(string path)
        {
            try
            {
                return (File.GetAttributes(path) & (FileAttributes.Hidden | FileAttributes.System)) != 0;
            }
            catch (Exception)
            {
                return true;
            }
        }

        // The user's desktop and the shared public desktop, which Windows shows together.
        public static string[] desktopDirectories()
        {
            return new[] { Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                           Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory) }
                .Where(d => !String.IsNullOrEmpty(d) && Directory.Exists(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static string[] folderDirectories(string folderPath)
        {
            if (folderPath == DesktopFolder)
                return desktopDirectories();
            string resolved = resolveFolder(folderPath);
            return resolved != null && Directory.Exists(resolved) ? new[] { resolved } : new string[0];
        }

        public static string resolveFolder(string folderPath)
        {
            if (folderPath == DesktopFolder)
                return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (String.Equals(folderPath, "shell:Downloads", StringComparison.OrdinalIgnoreCase))
                return downloads();
            return folderPath;
        }

        public static string downloads()
        {
            try
            {
                var id = new Guid("374DE290-123F-4565-9164-39C4925E467B");
                if (SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out IntPtr p) == 0)
                    try
                    {
                        return Marshal.PtrToStringUni(p);
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(p);
                    }
            }
            catch (EntryPointNotFoundException)
            {
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }

        // Visible entries of a folder panel, folders first, the way Explorer sorts by name.
        public static List<string> list(string folderPath)
        {
            var result = new List<string>();
            foreach (string directory in folderDirectories(folderPath))
            {
                try
                {
                    foreach (string path in Directory.EnumerateFileSystemEntries(directory))
                    {
                        FileAttributes a;
                        try
                        {
                            a = File.GetAttributes(path);
                        }
                        catch (IOException)
                        {
                            continue;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            continue;
                        }
                        if ((a & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                            result.Add(path);
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
            // Windows' recent items: newest first and only the latest few, as in File Explorer's Recent list.
            if (isRecent(folderPath))
                return result.Where(p => !Directory.Exists(p)).OrderByDescending(File.GetLastWriteTimeUtc).Take(40).ToList();
            return result.OrderBy(p => Directory.Exists(p) ? 0 : 1)
                .ThenBy(p => Path.GetFileName(p), StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public static string displayName(string path)
        {
            try
            {
                IShellItem item = createItem(path);
                try
                {
                    if (item.GetDisplayName(SIGDN_NORMALDISPLAY, out IntPtr name) == 0)
                        try
                        {
                            return Marshal.PtrToStringUni(name);
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(name);
                        }
                }
                finally
                {
                    Marshal.ReleaseComObject(item);
                }
            }
            catch (Exception)
            {
            }
            return Directory.Exists(path) ? new DirectoryInfo(path).Name : Path.GetFileNameWithoutExtension(path);
        }

        public static void open(string path)
        {
            if (isVirtual(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
                return;
            }
            if (!exists(path))
                throw new FileNotFoundException(Text.get("error.missing"));
            Process.Start(new ProcessStartInfo(path) {
                UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(path) ?? ""
            });
        }

        public static void reveal(string path)
        {
            if (isVirtual(path) || Directory.Exists(path) && !File.Exists(path) && path.Length <= 3)
                open(path);
            else if (exists(path))
                Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"") { UseShellExecute = true });
            else
                throw new FileNotFoundException(Text.get("error.missing"));
        }

        // Moves or copies files into a folder with Windows' own progress, conflict and undo handling.
        public static bool transfer(IntPtr owner, IEnumerable<string> paths, string folder, bool copy)
        {
            var sources = paths.Where(exists).ToList();
            if (sources.Count == 0 || !Directory.Exists(folder))
                return false;
            var op = new SHFILEOPSTRUCT {
                hwnd = owner, wFunc = copy ? FO_COPY : FO_MOVE, pFrom = String.Join("\0", sources) + "\0\0",
                pTo = folder + "\0\0", fFlags = FOF_ALLOWUNDO
            };
            return SHFileOperation(ref op) == 0 && !op.fAnyOperationsAborted;
        }

        public static bool sameVolume(string a, string b)
        {
            try
            {
                return String.Equals(Path.GetPathRoot(Path.GetFullPath(a)), Path.GetPathRoot(Path.GetFullPath(b)),
                                     StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ---- icons and thumbnails, loaded on a background STA thread ----

        static readonly BlockingCollection<Action> iconQueue = new BlockingCollection<Action>();
        static readonly Dictionary<string, ImageSource> iconCache = new Dictionary<string, ImageSource>();
        static Thread iconThread;

        public static void icon(string path, int pixels, Dispatcher dispatcher, Action<ImageSource> done)
        {
            startIconThread();
            if (path == null)
                return;
            string key = pixels + "|" + path;
            lock (iconCache)
                if (iconCache.TryGetValue(key, out ImageSource cached))
                {
                    done(cached);
                    return;
                }
            iconQueue.Add(delegate {
                ImageSource image = null;
                try
                {
                    image = loadImage(path, pixels);
                }
                catch (Exception)
                {
                }
                if (image != null)
                    lock (iconCache)
                    {
                        // Bounded: start over rather than grow without limit on very large folders.
                        if (iconCache.Count >= 1500)
                            iconCache.Clear();
                        iconCache[key] = image;
                    }
                dispatcher.BeginInvoke(done, DispatcherPriority.Background, image);
            });
        }

        // Explorer's display name (localized, extension hidden when Windows hides it), resolved off the UI thread.
        public static void name(string path, Dispatcher dispatcher, Action<string> done)
        {
            startIconThread();
            iconQueue.Add(delegate {
                string value = displayName(path);
                dispatcher.BeginInvoke(done, DispatcherPriority.Background, value);
            });
        }

        static void startIconThread()
        {
            if (iconThread != null)
                return;
            iconThread = new Thread(delegate() {
                foreach (Action work in iconQueue.GetConsumingEnumerable())
                    work();
            }) { IsBackground = true, Name = "Orla icons" };
            iconThread.SetApartmentState(ApartmentState.STA);
            iconThread.Start();
        }

        public static void forget(string path)
        {
            lock (iconCache)
                foreach (string key in iconCache.Keys.Where(k => k.EndsWith("|" + path, StringComparison.OrdinalIgnoreCase)).ToList())
                    iconCache.Remove(key);
        }

        static ImageSource loadImage(string path, int pixels)
        {
            IShellItem item = createItem(path);
            IntPtr bitmap = IntPtr.Zero;
            try
            {
                var factory = (IShellItemImageFactory)item;
                if (factory.GetImage(new SIZE { cx = pixels, cy = pixels }, SIIGBF_RESIZETOFIT, out bitmap) != 0)
                    return null;
                return toBitmapSource(bitmap);
            }
            finally
            {
                if (bitmap != IntPtr.Zero)
                    Native.DeleteObject(bitmap);
                Marshal.ReleaseComObject(item);
            }
        }

        static BitmapSource toBitmapSource(IntPtr hbitmap)
        {
            GetObject(hbitmap, Marshal.SizeOf(typeof(BITMAP)), out BITMAP bm);
            int w = bm.bmWidth, h = Math.Abs(bm.bmHeight);
            var header = new BITMAPINFOHEADER {
                biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER)), biWidth = w, biHeight = -h, biPlanes = 1,
                biBitCount = 32
            };
            var pixels = new byte[w * h * 4];
            IntPtr dc = CreateCompatibleDC(IntPtr.Zero);
            try
            {
                if (GetDIBits(dc, hbitmap, 0, (uint)h, pixels, ref header, 0) == 0)
                    return null;
            }
            finally
            {
                DeleteDC(dc);
            }
            // The shell usually hands back straight (not premultiplied) alpha; reading it as premultiplied leaves a pale,
            // jagged rim around icons. A colour brighter than its alpha can only happen with straight alpha.
            bool straight = false, anyAlpha = false;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                anyAlpha |= pixels[i + 3] != 0;
                straight |= Math.Max(pixels[i], Math.Max(pixels[i + 1], pixels[i + 2])) > pixels[i + 3];
            }
            // Some thumbnail providers leave alpha at zero everywhere: those images are opaque.
            PixelFormat format = !anyAlpha ? PixelFormats.Bgr32 : straight ? PixelFormats.Bgra32 : PixelFormats.Pbgra32;
            BitmapSource source = BitmapSource.Create(w, h, 96, 96, format, null, pixels, w * 4);
            source.Freeze();
            return source;
        }

        static IShellItem createItem(string path)
        {
            var id = typeof(IShellItem).GUID;
            SHCreateItemFromParsingName(path, IntPtr.Zero, ref id, out IShellItem item);
            return item;
        }

        const uint SIGDN_NORMALDISPLAY = 0;
        const int SIIGBF_RESIZETOFIT = 0x0;
        const uint FO_MOVE = 1, FO_COPY = 2;
        const ushort FOF_ALLOWUNDO = 0x40;

        [ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellItem
        {
            [PreserveSig]
            int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            [PreserveSig]
            int GetParent(out IShellItem parent);
            [PreserveSig]
            int GetDisplayName(uint sigdn, out IntPtr name);
            [PreserveSig]
            int GetAttributes(uint mask, out uint attributes);
            [PreserveSig]
            int Compare(IShellItem other, uint hint, out int order);
        }

        [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(SIZE size, int flags, out IntPtr bitmap);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SIZE
        {
            public int cx, cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct BITMAP
        {
            public int bmType, bmWidth, bmHeight, bmWidthBytes;
            public ushort bmPlanes, bmBitsPixel;
            public IntPtr bmBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth, biHeight;
            public ushort biPlanes, biBitCount;
            public uint biCompression, biSizeImage;
            public int biXPelsPerMeter, biYPelsPerMeter;
            public uint biClrUsed, biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom, pTo;
            public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        static extern void SHCreateItemFromParsingName(string path, IntPtr pbc, ref Guid riid,
                                                       [MarshalAs(UnmanagedType.Interface)] out IShellItem item);
        [DllImport("shell32.dll")]
        static extern int SHGetKnownFolderPath(ref Guid id, uint flags, IntPtr token, out IntPtr path);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHFileOperation(ref SHFILEOPSTRUCT op);
        [DllImport("gdi32.dll")]
        static extern int GetObject(IntPtr h, int size, out BITMAP bitmap);
        [DllImport("gdi32.dll")]
        static extern int GetDIBits(IntPtr dc, IntPtr bitmap, uint start, uint lines, byte[] bits,
                                    ref BITMAPINFOHEADER info, uint usage);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr dc);
    }
}
