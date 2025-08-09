# Case: Video Upload & Conversion System
    When users upload a video, it needs to be saved and converted into different resolutions (480p, 720p, 1080p). 
    After conversion, links should be sent to the user.

# Analysis:
    - Users can upload anysize of video
    - video conversion request may come at huge numbers at a same time
    - Video processing take time
    - Sequential processing of each video will arise huge delay
    - may declare a set of workers who will parallely proces multiple videos
    - when bandwidth is really low, may fragment the video to process multi part of it using workers
        - if 100 workers and 10 videos
        - can process 10 worker to each video by diving the video in 10 parts
    - after processing, will store it to cloud storage
    - after successful upload, generate a link and send the link to the user