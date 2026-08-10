using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using CyberPulseAdministration.Models;

namespace CyberPulseAdministration.DataAccess
{
    public class NewsArticleRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["CyberPulseDb"].ConnectionString;

        public List<NewsArticle> GetAll()
        {
            var list = new List<NewsArticle>();
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_GetNewsArticles", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapNewsArticle(reader));
                        }
                    }
                }
            }
            return list;
        }

        public NewsArticle GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_GetNewsArticleById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapNewsArticle(reader);
                        }
                    }
                }
            }
            return null;
        }

        public int Insert(NewsArticle article)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_InsertNewsArticle", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ImageName", (object)article.ImageName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ImagePath", (object)article.ImagePath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Type", article.Type);
                    cmd.Parameters.AddWithValue("@Description", (object)article.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Date", article.Date);
                    cmd.Parameters.AddWithValue("@URL", article.URL);
                    cmd.Parameters.AddWithValue("@IsActive", article.IsActive);

                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        public void Update(NewsArticle article)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_UpdateNewsArticle", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", article.ID);
                    cmd.Parameters.AddWithValue("@ImageName", (object)article.ImageName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ImagePath", (object)article.ImagePath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Type", article.Type);
                    cmd.Parameters.AddWithValue("@Description", (object)article.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Date", article.Date);
                    cmd.Parameters.AddWithValue("@URL", article.URL);
                    cmd.Parameters.AddWithValue("@IsActive", article.IsActive);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_DeleteNewsArticle", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<NewsArticle> GetActive()
        {
            var list = new List<NewsArticle>();
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_GetActiveNewsArticles", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapNewsArticle(reader));
                        }
                    }
                }
            }
            return list;
        }

        public void ToggleActive(int id, bool isActive)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_ToggleNewsArticleActive", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.Parameters.AddWithValue("@IsActive", isActive);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private NewsArticle MapNewsArticle(SqlDataReader reader)
        {
            return new NewsArticle
            {
                ID = Convert.ToInt32(reader["ID"]),
                ImageName = reader["ImageName"] != DBNull.Value ? reader["ImageName"].ToString() : null,
                ImagePath = reader["ImagePath"] != DBNull.Value ? reader["ImagePath"].ToString() : null,
                Type = reader["Type"].ToString(),
                Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : null,
                Date = Convert.ToDateTime(reader["Date"]),
                URL = reader["URL"].ToString(),
                IsActive = reader["ISactive"] != DBNull.Value && Convert.ToBoolean(reader["ISactive"])
            };
        }
    }
}
