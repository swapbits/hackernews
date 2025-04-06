namespace Stories.Application.Services;

public interface IBestStoriesService {
    Task<BestStoriesResult> GetBestStoriesAsync(int limit, CancellationToken cancellationToken);
}