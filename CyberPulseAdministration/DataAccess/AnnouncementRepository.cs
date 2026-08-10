using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using CyberPulseAdministration.Models;

namespace CyberPulseAdministration.DataAccess
{
    public class AnnouncementRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["CyberPulseDb"].ConnectionString;

        public List<Announcement> GetAll()
        {
            var list = new List<Announcement>();
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_GetAnnouncements", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapAnnouncement(reader));
                        }
                    }
                }
            }
            return list;
        }

        public Announcement GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_GetAnnouncementById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapAnnouncement(reader);
                        }
                    }
                }
            }
            return null;
        }

        public int Insert(Announcement ann)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_InsertAnnouncement", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Title", ann.Title);
                    cmd.Parameters.AddWithValue("@Description", (object)ann.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Date", ann.Date);
                    cmd.Parameters.AddWithValue("@ShortDescription", (object)ann.ShortDescription ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PageTitle", (object)ann.PageTitle ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", ann.IsActive);
                    
                    if (!ann.AnnouncementGuid.HasValue || ann.AnnouncementGuid == Guid.Empty)
                    {
                        ann.AnnouncementGuid = Guid.NewGuid();
                    }
                    cmd.Parameters.AddWithValue("@AnnouncementGuid", ann.AnnouncementGuid);

                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        public void Update(Announcement ann)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_UpdateAnnouncement", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", ann.ID);
                    cmd.Parameters.AddWithValue("@Title", ann.Title);
                    cmd.Parameters.AddWithValue("@Description", (object)ann.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Date", ann.Date);
                    cmd.Parameters.AddWithValue("@ShortDescription", (object)ann.ShortDescription ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PageTitle", (object)ann.PageTitle ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", ann.IsActive);
                    cmd.Parameters.AddWithValue("@AnnouncementGuid", (object)ann.AnnouncementGuid ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_DeleteAnnouncement", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Announcement> GetActive()
        {
            var list = new List<Announcement>();
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("usp_GetActiveAnnouncements", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapAnnouncement(reader));
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
                using (var cmd = new SqlCommand("usp_ToggleAnnouncementActive", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.Parameters.AddWithValue("@IsActive", isActive);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private Announcement MapAnnouncement(SqlDataReader reader)
        {
            return new Announcement
            {
                ID = Convert.ToInt32(reader["ID"]),
                Title = reader["Title"].ToString(),
                Description = reader["Description"].ToString(),
                Date = Convert.ToDateTime(reader["Date"]),
                ShortDescription = reader["ShortDescription"].ToString(),
                PageTitle = reader["PageTitle"].ToString(),
                IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                AnnouncementGuid = reader["AnnouncementGuid"] != DBNull.Value ? (Guid?)reader["AnnouncementGuid"] : null
            };
        }
    }
}
