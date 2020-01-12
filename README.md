# Photo Reviewer

Unity (C#) project enabling users to review and annotate family photos.  Works on iOS, Android, AppleTV, Mac, and PC.

For each photo, users can...
- Edit the date (year required -- month and day optional)
- Edit the location (pop-up list of prior entries makes this easier)
- Add a text comment
- Add a voice annotation up to 5 minutes long

Assumptions (in this version):
- Photos are scanned and stored on Dropbox
- Photo info is stored in a Google Sheet

Planned feature improvements:
- Use fast database as back-end (replace Google Sheets with Firebase or other dBase)
- Support other storage services (Google Drive, Amazon Photos, etc.)
- Auto-import photos into local photo storage (Apple or Google Photos)
- Enable a video "slide show" playing each photo with a "Ken Burns" effect with narration voiceover

