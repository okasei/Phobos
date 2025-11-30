using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Phobos.Interface.Database
{
    /// <summary>
    /// 数据库连接接口
    /// </summary>
    public interface PIDatabase
    {
        /// <summary>
        /// 数据库路径
        /// </summary>
        string DatabasePath { get; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接数据库
        /// </summary>
        Task<bool> Connect(string? password = null);

        /// <summary>
        /// 断开连接
        /// </summary>
        Task Disconnect();

        /// <summary>
        /// 执行非查询SQL
        /// </summary>
        Task<int> ExecuteNonQuery(string sql, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// 执行查询SQL
        /// </summary>
        Task<List<Dictionary<string, object>>> ExecuteQuery(string sql, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// 执行标量查询
        /// </summary>
        Task<object?> ExecuteScalar(string sql, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// 开始事务
        /// </summary>
        Task BeginTransaction();

        /// <summary>
        /// 提交事务
        /// </summary>
        Task CommitTransaction();

        /// <summary>
        /// 回滚事务
        /// </summary>
        Task RollbackTransaction();
    }

    /// <summary>
    /// 数据库仓储接口
    /// </summary>
    public interface PIRepository<T> where T : class
    {
        Task<T?> GetById(object id);
        Task<List<T>> GetAll();
        Task<bool> Insert(T entity);
        Task<bool> Update(T entity);
        Task<bool> Delete(object id);
        Task<List<T>> Query(string condition, Dictionary<string, object>? parameters = null);
    }
}