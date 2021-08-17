using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace EntityModelGenerator
{
    public static class Database
    {
        public static string ServerName { get; set; }
        public static int  Authentication { get; set; }
        public static string Login { get; set; }
        public static string Password { get; set; }

        public static string ConnectionString(string dbname)
        {
            try
            {
                SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder();
                sqlBuilder.DataSource = ServerName;
                sqlBuilder.PersistSecurityInfo = false;
                sqlBuilder.ConnectTimeout = 30;
                sqlBuilder.InitialCatalog = dbname;

                switch (Authentication)
                {
                    case 0:
                        sqlBuilder.IntegratedSecurity = true;
                        break;
                    default:
                        sqlBuilder.IntegratedSecurity = false;
                        sqlBuilder.UserID = Login;
                        sqlBuilder.Password = Password;
                        break;
                }

                return sqlBuilder.ConnectionString;
            }
            catch (Exception)
            {
            }
            return string.Empty;
        }
    }
}
