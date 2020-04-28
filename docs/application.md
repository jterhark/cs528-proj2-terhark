# Interesting Things

- Unity did not adopt the async/await Task based programming that is common in C# programs nowadays and instead opted to create their own asynchronous programming framework using Coroutines. Since these have been designed to affect GameObjects or web requests, they did not help me out when I was researching ways to load and process the data in the background.
- Since a lot of this project was centered around scripts, Unity crashes were not as noticeable/detrimental during the development process. One thing I did notice, however, was when one of my scripts would either crash or was posting a large amount of events to the main thread, the entire Unity editor would freeze up and I would have to set a breakpoint in the code and change some variable values to unfreeze the main Unity interface.
- Post Process anti-aliasing makes some objects unrecognizable in VR.
- I never realized what constellations look like from a different angle. I always knew that a different perspective would change things, but I never realized how drastically far apart some stars in the same constellations are.
