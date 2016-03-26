using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Mood
{
    public class TaskbarManager
    {
        public static string PathToTaskbar = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");

        public static string PathToSaves = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            @"dashji\mood");

        public TaskbarManager()
        {
            if (!Directory.Exists(PathToSaves))
            {
                Directory.CreateDirectory(PathToSaves);
            }
        }

        public void List()
        {
            Console.WriteLine("Files in {0}:", PathToSaves);
            Console.WriteLine();

            foreach (string s in Directory.GetFiles(PathToSaves))
            {
                Console.WriteLine(Path.GetFileName(s));
            }
        }

        public void Remove(string file, bool verbose)
        {
            if (!Path.IsPathRooted(file))
            {
                if (verbose)
                    Console.WriteLine("Local path, changing directory to {0}...", PathToSaves);

                file = Path.Combine(PathToSaves, file);
            }

            if (File.Exists(file))
            {
                File.Delete(file);

                if (verbose)
                    Console.WriteLine("File removed.");
            }
            else if (verbose)
                Console.WriteLine("File does not exist.");
        }

        public void LoadFrom(string file, bool verbose, bool killExplorer, bool restartExplorer)
        {
            if (!Path.IsPathRooted(file))
            {
                if (verbose)
                    Console.WriteLine("Local path, changing directory to {0}...", PathToSaves);

                file = Path.Combine(PathToSaves, file);
            }

            if (!File.Exists(file))
            {
                Console.WriteLine("Cannot find file {0}.", file);
                return;
            }

            if (killExplorer)
            {
                if (verbose) Console.WriteLine("Killing explorer.exe...");
                foreach (Process p in Process.GetProcessesByName("explorer"))
                    p.Kill();
            }

            using (FileStream fs = File.OpenRead(file))
            {
                // read metadata
                fs.Seek(0, SeekOrigin.Begin);
                byte[] metadata = new byte[2048];
                fs.Read(metadata, 0, 2048);

                int[] lengths = Encoding.UTF8.GetString(metadata).Split(';').Select(x => int.Parse(x)).ToArray();

                // remove old shortcuts
                if (verbose) Console.WriteLine("Removing old shortcuts...");
                foreach (string s in Directory.GetFiles(PathToTaskbar))
                {
                    File.Delete(s);
                }

                // load shortcuts
                if (verbose) Console.WriteLine("Loading shortcuts...");
                int i = 0;
                while (lengths[i] != 0)
                {
                    byte[] filename = new byte[lengths[i]];
                    byte[] data = new byte[lengths[i + 1]];

                    fs.Read(filename, 0, lengths[i]);
                    fs.Read(data, 0, lengths[i + 1]);

                    File.WriteAllBytes(Path.Combine(PathToTaskbar, Encoding.UTF8.GetString(filename) + ".lnk"), data);

                    i += 2;
                }

                // read registry values
                if (verbose) Console.WriteLine("Restoring registry values...");
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband", true);

                key.SetValue("Favorites", readBytes(fs, lengths[++i]));
                key.SetValue("FavoritesResolve", readBytes(fs, lengths[++i]));

                key.SetValue("FavoritesChanges", readUint(fs, lengths[++i]));
                key.SetValue("FavoritesRemovedChanges", readUint(fs, lengths[++i]));
                key.SetValue("FavoritesVersion", readUint(fs, lengths[++i]));
            }

            if (verbose) Console.WriteLine("Done loading.");

            if (restartExplorer)
            {
                if (verbose) Console.WriteLine("Restoring explorer.exe...");
                Process.Start("explorer.exe");
            }
        }

        public void SaveTo(string file, bool verbose)
        {
            if (!Path.IsPathRooted(file))
            {
                if (verbose)
                    Console.WriteLine("Local path, changing directory to {0}...", PathToSaves);

                file = Path.Combine(PathToSaves, file);
            }

            using (FileStream fs = File.OpenWrite(file))
            {
                List<int> lengths = new List<int>();

                // save space for metadata
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(new byte[2048], 0, 2048);

                // save shortcuts
                if (verbose) Console.WriteLine("Saving shortcuts...");
                foreach (string s in Directory.GetFiles(PathToTaskbar))
                {
                    byte[] data = Encoding.UTF8.GetBytes(Path.GetFileNameWithoutExtension(s));
                    fs.Write(data, 0, data.Length);
                    lengths.Add(data.Length);

                    data = File.ReadAllBytes(s);
                    fs.Write(data, 0, data.Length);
                    lengths.Add(data.Length);
                }

                lengths.Add(0);

                // write registry values
                if (verbose) Console.WriteLine("Saving registry values...");
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband", false);

                lengths.Add(write(fs, (byte[])key.GetValue("Favorites")));
                lengths.Add(write(fs, (byte[])key.GetValue("FavoritesResolve")));

                lengths.Add(write(fs, (int)key.GetValue("FavoritesChanges")));
                lengths.Add(write(fs, (int)key.GetValue("FavoritesRemovedChanges")));
                lengths.Add(write(fs, (int)key.GetValue("FavoritesVersion")));

                // write metadata
                if (verbose) Console.WriteLine("Writing metadata...");
                byte[] metadata = Encoding.UTF8.GetBytes(string.Join(";", lengths.Select(x => x.ToString()).ToArray()));
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(metadata, 0, metadata.Length);
            }
            
            if (verbose)
                Console.WriteLine("Current taskbar saved to {0}.", file);
        }

        private int write(Stream s, string str)
        {
            byte[] data = Encoding.UTF8.GetBytes(str);
            s.Write(data, 0, data.Length);
            return data.Length;
        }

        private int write(Stream s, byte[] data)
        {
            s.Write(data, 0, data.Length);
            return data.Length;
        }

        private int write(Stream s, int val)
        {
            byte[] data = BitConverter.GetBytes(val);
            s.Write(data, 0, data.Length);
            return data.Length;
        }

        private byte[] readBytes(Stream s, int length)
        {
            byte[] data = new byte[length];
            s.Read(data, 0, data.Length);
            return data;
        }

        private int readUint(Stream s, int length)
        {
            byte[] data = new byte[length];
            s.Read(data, 0, data.Length);
            return BitConverter.ToInt32(data, 0);
        }
    }
}
