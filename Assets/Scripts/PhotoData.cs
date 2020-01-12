using System;

[Serializable]
public class PhotoData
{
    public string Photo_uid; //key in database
    public string Dropbox_id;
    public string Filename;
    public int Size;
    public string Path_display;
    public int Month;
    public int Day;
    public int Year;
    public string Location;
    public string Geo_lat;
    public string Geo_lon;
    public string Comments;
    public string Favorite;
    public string Forget_photo;
    public string File_type;
    public string Audio_file_path;
    public string Last_edited_date;
    public string Last_editor_name;

    public PhotoData() { }

    public PhotoData(
            string photo_uid,
            string dropbox_id,
            string filename,
            int size,
            string path_display,
            int month,
            int day,
            int year,
            string location,
            string geo_lat,
            string geo_lon,
            string comments,
            string favorite,
            string forget_photo,
            string file_type,
            string audio_file_path,
            string last_edited_date,
            string last_editor_name
        )
    {
            Photo_uid = photo_uid;
            Dropbox_id = dropbox_id;
            Filename = filename;
            Size = size;
            Path_display = path_display;
            Month = month;
            Day = day;
            Year = year;
            Location = location;
            Geo_lat = geo_lat;
            Geo_lon = geo_lon;
            Comments = comments;
            Favorite = favorite;
            Forget_photo = forget_photo;
            File_type = file_type;
            Audio_file_path = audio_file_path;
            Last_edited_date = last_edited_date;
            Last_editor_name = last_editor_name;
    }

}