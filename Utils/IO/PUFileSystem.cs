using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Phobos.Interface.IO;

namespace Phobos.Utils.IO
{
    /// <summary>
    /// 文件系统工具实现
    /// </summary>
    public class PUFileSystem : PIFileSystem
    {
        private static PUFileSystem? _instance;
        private static readonly object _lock = new();

        public static PUFileSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PUFileSystem();
                    }
                }
                return _instance;
            }
        }

        public async Task<string> ReadAllText(string path)
        {
            return await File.ReadAllTextAsync(path);
        }

        public async Task WriteAllText(string path, string content)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(path, content);
        }

        public async Task<byte[]> ReadAllBytes(string path)
        {
            return await File.ReadAllBytesAsync(path);
        }

        public async Task WriteAllBytes(string path, byte[] bytes)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllBytesAsync(path, bytes);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void DeleteDirectory(string path, bool recursive = false)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive);
            }
        }

        public string[] GetFiles(string path, string searchPattern = "*", bool recursive = false)
        {
            if (!Directory.Exists(path))
                return Array.Empty<string>();

            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.GetFiles(path, searchPattern, option);
        }

        public string[] GetDirectories(string path)
        {
            if (!Directory.Exists(path))
                return Array.Empty<string>();

            return Directory.GetDirectories(path);
        }

        public void CopyFile(string source, string dest, bool overwrite = false)
        {
            var destDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            File.Copy(source, dest, overwrite);
        }

        public void MoveFile(string source, string dest)
        {
            var destDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            File.Move(source, dest);
        }

        /// <summary>
        /// 递归创建文件夹（如果路径中的父目录不存在也会一并创建）
        /// 例如: C:\A\B\C 如果 C:\A 不存在，会依次创建 C:\A、C:\A\B、C:\A\B\C
        /// </summary>
        /// <param name="path">目标文件夹路径</param>
        /// <returns>是否成功创建（如果已存在也返回 true）</returns>
        public bool CreateFullFolders(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                // Directory.CreateDirectory 本身就支持递归创建
                // 但我们显式处理以确保行为清晰
                if (Directory.Exists(path))
                    return true;

                Directory.CreateDirectory(path);
                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 异步递归创建文件夹
        /// </summary>
        /// <param name="path">目标文件夹路径</param>
        /// <returns>是否成功创建</returns>
        public Task<bool> CreateFullFoldersAsync(string path)
        {
            return Task.Run(() => CreateFullFolders(path));
        }
    }
}