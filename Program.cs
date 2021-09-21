using System;
using System.Data.SqlClient;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var conString =
                "Data source=TAMANNAH-MAVLON\\SQLEXPRESS; Initial catalog=Transaction; Integrated security = true";
            Console.WriteLine("Выберите команду: 1 - создать счёт, 2 - трансфер, 3 - Показать");
            int.TryParse(Console.ReadLine(), out var action);
            switch (action)
            {
                case 1:
                {
                    Console.WriteLine("Введите пятизначаный счёт:");
                    AccountModel model = new AccountModel();
                    model.Account = Console.ReadLine();
                    model.CreatedAt = DateTime.Now;

                    CreateAccount(conString, model);
                }
                    break;
                case 2:
                {
                    Console.Write("From account:");
                    var fromAcc = Console.ReadLine();

                    Console.Write("To account:");
                    var toAcc = Console.ReadLine();
                    Console.Write("Amount:");
                    decimal.TryParse(Console.ReadLine(), out var amount);

                    TransferFromToAcc(fromAcc,toAcc,amount, conString);
                }
                    break;
                case 3:
                {
                    ShowAccount(conString);
                }
                    break;
            }
        }
        
        private static decimal GetAccountBalance(string conString, string account)
        {
            var conn = new SqlConnection(conString);
            conn.Open();
            var command = conn.CreateCommand();
            command.CommandText = "select sum( case when t.Account_id=a.Id then t.Amount * -1 else t.Amount end) from Transactions t left join Accounts a on t.Account_Id = a.Id where a.Account = @fromAcc";
            command.Parameters.AddWithValue("@fromAcc", account);
            var reader = command.ExecuteReader();
            var fromAccBalance = 0m;

            while (reader.Read())
            {
                fromAccBalance = !string.IsNullOrEmpty(reader.GetValue(0)?.ToString()) ? reader.GetDecimal(0) : 0;
            }

            reader.Close();
            command.Parameters.Clear();

            conn.Close();
            return fromAccBalance;
        }

        private static void TransferFromToAcc(string fromAcc, string toAcc, decimal amount, string conString)
        {
            if (string.IsNullOrEmpty(fromAcc) || string.IsNullOrEmpty(toAcc) || amount == 0)
            {
                Console.WriteLine("Something went wrong.");
                return;
            }

            var conn = new SqlConnection(conString);
            conn.Open();

            if (conn.State != System.Data.ConnectionState.Open)
            {
                return;
            }

            SqlTransaction sqlTransaction = conn.BeginTransaction();

            var command = conn.CreateCommand();

            command.Transaction = sqlTransaction;

            try
            {
                var fromAccBalance = GetAccountBalance(conString, fromAcc);

                var fromAccId = GetAccountId(fromAcc, conString);

                if (fromAccId == 0)
                {
                    throw new Exception("Account not found");
                }

                command.CommandText = "INSERT INTO [dbo].[Transactions]([Amount] ,[Created_At] ,[Account_Id]) VALUES (@amount , @createdat, @accountid)";
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@createdat", DateTime.Now);
                command.Parameters.AddWithValue("@accountid", fromAccId);

                var result1 = command.ExecuteNonQuery();

                var toAccId = GetAccountId(toAcc, conString);

                if (toAccId == 0)
                {
                    throw new Exception("Account not found");
                }

                command.Parameters.Clear();

                command.CommandText = "INSERT INTO [dbo].[Transactions]([Amount] ,[Created_At] ,[Account_Id]) VALUES (@amount , @createdat, @accountid)";
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@createdat", DateTime.Now);
                command.Parameters.AddWithValue("@accountid", toAccId);

                var result2 = command.ExecuteNonQuery();

                if (result1 == 0 || result2 == 0)
                {
                    throw new Exception("Something went wrong");
                }

                sqlTransaction.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                sqlTransaction.Rollback();
            }
            finally
            {
                conn.Close();
            }
        }

        private static int GetAccountId(string account, string conString)
        {
            var accNumber = 0;
            var connection = new SqlConnection(conString);
            var query = "SELECT [Id] FROM [dbo].[Accounts] WHERE [Account] = @account";

            var command = connection.CreateCommand();
            command.Parameters.AddWithValue("@account", account);
            command.CommandText = query;

            connection.Open();

            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                accNumber = reader.GetInt32(0);
            }
            connection.Close();
            reader.Close();

            return accNumber;
        }

        public static void CreateAccount(string conString, AccountModel model)
        {
            var conn = new SqlConnection(conString);
            conn.Open();

            SqlTransaction sqlTransaction = conn.BeginTransaction();
            var command = conn.CreateCommand();
            command.Transaction = sqlTransaction;
            try
            {
                command.CommandText =
                    $@"INSERT INTO [dbo].[Accounts]([Account], [Is_Active], [Balance], [Created_At], [Updated_At])
            VALUES('{model.Account}', {model.IsActive}, {model.Balance}, @createdAt, null)
            ";
                command.Parameters.AddWithValue("@createdAt", model.CreatedAt);
                //вызываем команду
                var result = command.ExecuteNonQuery();
                if (result > 0)
                    Console.WriteLine("Удачно создался счет");
                sqlTransaction.Commit();
            }
            catch (Exception ex)
            {
                sqlTransaction.Rollback();
            }

            conn.Close();
        }

        public static void ShowAccount(string conString)
        {
            AccountModel[] accountModel = new AccountModel[0];

            var connection = new SqlConnection(conString);
            var query = "SELECT [Id] ,[Account] ,[Is_Active] ,[Created_At] ,[Updated_At] FROM [dbo].[Accounts]";

            var command = connection.CreateCommand();
            command.CommandText = query;

            connection.Open();

            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var acc = new AccountModel() { };
                var transaction = new Transaction { };

                acc.Id = int.Parse(reader["Id"].ToString());
                acc.Account = reader["Account"].ToString();
                acc.IsActive = int.Parse(reader["Is_Active"].ToString());
                var x = reader["Created_At"]?.ToString();
                acc.CreatedAt = !string.IsNullOrEmpty(reader["Created_At"]?.ToString())
                    ? DateTime.Parse(reader["Created_At"].ToString())
                    : null;
                acc.UpdatedAt = !string.IsNullOrEmpty(reader["Updated_At"]?.ToString())
                    ? DateTime.Parse(reader["Updated_At"].ToString())
                    : null;
                AddAccount(ref accountModel, acc);
            }

            connection.Close();
            foreach (var accountModels in accountModel)
            {
                Console.WriteLine(
                    $"ID:{accountModels.Id}, Account:{accountModels.Account}, Is_Active:{accountModels.IsActive}, Created_At:{accountModels.CreatedAt}, Updated_At:{accountModels.UpdatedAt}");
            }
        }

        private static void AddAccount(ref AccountModel[] accountModels, AccountModel account1)
        {
            if (accountModels == null)
            {
                return;
            }

            Array.Resize(ref accountModels, accountModels.Length + 1);

            accountModels[^1] = account1;
        }
    }


    public class AccountModel
    {
        public int Id { get; set; }
        public string Account { get; set; }
        public decimal Balance { get; set; } = 100;
        public int IsActive { get; set; } = 1;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class Transaction
    {
        public int Id { get; set; }
        public string AccountId { get; set; }
        public decimal Ammount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}