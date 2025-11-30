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
    }

    /// <summary>
    /// 消息通信实现
    /// </summary>
    public class PUMessenger : PIMessenger
    {
        private static PUMessenger? _instance;
        private static readonly object _lock = new();

        private readonly ConcurrentDictionary<string, List<Action<object>>> _subscriptions = new();

        public static PUMessenger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PUMessenger();
                    }
                }
                return _instance;
            }
        }

        public async Task<bool> Send(string channel, object message)
        {
            return await Task.Run(() =>
            {
                if (_subscriptions.TryGetValue(channel, out var handlers))
                {
                    lock (handlers)
                    {
                        foreach (var handler in handlers)
                        {
                            try
                            {
                                handler(message);
                            }
                            catch { }
                        }
                    }
                    return true;
                }
                return false;
            });
        }

        public void Subscribe(string channel, Action<object> handler)
        {
            var handlers = _subscriptions.GetOrAdd(channel, _ => new List<Action<object>>());
            lock (handlers)
            {
                if (!handlers.Contains(handler))
                {
                    handlers.Add(handler);
                }
            }
        }

        public void Unsubscribe(string channel, Action<object>? handler = null)
        {
            if (_subscriptions.TryGetValue(channel, out var handlers))
            {
                lock (handlers)
                {
                    if (handler != null)
                    {
                        handlers.Remove(handler);
                    }
                    else
                    {
                        handlers.Clear();
                    }
                }
            }
        }

        public async Task Publish(string channel, object message)
        {
            await Send(channel, message);
        }
    }
}