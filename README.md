# hackernews

Get best stories.

## Build
cd src/
docker build -f ./Dockerfile -t stories-api .

## Run
docker run --rm -p 8080:8080 stories-api

## Open in your browser:
http://localhost:8080/stories/best?limit=20

## Assumptions

* beststories.json contains the best stories sorted by their score (highest to lowest).
* beststories.json can contain a maximum of 500 stories.
* The "limit" argument is required.
* The "limit" argument must be greater than zero.
* If "limit" exceeds the number of available stories, all existing stories are returned.

## Implementation details and assumptions
There are two implementations of the service BestStoriesService and BestStoriesCachingService without cache and with cache correspondingly.
The implementation can be selected in appsettings.json by the option HackerNews:UseCache.

* BestStoriesCachingService (cache)
* BestStoriesService (no cache)
* There are unit tests in Stories.Application.UnitTest/TestBestStoriesService
* There is swagger http://localhost:5044/swagger/index.html (if you run the project in Development environment)

## Todo
* Replace hardcoded limits with configuration values from appsettings.json using DI.
* Add logs.
* Use non-blocking approach in GetStories methods.
* Create an integration tests project. Move tests from TestHackerNewsClient to it. Add stress tests.
