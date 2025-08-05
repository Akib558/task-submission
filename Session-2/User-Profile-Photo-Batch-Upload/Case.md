# Case: User Profile Photo Batch Upload
    A mobile app allows users to select multiple profile photos to upload at once. 
    Each uploaded image must be optimized (compressed), renamed, and stored before the user can proceed.

# Analysis:
- User will select multiple photos
- Must handle multiple images perfectly, such that no image is lost
- There needs to be a concern about handling the bandwidth for huge amounts of users if all upload same time
- After successful upload images should be:
  - compressed
  - renamed
  - stored