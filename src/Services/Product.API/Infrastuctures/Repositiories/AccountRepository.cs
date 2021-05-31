using Dapper;
using Product.API.Domains.Entities;
using Product.API.Infrastuctures.Providers;
using SqlKata.Compilers;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Product.API.Infrastuctures.Repositiories
{
    public interface IAccountRepository
    {
        Task<IEnumerable<Account>> GetAllAsync();

        Task<IEnumerable<Account>> GetAllSqlKataAsync();
    }

    public class AccountRepository : IAccountRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

        public AccountRepository(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _databaseConnectionFactory = databaseConnectionFactory;
        }

        public async Task<IEnumerable<Account>> GetAllAsync()
        {
            try
            {
                using var conn = await _databaseConnectionFactory.CreateConnectionAsync();

                var sql = $@"select * from tbl_Account";

                var result = await conn.QueryAsync<Account>(sql);

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Account>> GetAllSqlKataAsync()
        {
            try
            {
                using var conn = await _databaseConnectionFactory.CreateConnectionAsync();
                var db = new QueryFactory(conn, new SqlServerCompiler());

                var result = db.Query("tbl_Account").Paginate(1,10);

                foreach (var book in result)
                {
                    Console.WriteLine($"{book.Title}: {book.AuthorName}");
                }

                return await result.GetAsync<Account>();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}