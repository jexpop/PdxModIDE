using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PdxModIDE.IO
{
    public static class FileOperations
    {
        public static void EnsureDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public static void CopyFile(string sourcePath, string destinationPath)
        {
            var dir = Path.GetDirectoryName(destinationPath);
            if (dir != null) EnsureDirectory(dir);
            File.Copy(sourcePath, destinationPath, true);
        }

        public static void CopyFilePreserveTimestamps(string sourcePath, string destinationPath)
        {
            var dir = Path.GetDirectoryName(destinationPath);
            if (dir != null) EnsureDirectory(dir);
            File.Copy(sourcePath, destinationPath, true);

            var creationTime = File.GetCreationTime(sourcePath);
            var lastAccessTime = File.GetLastAccessTime(sourcePath);
            var lastWriteTime = File.GetLastWriteTime(sourcePath);

            File.SetCreationTime(destinationPath, creationTime);
            File.SetLastAccessTime(destinationPath, lastAccessTime);
            File.SetLastWriteTime(destinationPath, lastWriteTime);
        }

        public static string RenameExistingFile(string path)
        {
            if (!File.Exists(path))
                return path;

            string dir = Path.GetDirectoryName(path) ?? ".";
            string nameWithoutExt = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);

            int version = 1;
            string newPath;
            do
            {
                newPath = Path.Combine(dir, $"{nameWithoutExt}_v{version}{ext}");
                version++;
            } while (File.Exists(newPath));

            File.Move(path, newPath);
            return newPath;
        }

        public static bool FilesAreEqual(string path1, string path2)
        {
            if (!File.Exists(path1) || !File.Exists(path2))
                return false;

            var fi1 = new FileInfo(path1);
            var fi2 = new FileInfo(path2);
            if (fi1.Length != fi2.Length)
                return false;

            return File.ReadAllBytes(path1).AsSpan().SequenceEqual(File.ReadAllBytes(path2));
        }

        public static string ReadTextFile(string path)
        {
            try
            {
                return File.ReadAllText(path, Encoding.UTF8);
            }
            catch (DecoderFallbackException)
            {
                return File.ReadAllText(path, Encoding.GetEncoding("iso-8859-1"));
            }
        }

        public static string[] ReadTextLines(string path)
        {
            try
            {
                return File.ReadAllLines(path, Encoding.UTF8);
            }
            catch (DecoderFallbackException)
            {
                return File.ReadAllLines(path, Encoding.GetEncoding("iso-8859-1"));
            }
        }

        public static void WriteTextFile(string path, string content)
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null) EnsureDirectory(dir);
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }
    }
}
