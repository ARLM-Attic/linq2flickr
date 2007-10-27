﻿using System;
using System.Configuration;
using System.Collections;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using Flickr.Core;
using System.IO;

namespace Flickr.Web
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                this.BindData();
            }
        }

        private void BindData()
        {
            FlickrContext context = new FlickrContext();
            var query = (from ph in context.Photos
                         where ph.ViewMode == ViewMode.Owner
                         select ph).Take(12).Skip(0);

            lstPhotos.DataSource = query.ToList<Photo>();
            lstPhotos.DataBind();
        }

        protected void lstPhotos_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                Photo photo =  e.Item.DataItem as Photo;

                LinkButton lnkDetail = (LinkButton)e.Item.FindControl("lnkImage");

                lnkDetail.CommandArgument = photo.Id;

                Image image = (Image)e.Item.FindControl("photo");
                image.ImageUrl = photo.Url;

            }
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            FlickrContext context = new FlickrContext();
            context.Photos.Add(new Photo{ FileName = Path.GetFileName(uploader.Value), File = uploader.PostedFile.InputStream, ViewMode = ViewMode.Private});
            context.SubmitChanges();

            BindData();
        }

        public string PhotoId { get; set; }

        protected void lstPhotos_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "showDetail")
            {
               LinkButton lnk = (LinkButton)  e.Item.FindControl("lnkImage");

                PhotoId = (string)e.CommandArgument;
                hPhotoId.Value = PhotoId;

                FlickrContext context = new FlickrContext();

                var query = from ph in context.Photos
                            where ph.Id == PhotoId && ph.PhotoSize == PhotoSize.Medium
                            select ph;

                Photo photo = query.Single<Photo>();

                photoDetail.ImageUrl = photo.Url;
                detailView.Visible = true;
                nomarlView.Visible = false;
            }
        }

        protected void lnkBack_Click(object sender, EventArgs e)
        {
            detailView.Visible = false;
            nomarlView.Visible = true;
        }

        protected void lnkDelete_Click(object sender, EventArgs e)
        {
            PhotoId = hPhotoId.Value;
            //FlickrContext
            FlickrContext context = new FlickrContext();
            var query = from ph in context.Photos
                         where ph.Id == PhotoId
                         select ph;

            Photo photo = query.Single<Photo>();

            context.Photos.Remove(photo);
            context.SubmitChanges();

            detailView.Visible = false;
            nomarlView.Visible = true;
            BindData();
        }
    }
}
