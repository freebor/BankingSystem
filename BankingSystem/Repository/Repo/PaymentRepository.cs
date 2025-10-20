using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using Dapper;
using System.Data;

namespace BankingSystem.Repository.Repo
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly IDbConnection _db;

        public PaymentRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            payment.Id = Guid.NewGuid();
            payment.CreatedAt = DateTime.UtcNow;
            var sql = @"INSERT INTO Payments(Id, UserId, Provider, Reference, Status, Amount, CreatedAt)
                        VALUES (@Id, @UserId, @Provider, @Reference, @Status, @Amount, @CreatedAt)";
            await _db.ExecuteAsync(sql, payment);
            return payment;
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            var sql = "SELECT * FROM Payments WHERE Id = @Id";
            return await _db.QuerySingleOrDefaultAsync<Payment>(sql, new { Id = id });
        }

        public async Task<Payment?> GetByReferenceAsync(string reference)
        {
            var sql = "SELECT * FROM Payments WHERE Reference = @Reference";
            return await _db.QuerySingleOrDefaultAsync<Payment>(sql, new { Reference = reference });
        }

        public async Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId)
        {
            var sql = "SELECT * FROM Payments WHERE UserId = @UserId ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<Payment>(sql, new { UserId = userId });
        }

        public async Task<IEnumerable<Payment>> GetAllPaymentsAsync()
        {
            var sql = "SELECT * FROM Payments ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<Payment>(sql);
        }

        public async Task UpdateStatusAsync(Guid paymentId, string status)
        {
            var sql = "UPDATE Payments SET Status = @Status WHERE Id = @Id";
            await _db.ExecuteAsync(sql, new { Id = paymentId, Status = status });
        }
    }
}
