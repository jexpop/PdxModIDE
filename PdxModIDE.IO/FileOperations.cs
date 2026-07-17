using System;
using System.IO;
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
