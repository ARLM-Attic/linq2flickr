using System;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using Linq.Flickr;
using System.IO;
using System.Linq;

namespace LinqFlickr_Demo
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (ShowOnlyMyPhotos)
                {
                    rbMeOnly.Checked = true;
                    rbPublic.Checked = false;
                }
                else
                {
                    rbPublic.Checked = true;
                    rbMeOnly.Checked = false;
                }

                this.BindData();
            }
            //errorPanel.Visible = false;
        }

        protected override void OnError(EventArgs e)
        {
            errorPanel.Visible = true;
            lblStatus.Text = "Opps , there has been some error proccesing request";
        }

        private bool ShowOnlyMyPhotos =Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["showOnlyMyPhotots"]);

        private void BindData()
        {
            FlickrContext context = new FlickrContext();
            context.Photos.OnError += new LinqExtender.Query<Photo>.ErrorHanler(Photos_OnError);

            string text = textboxSearch.Text;

            detailView.Visible = false;
            nomarlView.Visible = true;

            ViewMode mode = ViewMode.Public;
            SearchMode sMode = SearchMode.FreeText;

            if (checkSearchTags.Checked)
            {
                sMode = SearchMode.TagsOnly;
            }

            if (rbMeOnly.Checked)
            {
                mode = ViewMode.Owner;
                panelUpload.Visible = true;
                lnkDelete.Visible = true;
            }
            else
            {
                lnkDelete.Visible = false;
                panelUpload.Visible = false;
            }

            var query = (from ph in context.Photos
                         where ph.ViewMode == mode && ph.SearchText == text && ph.SearchMode == sMode
                         orderby PhotoOrder.Date_Posted descending
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
                image.ToolTip = photo.Title;

            }
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                FlickrContext context = new FlickrContext();

                context.Photos.OnError += new LinqExtender.Query<Photo>.ErrorHanler(Photos_OnError);

                context.Photos.Add(new Photo { FileName = Path.GetFileName(uploader.Value), File = uploader.PostedFile.InputStream, ViewMode = chkPublic.Checked ? ViewMode.Public : ViewMode.Private, Title = txtTitle.Text.Trim() });
                context.SubmitChanges();

                BindData();
            }
            catch (Exception ex)
            {
                errorPanel.Visible = true;
                lblStatus.Text = ex.Message;
            }
        }

        void Photos_OnError(string error)
        {
            errorPanel.Visible = true;
            lblStatus.Text = error;
        }

        protected void lstPhotos_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "showDetail")
            {
                LinkButton lnk = (LinkButton)e.Item.FindControl("lnkImage");

                hPhotoId.Value = (string)e.CommandArgument;

                FlickrContext context = new FlickrContext();

                Photo photo = context.Photos.Where<Photo>(ph => ph.Id == (string)e.CommandArgument && ph.PhotoSize == PhotoSize.Medium).Single<Photo>(); ;

                string[] tags = (from tag in photo.PhotoTags
                                 select tag.Title).ToArray<string>();

                string tagText = string.Join(",", tags);

                lblTags.Text = string.IsNullOrEmpty(tagText) ? "(n/a)" : tagText;

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
            try
            {
                //FlickrContext
                FlickrContext context = new FlickrContext();
                context.Photos.OnError += new LinqExtender.Query<Photo>.ErrorHanler(Photos_OnError);

                var query = from ph in context.Photos
                            where ph.Id == hPhotoId.Value
                            select ph;

                Photo photo = query.Single<Photo>();

                context.Photos.Remove(photo);
                context.SubmitChanges();

                detailView.Visible = false;
                nomarlView.Visible = true;
                BindData();
            }
            catch (Exception ex)
            {
                errorPanel.Visible = true;
                lblStatus.Text = ex.Message;
            }
        }

        protected void buttonSearch_Click(object sender, EventArgs e)
        {
            this.BindData();
        }

        protected void rbPublic_CheckedChanged(object sender, EventArgs e)
        {
            this.BindData();
        }

        protected void rbMeOnly_CheckedChanged(object sender, EventArgs e)
        {
            this.BindData();
        }

       
    }
}
