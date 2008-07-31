Getting stared - Athena : a LINQ to flickr API
=====================================================

1. Add referece to the LINQ.Flickr.DLL and LinqExtender.dll (it is for the core provider functionality that is used by Linq.Flickr)
2. Use the following namecespace declartion at the top of your cs file

using LINQ.Flickr;
using System.Linq;
using LinqExtender; -- Athena is made on this toolkit.

3. Define the flickr connection string in your web/app config file.


  <configSections>
    <section name="flickr" type="Linq.Flickr.Configuration.FlickrSettings, Linq.Flickr"/>
  </configSections>
 
  <flickr apiKey="#Your key#"
    secretKey="#Your secrect#"
    cacheDirectory="cache" (optional, only for desktop apps) />

In cache direcotry the authentication token will be stored for desktop based application.




Example queries 
===============

from ph in context.Photos
where ph.ViewMode == ViewMode.Public
&& ph.SearchText == 'Iphone' && 
ph.SearchMode == SearchMode.FreeText 
orderby PhotoOrder.Date_Posted descending
select ph;

Projection 
-------------

where ph.PhotoSize == PhotoSize.Medium 
&& ph.SearchText == "iphone" && 
ph.SearchMode == SearchMode.TagsOnly
orderby PhotoOrder.Date_Taken descending
select new { ph.Title, ph.Url };


Query , add, delete comment
---------------------------

Comment  comment = new Comment { PhotoId = "someId", Text="This is nice"};
 
_context.Photos.Comments.Add(comment);
_context.SubmitChanges();
 
var query = from cm in _context.Photos.Comments
                  where  cm .PhotoId == 'someId' && cm .Id == comment.Id
                  Select cm;
 
comment = query.Single();
 
_context.Photos.Comment.Remove(comment);
_context.SubmitChanges();


Find people either by username or nsid
--------------------------------------


var query = from people in _context.People
where people.Username == 'someusername'
or 
var query = from people in _context.People
where people.Id == 'nsId'

Query Popular tags by their score , order them by score, title or period duration
-------------------------------------------------------------------------------- 

var query = from tag in _content.PopularTags
where tag.count == 30 order by tag.title
select tag;

Updating 
------------
Lets update the photo title.
{{

Photo photo= (from p in context.Photots
where p.id == 123
select p).Single();

photo.Title = This is new Title.

// make the update possible just do the following
coment.SubmitChanges();

//This will fire the Update in LinqExtender that where logic for flickr.photos.setMeta is written.
//Flickr api support only alteration of Title and Description, more info can be found at  flickr.photos.setMeta

}}

Similarly, you can update comment like

{{

Comment comment = (from c in context.Photots.Comments
where c.Id == "1234"
select c).Single();

comment.Text = "This is a updated comment.";
coment.SubmitChanges();

}}


Photo.Extras
-----------------

Photo.ExtrasOption
/// A comma-delimited list of extra information to fetch for each returned record. 
/// Currently supported fields are: license, date_upload, date_taken, owner_name, icon_server, 
/// original_format, last_update, geo, tags, machine_tags, views, media. 
/// Use ExtrasOption enum with  | to set your options. Ex 
/// p.Extras == (ExtrasOption.Views | ExtrasOption.Date_Taken | ExtrasOption.Date_Upload)


var query = from photo in context.Photos
            where photo.SearchText == "Redmond" && p.Extras == (ExtrasOption.Views | ExtrasOption.Date_Taken | ExtrasOption.Date_Upload)
            select photo;

This will return the photos with extra info like Views(Photo.views), Date taken (Photo.TakenOn), Date uploaded(Photo.UploadedOn), as mentioned In query.


Trapping errors
----------------

_context.Photos.OnError += new LinqExtender.Query<Photo>.ErrorHandler(Photos_OnError);
 
/*Inside Photos_OnError*/
 
 
void Photos_OnError(ProviderException ex)
{
   string message =ex.Message;
   string stackTrace =ex.StackTrace;
}
 
/* All query object inherits the changes of LinqExtender */
 
_context.Photos.Comments.OnError
_context.Photos.Peoples
 
/*All have the OnError handler */


Others
---------
1. While getting multiple photos, we might need to know page number, total photos, total pages and 
per page item count.These are stored under Photo.SharedProperty
 

More combination can be possible, its all up to you.

=========================

That's it Enjoy!


If any issue plz mail at  mehfuz@gmail.com.


