using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender;
using OpenLinqToSql;
using System.Data.SqlServerCe;
using System.Data;
using System.Configuration;
using System.IO;
using Linq.Flickr;

namespace FlickrConsole
{
    public enum Action
    {
        Add,
        Upload,
        Revert,
        Delete,
    }

    public class PhotoManager
    {
        private string _dataSource = string.Empty;
        private string _fileName = string.Empty;
        private  FlickrContext _context = new FlickrContext();
        private  SqlQuery<PhotoUploadStatus> _photoContext = new SqlQuery<PhotoUploadStatus>();

        public PhotoManager()
        {
            _context.Photos.OnError += new LinqExtender.Query<Photo>.ErrorHanler(Photos_OnError);
            _context.Photos.OnSuccess += new LinqExtender.Query<Photo>.SuccessHandler(Photos_OnSuccess);
            _photoContext.OnError += new Query<PhotoUploadStatus>.ErrorHanler(PhotoContext_OnError);

            OpenLinqToSql.Configuration.OpenLinqDataProviderConfiguration config =
                (OpenLinqToSql.Configuration.OpenLinqDataProviderConfiguration)System.Configuration.ConfigurationManager.GetSection("customDataConfig");
            _dataSource = config.ConnectionString;

            CreateCacheDataBase();
        }

        public IList<Photo> SearchPhoto(string[] args)
        {
            // do query.
            var query = (from ph in _context.Photos
                         where ph.PhotoSize == PhotoSize.Medium && ph.SearchText == args[1] && ph.SearchMode == SearchMode.FreeText
                         && ph.User == (args.Length > 2 ? args[2] : string.Empty)
                         orderby PhotoOrder.Date_Posted descending
                         select ph).Take(10).Skip(0);


            return query.ToList<Photo>();
        }

        public delegate void StatusHandler(string message);
        public event StatusHandler OnStatus;

        void PhotoContext_OnError(string error)
        {
            RaiseEvent(error);
        }

        private void RaiseEvent(string error)
        {
            if (OnStatus != null)
                OnStatus(_action.ToString() + " : " + error);
        }

        void Photos_OnSuccess(Photo item)
        {
            RaiseEvent(item.FileName);
        }

        void Photos_OnError(string error)
        {
            RaiseEvent(error);
        }

        private Action _action = Action.Add;

        public void PerformAction(Action action, string[] args)
        {
            _action = action;

            switch (action)
            {
                case Action.Upload:
                    UploadPhoto(args);
                    break;
                case Action.Add:
                    AddPhotosTobeUploaded(args);
                    break;
            }
        }

        public void AddPhotosTobeUploaded(string[] args)
        {
            string [] files = new string[0];

            if (Directory.Exists(args[1]))
                 files = Directory.GetFiles(args[1]);
            if (File.Exists(args[1]))
                files = new string[]{ args[1] };
           
            bool added = false;

            foreach (string file in files)
            {
                int count = (from status in _photoContext
                             where status.Path == file
                             select status).Count();
                if (count == 0)
                {
                    _photoContext.Add(new PhotoUploadStatus { Action = args[0], Path = file, Synced = false });
                    added = true;
                }
            }

            if (!added)
                Console.WriteLine("Nothing new to add");
            else
                _photoContext.SubmitChanges();
        }

        public void UploadPhoto(string[] args)
        {
            if (args.Length != 2)
                throw new ApplicationException("Usage : app.exe upload <path>");

            try
            {
                string[] files = Directory.GetFiles(args[1]);

                foreach (string file in files)
                {
                    FileStream fileSream = File.OpenRead(file);
                    _context.Photos.Add(new Photo { FileName = file, File = fileSream, ViewMode = ViewMode.Public });
                }
                _context.SubmitChanges();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
       
        private void CreateCacheDataBase()
        {
            SqlCeConnection conn = null;
            
            try
            {
                if (!File.Exists("FlickrCache.sdf"))
                {
                    SqlCeEngine engine = new SqlCeEngine(_dataSource);
                    engine.CreateDatabase();

                    conn = new SqlCeConnection(_dataSource);
                    conn.Open();

                    SqlCeCommand cmd = conn.CreateCommand();

                    cmd.CommandText = FlickrConsole.Properties.Settings.Default.TableScript;
                    cmd.ExecuteNonQuery();
                }

            }
            catch (SqlCeException e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (conn != null)
                {
                    if (conn.State == ConnectionState.Open)
                        conn.Close();
                }
            }
        }

    }
}
