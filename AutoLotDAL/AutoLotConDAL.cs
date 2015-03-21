using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace AutoLotConnectedLayer
{
    public class InventoryDAL
    {
        private SqlConnection connect = null;

        public void OpenConnection(string connectionString)
        {
            connect = new SqlConnection(connectionString);
            connect.Open();
        }

        public void CloseConnection()
        {
            connect.Close();
        }

        public void InsertAuto(int id, string color, string make, string petName)
        {
            string sql = string.Format("Insert into Inventory" +
                "(CarId,Make,Color,PetName) Values('{0}','{1}','{2}','{3}')", id, make, color, petName);
            using (SqlCommand cmd = new SqlCommand(sql, this.connect))
            {
                SqlParameter param = new SqlParameter();
                param.ParameterName = "@CarID";
                param.Value = id;
                param.SqlDbType = SqlDbType.Int;
                cmd.Parameters.Add(param);

                param = new SqlParameter();
                param.ParameterName = "@Make";
                param.Value = make;
                param.SqlDbType = SqlDbType.Char;
                param.Size = 10;
                cmd.Parameters.Add(param);

                param = new SqlParameter();
                param.ParameterName = "@Color";
                param.Value = color;
                param.SqlDbType = SqlDbType.Char;
                param.Size = 10;
                cmd.Parameters.Add(param);

                param = new SqlParameter();
                param.ParameterName = "@PetName";
                param.Value = petName;
                param.SqlDbType = SqlDbType.Char;
                param.Size = 10;
                cmd.Parameters.Add(param);

                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteCar(int id)
        {
            string sql = string.Format("Delete from Inventory where CarID = {0}", id);
            using (SqlCommand cmd = new SqlCommand(sql, this.connect))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }

                catch (SqlException ex)
                {
                    Exception error = new Exception("К сожалению эта машина заказана!", ex);
                    throw error;
                }
            }
        }

        public void UpdateCarPetName(int id, string newPetName)
        {
            string sql = string.Format("Update Inventory set PetName = '{0}' where CarID = '{1}'", newPetName, id);
            using (SqlCommand cmd = new SqlCommand(sql, this.connect))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public DataTable GetAllInventoryAsDataTable()
        {
            DataTable dataTable = new DataTable();
            string sql = "Select * from inventory";
            using (SqlCommand cmd = new SqlCommand(sql, this.connect))
            {
                SqlDataReader dr = cmd.ExecuteReader();
                dataTable.Load(dr);
                dr.Close();
            }
            return dataTable;
        }

        public string GetCarPetName(int carID)
        {
            string carPetName = string.Empty;

            using (SqlCommand cmd = new SqlCommand("GetPetName", this.connect))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                SqlParameter param = new SqlParameter();
                param.ParameterName = "@carID";
                param.SqlDbType = SqlDbType.Int;
                param.Value = carID;
                param.Direction = ParameterDirection.Input;
                cmd.Parameters.Add(param);

                param = new SqlParameter();
                param.ParameterName = "@petName";
                param.SqlDbType = SqlDbType.Char;
                param.Size = 10;
                param.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(param);

                cmd.ExecuteNonQuery();

                carPetName = cmd.Parameters["@petName"].Value.ToString().Trim();
            }

            return carPetName;
        }

        public void ProcessCreditRisk(bool throwEx, int custId)
        {
            string FirstName = "";
            string LastName = "";

            string sql = String.Format("Select * from Customers where CustId = {0}", custId);
            SqlCommand cmd = new SqlCommand(sql, this.connect);
            
            using(SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.HasRows)
                {
                    dr.Read();
                    FirstName = dr["FirstName"].ToString();
                    LastName = dr["LastName"].ToString();
                }

                else return;
            }

            SqlCommand sqlRemove = new SqlCommand(String.Format("Delete from Customers where CustID = {0}", custId), this.connect);
            SqlCommand sqlInsert = new SqlCommand(String.Format("Insert Into CreditRisks (CustID, FirstName,LastName) Values ('{0}','{1}','{2}')", custId,FirstName,LastName));

            SqlTransaction sqlTransaction = null;

            try
            {
                sqlTransaction = connect.BeginTransaction();
                sqlRemove.Transaction = sqlTransaction;
                sqlInsert.Transaction = sqlTransaction;

                sqlRemove.ExecuteNonQuery();
                sqlInsert.ExecuteNonQuery();

                if (throwEx)
                {
                    throw new ApplicationException("Ошибка базы данных! Транзакция завершена неудачно.");
                }

                sqlTransaction.Commit();
            }

            catch (Exception ex)
            { 
                Console.WriteLine(ex.Message);
                sqlTransaction.Rollback();
            }


        }
    }
}