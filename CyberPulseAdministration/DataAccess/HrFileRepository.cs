using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using CyberPulseAdministration.Models;

namespace CyberPulseAdministration.DataAccess
{
    public class HrFileRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["CyberPulseDb"]?.ConnectionString;

        public List<HrFile> GetAll()
        {
            var list = new List<HrFile>();
            if (string.IsNullOrEmpty(_connectionString)) return list;

            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_GetHrFiles", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapHrFile(reader));
                        }
                    }
                }
            }
            return list;
        }

        public HrFile GetById(int id)
        {
            if (string.IsNullOrEmpty(_connectionString)) return null;

            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_GetHrFileById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapHrFile(reader);
                        }
                    }
                }
            }
            return null;
        }

        public int Insert(HrFile file)
        {
            if (string.IsNullOrEmpty(_connectionString)) return 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_InsertHrFile", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Title", file.Title);
                    cmd.Parameters.AddWithValue("@FileName", file.FileName);
                    cmd.Parameters.AddWithValue("@FilePath", file.FilePath);
                    cmd.Parameters.AddWithValue("@FileType", file.FileType);
                    cmd.Parameters.AddWithValue("@FileSize", file.FileSize);

                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        public void Delete(int id)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_DeleteHrFile", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private HrFile MapHrFile(SqlDataReader reader)
        {
            return new HrFile
            {
                ID = Convert.ToInt32(reader["ID"]),
                Title = reader["Title"].ToString(),
                FileName = reader["FileName"].ToString(),
                FilePath = reader["FilePath"].ToString(),
                FileType = reader["FileType"].ToString(),
                UploadDate = Convert.ToDateTime(reader["UploadDate"]),
                FileSize = Convert.ToInt64(reader["FileSize"])
            };
        }
    }
}
