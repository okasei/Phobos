using System.IO;

namespace Phobos.Utils.IO
{
    public class PUFile
    {
        private static readonly Lazy<PUFile> _lazyInstance = new Lazy<PUFile>(() => new PUFile());
        private PUFile() { }
        public static PUFile Instance => _lazyInstance.Value;
        /// <summary>
        /// 删除指定目录中特定类型的文件，但保留指定的例外文件
        /// </summary>
        /// <param name="directoryPath">要清理的目录路径</param>
        /// <param name="filePatterns">要删除的文件类型模式（如 "*.txt", "*.log"）</param>
        /// <param name="excludedFiles">要保留的文件名列表（包含扩展名）</param>
        /// <returns>删除的文件数量</returns>
        public int DeleteFilesExcept(string directoryPath, string[] filePatterns, string[] excludedFiles)
        {
            if (string.IsNullOrEmpty(directoryPath))
                throw new ArgumentException("目录路径不能为空");

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");

            if (filePatterns == null || filePatterns.Length == 0)
                throw new ArgumentException("文件类型模式不能为空");

            // 将排除文件列表转换为不区分大小写的集合，便于比较[4](@ref)
            var excludedSet = new HashSet<string>(
                excludedFiles ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase
            );

            int deletedCount = 0;

            // 递归删除文件[8](@ref)
            DeleteFilesRecursive(directoryPath, filePatterns, excludedSet, ref deletedCount);

            return deletedCount;
        }

        private void DeleteFilesRecursive(string currentPath, string[] patterns, HashSet<string> excludedSet, ref int deletedCount)
        {
            try
            {
                // 处理当前目录中的文件[6](@ref)
                foreach (string pattern in patterns)
                {
                    string[] files = Directory.GetFiles(currentPath, pattern);

                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);

                        // 检查文件是否在排除列表中[3](@ref)
                        if (!excludedSet.Contains(fileName))
                        {
                            try
                            {
                                File.Delete(file);
                                deletedCount++;
                                Console.WriteLine($"已删除: {file}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"删除文件失败 {file}: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"保留文件: {file}");
                        }
                    }
                }

                // 递归处理子目录[7](@ref)
                string[] subDirectories = Directory.GetDirectories(currentPath);
                foreach (string subDir in subDirectories)
                {
                    DeleteFilesRecursive(subDir, patterns, excludedSet, ref deletedCount);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"无权访问目录: {currentPath}");
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine($"目录不存在: {currentPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理目录 {currentPath} 时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 重载方法：支持单个文件模式和单个排除文件
        /// </summary>
        public int DeleteFilesExcept(string directoryPath, string filePattern, string excludedFile)
        {
            return DeleteFilesExcept(directoryPath, new[] { filePattern }, new[] { excludedFile });
        }
    }
}
